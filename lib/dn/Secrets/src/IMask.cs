using System.Diagnostics.CodeAnalysis;

namespace NeoBeard.Secrets;

/// <summary>
/// A contract for masking values.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IMask masker = SecretMasker.Default;
/// string result = masker.Mask("sensitive data");
/// </code>
/// </example>
/// </remarks>
public interface IMask
{
    /// <summary>
    /// Mask a value.
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string masked = masker.Mask("my secret password");
    /// </code>
    /// </example>
    /// </remarks>
    [return: NotNullIfNotNull("value")]
    string? Mask(string? value);

    /// <summary>
    /// Mask a value.
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ReadOnlySpan&lt;char&gt; masked = masker.Mask("secret".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    ReadOnlySpan<char> Mask(ReadOnlySpan<char> value);
}