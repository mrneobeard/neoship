using System.Security.Cryptography;
using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Services;

public class TokenService
{
    private const int TokenLength = 32;

    public (string RawToken, byte[] Digest) GenerateSessionToken()
    {
        var tokenBytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        var rawToken = Convert.ToBase64String(tokenBytes);
        var digest = SHA256.HashData(tokenBytes);

        return (rawToken, digest);
    }

    public string GenerateResetToken()
    {
        var bytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
