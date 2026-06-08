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