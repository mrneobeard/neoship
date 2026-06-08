namespace NeoBeard.Secrets;

/// <summary>
/// The contract for a secret masker.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ISecretMasker masker = new SecretMasker();
/// masker.Add("password123");
/// string output = masker.Mask("The password is password123");
/// </code>
/// </example>
/// </remarks>
public interface ISecretMasker : IMask
{
    /// <summary>
    /// Add a secret to mask.
    /// </summary>
    /// <param name="secret">The secret to mask.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// masker.Add("api-key-value");
    /// </code>
    /// </example>
    /// </remarks>
    void Add(string? secret);

    /// <summary>
    /// Add a generator to create derivatives of secrets.
    /// </summary>
    /// <param name="generator">The derivative generator.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// masker.AddDerivativeGenerator(secret =>
    ///     Convert.ToBase64String(Encoding.UTF8.GetBytes(secret.ToString())).AsMemory());
    /// </code>
    /// </example>
    /// </remarks>
    void AddDerivativeGenerator(Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>> generator);
}