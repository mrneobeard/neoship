using System.Security.Cryptography;

using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Lib.Security;

/// <summary>
/// Hashes and verifies user passwords.
/// </summary>
/// <example>
/// <code>
/// var hash = PasswordHashing.Hash("correct horse battery staple");
/// </code>
/// </example>
public static class PasswordHashing
{
    private static readonly PasswordHasher Hasher = new(new PasswordHasherOptions
    {
        PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
        Argon2Parameters = Argon2Parameters.SecondRecommended,
    });

    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    /// <param name="password">The plaintext password.</param>
    /// <returns>The encoded password hash.</returns>
    public static string Hash(string password)
    {
        return Hasher.Hash(password);
    }

    /// <summary>
    /// Verifies a plaintext password against an encoded hash.
    /// </summary>
    /// <param name="password">The plaintext password.</param>
    /// <param name="hash">The encoded password hash.</param>
    /// <returns>The verification result and whether the password needs rehashing.</returns>
    public static (bool Success, bool NeedsRehash) Verify(string password, string hash)
    {
        var result = Hasher.Verify(password, hash);
        return (result != PasswordHashResult.Failed, result == PasswordHashResult.NeedsRehash);
    }
}
