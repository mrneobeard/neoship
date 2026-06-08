using System.Security.Cryptography;

using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Stores;

public class TokenStore
{
    private const int TokenLength = 32;

    public (string RawToken, byte[] Digest) GenerateSessionToken()
    {
        var tokenBytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        var rawToken = Convert.ToBase64String(tokenBytes);
        var digest = Blake3.HashData(tokenBytes);

        return (rawToken, digest);
    }

    public string GenerateResetToken()
    {
        var bytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static byte[] ComputeDigest(byte[] data)
    {
        return Blake3.HashData(data);
    }

    public static byte[] ComputeDigest(string data)
    {
        return Blake3.HashData(System.Text.Encoding.UTF8.GetBytes(data));
    }

    public static string ComputeDigestBase64(byte[] data)
    {
        return Convert.ToBase64String(Blake3.HashData(data));
    }

    public static string ComputeDigestBase64(string data)
    {
        return Convert.ToBase64String(Blake3.HashData(System.Text.Encoding.UTF8.GetBytes(data)));
    }
}