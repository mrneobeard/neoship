namespace NeoBeard.DotEnv;

/// <summary>
/// Represents the quote style used for a variable value.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var doc = new EnvDoc();
/// doc.Set("KEY1", "value", QuoteStyle.Double);
/// doc.Set("KEY2", "value", QuoteStyle.Single);
/// doc.Set("KEY3", "value", QuoteStyle.None);
/// </code>
/// </example>
/// </remarks>
public enum QuoteStyle
{
    /// <summary>
    /// No quotes around the value.
    /// </summary>
    None = 0,

    /// <summary>
    /// Single quotes: 'value'. Escape sequences are NOT processed.
    /// </summary>
    Single = 1,

    /// <summary>
    /// Double quotes: "value". Escape sequences ARE processed.
    /// </summary>
    Double = 2,

    /// <summary>
    /// Backtick quotes: `value`. Escape sequences ARE processed.
    /// </summary>
    Backtick = 3,

    /// <summary>
    /// Automatically determine quote style based on value content.
    /// Values with special characters will be quoted with double quotes.
    /// </summary>
    Auto = 4,
}