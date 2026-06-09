using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class ApiKeyStore
{
    private readonly ShipDb db;
    private readonly ILogger<ApiKeyStore> logger;

    public ApiKeyStore(ShipDb db, ILogger<ApiKeyStore> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public (string PlaintextKey, UserApiKey ApiKey) GenerateUserApiKey(
        Guid userId, string name, string? description, string? scopesJson, DateTime? expiresAt)
    {
        var keyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);

        var plaintextKey = "nsu_" + Convert.ToBase64String(keyBytes);
        var digest = TokenStore.ComputeDigestBase64(keyBytes);

        var apiKey = new UserApiKey(userId, name, digest, scopesJson ?? "[]", expiresAt, description);

        return (plaintextKey, apiKey);
    }

    /// <summary>
    /// Authenticates a user API key and updates its last-used timestamp.
    /// </summary>
    /// <param name="rawKey">The plaintext API key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching API key with its user loaded when valid; otherwise <see langword="null"/>.</returns>
    public async Task<UserApiKey?> AuthenticateUserApiKeyAsync(string rawKey, CancellationToken ct = default)
    {
        if (!TryDecodeUserKey(rawKey, out var keyBytes))
        {
            return null;
        }

        var digest = TokenStore.ComputeDigestBase64(keyBytes);

        var key = await db.UserApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyDigest == digest
                && k.DeletedAt == null
                && k.RevokedAt == null
                && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow), ct);

        if (key is null || key.User is null)
        {
            return null;
        }

        key.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("API key authenticated: {ApiKeyId} for user {UserId}", key.Id, key.UserId);
        return key;
    }

    private static bool TryDecodeUserKey(string rawKey, out byte[] keyBytes)
    {
        keyBytes = [];

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return false;
        }

        if (!rawKey.StartsWith("nsu_", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            keyBytes = Convert.FromBase64String(rawKey[4..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task<List<UserApiKey>> ListUserApiKeysAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserApiKeys
            .Where(k => k.UserId == userId && k.DeletedAt == null && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> RevokeUserApiKeyAsync(Guid apiKeyId, Guid userId, CancellationToken ct = default)
    {
        var key = await db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId, ct);

        if (key is null || key.RevokedAt is not null) return false;

        key.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("API key revoked: {ApiKeyId} for user {UserId}", apiKeyId, userId);
        return true;
    }
}
