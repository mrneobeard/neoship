using System.Diagnostics.CodeAnalysis;

using NeoBeard.Sys.Strings;

namespace NeoBeard.Secrets;

/// <summary>
/// The default implementation of <see cref="ISecretMasker"/> which can
/// mask secrets in strings and spans have been added to it.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var masker = new SecretMasker();
/// masker.Add("secret");
/// var result = masker.Mask("this is a secret");
/// </code>
/// </example>
/// </remarks>
public class SecretMasker : ISecretMasker
{
    /// <summary>
    /// Gets the default shared instance of the secret masker.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ISecretMasker masker = SecretMasker.Default;
    /// </code>
    /// </example>
    /// </remarks>
    public static ISecretMasker Default { get; } = new SecretMasker();

    /// <summary>
    /// Gets the list of secrets currently registered in the masker.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// // Accessing secrets in a derived class
    /// var count = this.Secrets.Count;
    /// </code>
    /// </example>
    /// </remarks>
    protected List<ReadOnlyMemory<char>> Secrets { get; } = new();

    /// <summary>
    /// Gets the list of derivative generators currently registered in the masker.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// // Accessing generators in a derived class
    /// var count = this.Generators.Count;
    /// </code>
    /// </example>
    /// </remarks>
    protected List<Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>>> Generators { get; } = new();

    /// <summary>
    /// Add a secret to mask.
    /// </summary>
    /// <param name="secret">The secret to mask.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new SecretMasker();
    /// masker.Add("my-password");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return;

        var memory = secret.AsMemory();

        // don't exit method as there may be new generators.
        if (!this.Secrets.Contains(memory))
            this.Secrets.Add(memory);

        foreach (var generator in this.Generators)
        {
            var next = generator(memory);

            if (!this.Secrets.Contains(next))
                this.Secrets.Add(next);
        }
    }

    /// <summary>
    /// Add a generator to create derivatives of secrets.
    /// </summary>
    /// <param name="generator">The derivative generator.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new SecretMasker();
    /// masker.AddDerivativeGenerator(s => s);
    /// </code>
    /// </example>
    /// </remarks>
    public void AddDerivativeGenerator(Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>> generator)
    {
        this.Generators.Add(generator);
    }

    /// <summary>
    /// Mask a value.
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new SecretMasker();
    /// masker.Add("secret");
    /// var masked = masker.Mask("input secret".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public ReadOnlySpan<char> Mask(ReadOnlySpan<char> value)
    {
        if (this.Secrets.Count == 0 || value.IsEmpty || value.IsWhiteSpace())
            return value;

        return value.SearchAndReplace(this.Secrets, "**********".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mask a value.
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var masker = new SecretMasker();
    /// masker.Add("secret");
    /// var masked = masker.Mask("input secret");
    /// </code>
    /// </example>
    /// </remarks>
    [return: NotNullIfNotNull("value")]
    public string? Mask(string? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value))
            return value;

        if (this.Secrets.Count == 0)
            return value;

        return value.AsSpan()
            .SearchAndReplace(
                this.Secrets,
                "**********".AsSpan(),
                StringComparison.OrdinalIgnoreCase)
            .AsString();
    }
}