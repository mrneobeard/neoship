namespace NeoBeard;

/// <summary>
/// Defines a contract for verifying a secret against a hash.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // ISecretVerifier verifier = ...;
/// // bool isValid = verifier.Verify("password", hash);
/// </code>
/// </example>
/// </remarks>
public interface ISecretVerifier
{
    /// <summary>
    /// Verifies the specified secret against a hash.
    /// </summary>
    /// <param name="secret">The secret to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns><see langword="true"/> if the secret matches the hash; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool isValid = verifier.Verify("my-secret".AsSpan(), hashSpan);
    /// </code>
    /// </example>
    /// </remarks>
    bool Verify(ReadOnlySpan<char> secret, ReadOnlySpan<char> hash);

    /// <summary>
    /// Verifies the specified secret against a hash.
    /// </summary>
    /// <param name="secret">The secret to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns><see langword="true"/> if the secret matches the hash; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool isValid = verifier.Verify(secretBytes, hashBytes);
    /// </code>
    /// </example>
    /// </remarks>
    bool Verify(ReadOnlySpan<byte> secret, ReadOnlySpan<byte> hash);
}