using System.Security.Cryptography;

using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Lib.Security;

/// <summary>
/// Generates opaque tokens and computes token digests.
/// </summary>
/// <example>
/// <code>
/// var (rawToken, digest) = TokenGenerator.GenerateSessionToken();
/// </code>
/// </example>
public static class TokenGenerator
{
    private const int TokenLength = 32;

    /// <summary>
    /// Generates a browser session token and its digest.
    /// </summary>
    /// <returns>The raw token and digest bytes.</returns>
    public static (string RawToken, byte[] Digest) GenerateSessionToken()
    {
        var tokenBytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        var rawToken = Convert.ToBase64String(tokenBytes);
        var digest = Blake3.HashData(tokenBytes);

        return (rawToken, digest);
    }

    /// <summary>
    /// Generates a random reset-style token.
    /// </summary>
    /// <returns>The raw reset token.</returns>
    public static string GenerateResetToken()
    {
        var bytes = new byte[TokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Computes a digest for binary data.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <returns>The digest bytes.</returns>
    public static byte[] ComputeDigest(byte[] data)
    {
        return Blake3.HashData(data);
    }

    /// <summary>
    /// Computes a digest for text data.
    /// </summary>
    /// <param name="data">The input text.</param>
    /// <returns>The digest bytes.</returns>
    public static byte[] ComputeDigest(string data)
    {
        return Blake3.HashData(System.Text.Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Computes a Base64 digest for binary data.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <returns>The Base64 digest.</returns>
    public static string ComputeDigestBase64(byte[] data)
    {
        return Convert.ToBase64String(Blake3.HashData(data));
    }

    /// <summary>
    /// Computes a Base64 digest for text data.
    /// </summary>
    /// <param name="data">The input text.</param>
    /// <returns>The Base64 digest.</returns>
    public static string ComputeDigestBase64(string data)
    {
        return Convert.ToBase64String(Blake3.HashData(System.Text.Encoding.UTF8.GetBytes(data)));
    }
}
