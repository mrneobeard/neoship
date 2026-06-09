using System;

namespace NeoBeard.DotEnv;

/// <summary>
/// Represents the result of a TryParse operation.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = DotEnv.TryParseFiles(".env", ".env.local?");
/// if (result.IsOk)
/// {
///     var doc = result.Doc;
///     var value = doc.Get("KEY");
/// }
/// else
/// {
///     Console.WriteLine($"Error: {result.Error?.Message}");
/// }
/// </code>
/// </example>
/// </remarks>
public readonly struct DotEnvResult
{
    private readonly EnvDoc? doc;
    private readonly Exception? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvResult"/> struct.
    /// </summary>
    /// <param name="doc">The parsed document, or null if parsing failed.</param>
    /// <param name="error">The error that occurred, or null if parsing succeeded.</param>
    public DotEnvResult(EnvDoc? doc, Exception? error)
    {
        this.doc = doc;
        this.error = error;
    }

    /// <summary>
    /// Gets the parsed document. Null if parsing failed.
    /// </summary>
    public EnvDoc? Doc => this.doc;

    /// <summary>
    /// Gets the error that occurred during parsing. Null if parsing succeeded.
    /// </summary>
    public Exception? Error => this.error;

    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = DotEnv.TryParseFile(".env");
    /// if (result.IsOk)
    /// {
    ///     Console.WriteLine("Parsing succeeded");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsOk => this.error is null;

    /// <summary>
    /// Gets the parsed document, throwing if parsing failed.
    /// </summary>
    /// <returns>The parsed document.</returns>
    /// <exception cref="InvalidOperationException">Thrown if parsing failed.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = DotEnv.TryParseFile(".env");
    /// var doc = result.GetOrThrow();
    /// </code>
    /// </example>
    /// </remarks>
    public EnvDoc GetOrThrow()
    {
        if (this.error is not null)
        {
            throw this.error;
        }

        return this.doc!;
    }

    /// <summary>
    /// Implicitly converts an <see cref="EnvDoc"/> to a successful <see cref="DotEnvResult"/>.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// EnvDoc doc = new EnvDoc();
    /// doc.Set("KEY", "value");
    /// DotEnvResult result = doc;
    /// Assert.True(result.IsOk);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator DotEnvResult(EnvDoc doc)
        => new(doc, null);
}