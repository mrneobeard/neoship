using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class ServiceAccountStore
{
    private readonly ShipDb db;
    private readonly PermissionClaimCodec codec;
    private readonly ILogger<ServiceAccountStore> logger;

    public ServiceAccountStore(ShipDb db, PermissionClaimCodec codec, ILogger<ServiceAccountStore> logger)
    {
        this.db = db;
        this.codec = codec;
        this.logger = logger;
    }

    public async Task<List<ServiceAccount>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        return await db.ServiceAccounts
            .Where(s => s.OrgId == orgId && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ServiceAccount> CreateAsync(
        Guid orgId, Guid createdBy, string name, string? description, CancellationToken ct = default)
    {
        var sa = new ServiceAccount
        {
            Id = Factory.NewGuid(),
            OrgId = orgId,
            CreatedBy = createdBy,
            Name = name,
            NameUpcase = name.ToUpperInvariant(),
            Description = description ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
        };

        db.ServiceAccounts.Add(sa);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Service account created: {Id} {Name} org={OrgId}", sa.Id, name, orgId);
        return sa;
    }

    public async Task<ServiceAccount?> GetAsync(Guid orgId, Guid serviceAccountId, CancellationToken ct = default)
    {
        return await db.ServiceAccounts
            .FirstOrDefaultAsync(s => s.Id == serviceAccountId && s.OrgId == orgId && s.DeletedAt == null, ct);
    }

    public async Task<ServiceAccount?> UpdateAsync(
        Guid orgId, Guid serviceAccountId, string? name, string? description, CancellationToken ct = default)
    {
        var sa = await GetAsync(orgId, serviceAccountId, ct);
        if (sa is null) return null;

        if (name is not null)
        {
            sa.Name = name;
            sa.NameUpcase = name.ToUpperInvariant();
        }

        if (description is not null) sa.Description = description;

        sa.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return sa;
    }

    public async Task<bool> DisableAsync(Guid orgId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var sa = await GetAsync(orgId, serviceAccountId, ct);
        if (sa is null) return false;

        sa.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Service account disabled: {Id} org={OrgId}", serviceAccountId, orgId);
        return true;
    }

    /// <summary>
    /// Enables a previously disabled service account.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when enabled; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> EnableAsync(Guid orgId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var sa = await db.ServiceAccounts
            .FirstOrDefaultAsync(s => s.Id == serviceAccountId && s.OrgId == orgId && s.DeletedAt != null, ct);
        if (sa is null)
        {
            return false;
        }

        sa.DeletedAt = null;
        sa.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Service account enabled: {Id} org={OrgId}", serviceAccountId, orgId);
        return true;
    }

    public (string PlaintextKey, ServiceAccountApiKey ApiKey) GenerateApiKey(
        Guid serviceAccountId, string name, string? description, string? scopesJson, DateTime? expiresAt)
    {
        var keyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);

        var plaintextKey = "nssa_" + Convert.ToBase64String(keyBytes);
        var digest = TokenStore.ComputeDigestBase64(keyBytes);

        var apiKey = new ServiceAccountApiKey
        {
            Id = Factory.NewGuid(),
            ServiceAccountId = serviceAccountId,
            Name = name,
            Description = description,
            KeyDigest = digest,
            ScopesJson = scopesJson ?? "[]",
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        return (plaintextKey, apiKey);
    }

    /// <summary>
    /// Authenticates a service account API key.
    /// </summary>
    /// <param name="rawKey">The plaintext API key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching key with its service account loaded when valid; otherwise <see langword="null"/>.</returns>
    public async Task<ServiceAccountApiKey?> AuthenticateApiKeyAsync(string rawKey, CancellationToken ct = default)
    {
        if (!TryDecodeApiKey(rawKey, out var keyBytes))
        {
            return null;
        }

        var digest = TokenStore.ComputeDigestBase64(keyBytes);

        var key = await db.ServiceAccountApiKeys
            .Include(k => k.ServiceAccount)
            .FirstOrDefaultAsync(k => k.KeyDigest == digest
                && k.DeletedAt == null
                && k.RevokedAt == null
                && k.ServiceAccount != null
                && k.ServiceAccount.DeletedAt == null
                && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow), ct);

        if (key is null || key.ServiceAccount is null)
        {
            return null;
        }

        logger.LogInformation("Service account API key authenticated: {ApiKeyId} for service account {ServiceAccountId}", key.Id, key.ServiceAccountId);
        return key;
    }

    private static bool TryDecodeApiKey(string rawKey, out byte[] keyBytes)
    {
        keyBytes = [];

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return false;
        }

        if (!rawKey.StartsWith("nssa_", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            keyBytes = Convert.FromBase64String(rawKey[5..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<List<ServiceAccountApiKey>> ListApiKeysAsync(
        Guid serviceAccountId, CancellationToken ct = default)
    {
        return await db.ServiceAccountApiKeys
            .Where(k => k.ServiceAccountId == serviceAccountId
                && k.DeletedAt == null && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Revokes a service account API key within an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when revoked; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RevokeApiKeyAsync(Guid orgId, Guid apiKeyId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var key = await db.ServiceAccountApiKeys
            .Include(k => k.ServiceAccount)
            .FirstOrDefaultAsync(k => k.Id == apiKeyId
                && k.ServiceAccountId == serviceAccountId
                && k.ServiceAccount != null
                && k.ServiceAccount.OrgId == orgId, ct);

        if (key is null || key.RevokedAt is not null) return false;

        key.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Lists direct permission claims for a service account.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The service account direct claims.</returns>
    public async Task<List<ServiceAccountClaim>?> ListClaimsAsync(Guid orgId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var exists = await db.ServiceAccounts.AnyAsync(x => x.Id == serviceAccountId && x.OrgId == orgId && x.DeletedAt == null, ct);
        if (!exists)
        {
            return null;
        }

        return await db.ServiceAccountClaims
            .Where(x => x.ServiceAccountId == serviceAccountId)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Value)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Adds a direct permission claim to a service account.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="grant">The permission grant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="ServiceAccountClaim"/>, or <see langword="null"/>.</returns>
    public async Task<ServiceAccountClaim?> AddClaimAsync(Guid orgId, Guid serviceAccountId, PermissionGrant grant, CancellationToken ct = default)
    {
        var exists = await db.ServiceAccounts.AnyAsync(x => x.Id == serviceAccountId && x.OrgId == orgId && x.DeletedAt == null, ct);
        if (!exists)
        {
            return null;
        }

        var (type, value) = this.codec.Encode(grant);
        if (!this.codec.TryDecode(type, value, out var normalizedGrant))
        {
            return null;
        }

        var (normalizedType, normalizedValue) = this.codec.Encode(normalizedGrant);
        var claim = new ServiceAccountClaim
        {
            Id = Guid.CreateVersion7(),
            ServiceAccountId = serviceAccountId,
            Type = normalizedType,
            Value = normalizedValue,
            CreatedAt = DateTime.UtcNow,
        };

        db.ServiceAccountClaims.Add(claim);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Service account claim added: serviceAccount={ServiceAccountId} claim={ClaimId}", serviceAccountId, claim.Id);
        return claim;
    }

    /// <summary>
    /// Removes a direct permission claim from a service account.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="claimId">The claim identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveClaimAsync(Guid orgId, Guid serviceAccountId, Guid claimId, CancellationToken ct = default)
    {
        var claim = await db.ServiceAccountClaims
            .Include(x => x.ServiceAccount)
            .FirstOrDefaultAsync(x => x.Id == claimId
                && x.ServiceAccountId == serviceAccountId
                && x.ServiceAccount != null
                && x.ServiceAccount.OrgId == orgId
                && x.ServiceAccount.DeletedAt == null, ct);

        if (claim is null)
        {
            return false;
        }

        db.ServiceAccountClaims.Remove(claim);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Service account claim removed: serviceAccount={ServiceAccountId} claim={ClaimId}", serviceAccountId, claimId);
        return true;
    }

    /// <summary>
    /// Lists direct permission claims for a service account API key.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The direct API key claims, or <see langword="null"/>.</returns>
    public async Task<List<ServiceAccountApiKeyClaim>?> ListApiKeyClaimsAsync(Guid orgId, Guid serviceAccountId, Guid apiKeyId, CancellationToken ct = default)
    {
        var exists = await db.ServiceAccountApiKeys
            .Include(x => x.ServiceAccount)
            .AnyAsync(x => x.Id == apiKeyId
                && x.ServiceAccountId == serviceAccountId
                && x.ServiceAccount != null
                && x.ServiceAccount.OrgId == orgId
                && x.ServiceAccount.DeletedAt == null
                && x.RevokedAt == null
                && x.DeletedAt == null, ct);

        if (!exists)
        {
            return null;
        }

        return await db.ServiceAccountApiKeyClaims
            .Where(x => x.ServiceAccountApiKeyId == apiKeyId)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Value)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Adds a direct permission claim to a service account API key.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="grant">The permission grant.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="ServiceAccountApiKeyClaim"/>, or <see langword="null"/>.</returns>
    public async Task<ServiceAccountApiKeyClaim?> AddApiKeyClaimAsync(Guid orgId, Guid serviceAccountId, Guid apiKeyId, PermissionGrant grant, CancellationToken ct = default)
    {
        var exists = await db.ServiceAccountApiKeys
            .Include(x => x.ServiceAccount)
            .AnyAsync(x => x.Id == apiKeyId
                && x.ServiceAccountId == serviceAccountId
                && x.ServiceAccount != null
                && x.ServiceAccount.OrgId == orgId
                && x.ServiceAccount.DeletedAt == null
                && x.RevokedAt == null
                && x.DeletedAt == null, ct);

        if (!exists)
        {
            return null;
        }

        var (type, value) = this.codec.Encode(grant);
        if (!this.codec.TryDecode(type, value, out var normalizedGrant))
        {
            return null;
        }

        var (normalizedType, normalizedValue) = this.codec.Encode(normalizedGrant);
        var claim = new ServiceAccountApiKeyClaim
        {
            Id = 0,
            ServiceAccountApiKeyId = apiKeyId,
            Type = normalizedType,
            Value = normalizedValue,
            CreatedAt = DateTime.UtcNow,
        };

        db.ServiceAccountApiKeyClaims.Add(claim);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Service account API key claim added: apiKey={ApiKeyId} claim={ClaimId}", apiKeyId, claim.Id);
        return claim;
    }

    /// <summary>
    /// Removes a direct permission claim from a service account API key.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="claimId">The claim identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RemoveApiKeyClaimAsync(Guid orgId, Guid serviceAccountId, Guid apiKeyId, ulong claimId, CancellationToken ct = default)
    {
        var claim = await db.ServiceAccountApiKeyClaims
            .Include(x => x.ServiceAccountApiKey)
            .ThenInclude(x => x!.ServiceAccount)
            .FirstOrDefaultAsync(x => x.Id == claimId
                && x.ServiceAccountApiKeyId == apiKeyId
                && x.ServiceAccountApiKey != null
                && x.ServiceAccountApiKey.ServiceAccountId == serviceAccountId
                && x.ServiceAccountApiKey.ServiceAccount != null
                && x.ServiceAccountApiKey.ServiceAccount.OrgId == orgId
                && x.ServiceAccountApiKey.ServiceAccount.DeletedAt == null, ct);

        if (claim is null)
        {
            return false;
        }

        db.ServiceAccountApiKeyClaims.Remove(claim);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Service account API key claim removed: apiKey={ApiKeyId} claim={ClaimId}", apiKeyId, claimId);
        return true;
    }
}