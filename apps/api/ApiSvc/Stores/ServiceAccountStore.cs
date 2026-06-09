using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class ServiceAccountStore
{
    private readonly ShipDb db;
    private readonly ILogger<ServiceAccountStore> logger;

    public ServiceAccountStore(ShipDb db, ILogger<ServiceAccountStore> logger)
    {
        this.db = db;
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

    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var key = await db.ServiceAccountApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.ServiceAccountId == serviceAccountId, ct);

        if (key is null || key.RevokedAt is not null) return false;

        key.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
