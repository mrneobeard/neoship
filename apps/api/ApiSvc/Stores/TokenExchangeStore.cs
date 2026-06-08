using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NeoShip.ApiSvc.Stores;

public class TokenExchangeStore
{
    private readonly byte[] encryptionKey;

    public TokenExchangeStore()
    {
        this.encryptionKey = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(this.encryptionKey);
    }

    public string CreateToken(Guid userId, Guid orgId, string? scopesJson = null, int lifetimeMinutes = 5)
    {
        var payload = new TokenPayload
        {
            Sub = userId.ToString(),
            Org = orgId.ToString(),
            Scopes = scopesJson ?? "[]",
            Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Exp = DateTimeOffset.UtcNow.AddMinutes(lifetimeMinutes).ToUnixTimeSeconds(),
            Jti = Guid.CreateVersion7().ToString(),
        };

        var json = JsonSerializer.Serialize(payload);
        var plaintext = Encoding.UTF8.GetBytes(json);

        var nonce = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(this.encryptionKey, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public TokenPayload? DecodeToken(string token)
    {
        try
        {
            var data = Convert.FromBase64String(token);
            if (data.Length < 28)
            {
                return null;
            }

            var nonce = data[..12];
            var tag = data[12..28];
            var ciphertext = data[28..];

            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(this.encryptionKey, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            var json = Encoding.UTF8.GetString(plaintext);
            var payload = JsonSerializer.Deserialize<TokenPayload>(json);

            if (payload is not null && payload.Exp > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return payload;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

public class TokenPayload
{
    public string Sub { get; set; } = string.Empty;
    public string Org { get; set; } = string.Empty;
    public string Scopes { get; set; } = "[]";
    public long Iat { get; set; }
    public long Exp { get; set; }
    public string Jti { get; set; } = string.Empty;
}