using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class ApiKeyStore
{
    private readonly ShipDb db;

    public ApiKeyStore(ShipDb db)
    {
        this.db = db;
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
        return await this.db.UserApiKeys
            .Where(k => k.UserId == userId && k.DeletedAt == null && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> RevokeUserApiKeyAsync(Guid apiKeyId, Guid userId, CancellationToken ct = default)
    {
        var key = await this.db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId, ct);

        if (key is null || key.RevokedAt is not null)
        {
            return false;
        }

        key.RevokedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);
        return true;
    }
}