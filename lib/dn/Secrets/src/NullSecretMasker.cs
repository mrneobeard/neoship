using System.Diagnostics.CodeAnalysis;

namespace NeoBeard.Secrets;

/// <summary>
/// A secret masker that does nothing.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ISecretMasker masker = new NullSecretMasker();
/// string result = masker.Mask("this will not be masked");
/// </code>
/// </example>
/// </remarks>
public class NullSecretMasker : ISecretMasker
{
    /// <summary>
    /// Gets the default instance of the <see cref="NullSecretMasker"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ISecretMasker masker = NullSecretMasker.Default;
    /// </code>
    /// </example>
    /// </remarks>
    public static ISecretMasker Default { get; } = new NullSecretMasker();

    /// <summary>
    /// Add a secret to mask (does nothing).
    /// </summary>
    /// <param name="secret">The secret to mask.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new NullSecretMasker();
    /// masker.Add("secret");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string? secret)
    {
        // noop
    }

    /// <summary>
    /// Add a generator to create derivatives of secrets (does nothing).
    /// </summary>
    /// <param name="generator">The derivative generator.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new NullSecretMasker();
    /// masker.AddDerivativeGenerator(s => s);
    /// </code>
    /// </example>
    /// </remarks>
    public void AddDerivativeGenerator(Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>> generator)
    {
        // noop
    }

    /// <summary>
    /// Mask a value (returns the value as-is).
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The original value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new NullSecretMasker();
    /// string masked = masker.Mask("original");
    /// </code>
    /// </example>
    /// </remarks>
    public string? Mask([NotNullIfNotNull("value")] string? value)
    {
        return value;
    }

    /// <summary>
    /// Mask a value (returns the value as-is).
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The original value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new NullSecretMasker();
    /// var masked = masker.Mask("original".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public ReadOnlySpan<char> Mask(ReadOnlySpan<char> value)
    {
        return value;
    }
}