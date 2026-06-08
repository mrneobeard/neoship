namespace NeoBeard.Secrets;

/// <summary>
/// Defines a contract for hashing secrets.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // ISecretHasher hasher = ...;
/// // var hash = hasher.ComputeHash("password");
/// </code>
/// </example>
/// </remarks>
public interface ISecretHasher
{
    /// <summary>
    /// Computes the hash of the specified secret.
    /// </summary>
    /// <param name="secret">The secret to hash.</param>
    /// <returns>The computed hash as a <see cref="ReadOnlySpan{T}"/> of bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = hasher.ComputeHash("my-secret".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    ReadOnlySpan<byte> ComputeHash(ReadOnlySpan<char> secret);

    /// <summary>
    /// Computes the hash of the specified secret.
    /// </summary>
    /// <param name="secret">The secret to hash.</param>
    /// <returns>The computed hash as a <see cref="ReadOnlySpan{T}"/> of bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = hasher.ComputeHash(secretBytes);
    /// </code>
    /// </example>
    /// </remarks>
    ReadOnlySpan<byte> ComputeHash(ReadOnlySpan<byte> secret);
}