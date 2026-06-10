namespace NeoBeard.Crypto
{
    /// <summary>
    /// Provides hashing and verification primitives for user secrets.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hasher = new PasswordHasher();
    /// var hash = hasher.Hash("password");
    /// var result = hasher.Verify("password", hash);
    /// Assert.Equal(PasswordHashResult.Success, result);
    /// </code>
    /// </example>
    /// </remarks>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a UTF-8 text password into a serializable string value.
        /// </summary>
        /// <param name="password">The plain-text password.</param>
        /// <returns>An encoded password hash string.</returns>
        string Hash(string password);

        /// <summary>
        /// Hashes raw password bytes into a serializable string value.
        /// </summary>
        /// <param name="password">The password bytes.</param>
        /// <returns>An encoded password hash string.</returns>
        string Hash(ReadOnlySpan<byte> password);

        /// <summary>
        /// Verifies a UTF-8 text password against an encoded hash value.
        /// </summary>
        /// <param name="password">The plain-text password.</param>
        /// <param name="hash">The encoded hash string.</param>
        /// <returns>
        /// Returns <see cref="PasswordHashResult.Success"/> when the password and policy match,
        /// <see cref="PasswordHashResult.NeedsRehash"/> when the password is correct but policy changed,
        /// and <see cref="PasswordHashResult.Failed"/> when verification fails.
        /// </returns>
        PasswordHashResult Verify(string password, string hash);

        /// <summary>
        /// Verifies raw password bytes against an encoded hash value.
        /// </summary>
        /// <param name="password">The password bytes.</param>
        /// <param name="hash">The encoded hash bytes.</param>
        /// <returns>
        /// Returns <see cref="PasswordHashResult.Success"/> when the password and policy match,
        /// <see cref="PasswordHashResult.NeedsRehash"/> when the password is correct but policy changed,
        /// and <see cref="PasswordHashResult.Failed"/> when verification fails.
        /// </returns>
        PasswordHashResult Verify(ReadOnlySpan<byte> password, ReadOnlySpan<byte> hash);
    }

    /// <summary>
    /// Indicates password hash verification result.
    /// </summary>
    public enum PasswordHashResult
    {
        /// <summary>
        /// The hash verification failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The password was correct and hash metadata matches current policy.
        /// </summary>
        Success,

        /// <summary>
        /// The password was correct and hash should be regenerated due to policy changes.
        /// </summary>
        NeedsRehash,
    }
}