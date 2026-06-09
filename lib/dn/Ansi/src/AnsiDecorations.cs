namespace NeoBeard;

/// <summary>
/// Defines ANSI text decoration flags that can be combined using bitwise operations.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var decorations = AnsiDecorations.Bold | AnsiDecorations.Italic;
/// Assert.True(decorations.HasFlag(AnsiDecorations.Bold));
/// </code>
/// </example>
/// </remarks>
[Flags]
public enum AnsiDecorations
{
    /// <summary>
    /// No text decorations.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var none = AnsiDecorations.None;
    /// Assert.Equal(0, (int)none);
    /// </code>
    /// </example>
    /// </remarks>
    None = 0,

    /// <summary>
    /// Bold text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var bold = AnsiDecorations.Bold;
    /// Assert.Equal(1, (int)bold);
    /// </code>
    /// </example>
    /// </remarks>
    Bold = 1,

    /// <summary>
    /// Dim text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dim = AnsiDecorations.Dim;
    /// Assert.Equal(2, (int)dim);
    /// </code>
    /// </example>
    /// </remarks>
    Dim = 2,

    /// <summary>
    /// Italic text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var italic = AnsiDecorations.Italic;
    /// Assert.Equal(4, (int)italic);
    /// </code>
    /// </example>
    /// </remarks>
    Italic = 4,

    /// <summary>
    /// Underline text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var underline = AnsiDecorations.Underline;
    /// Assert.Equal(8, (int)underline);
    /// </code>
    /// </example>
    /// </remarks>
    Underline = 8,

    /// <summary>
    /// Inverse text decoration (swaps foreground and background colors).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inverse = AnsiDecorations.Inverse;
    /// Assert.Equal(16, (int)inverse);
    /// </code>
    /// </example>
    /// </remarks>
    Inverse = 16,

    /// <summary>
    /// Hidden text decoration (text is not displayed).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hidden = AnsiDecorations.Hidden;
    /// Assert.Equal(32, (int)hidden);
    /// </code>
    /// </example>
    /// </remarks>
    Hidden = 32,

    /// <summary>
    /// Slow blink text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var slowBlink = AnsiDecorations.SlowBlink;
    /// Assert.Equal(64, (int)slowBlink);
    /// </code>
    /// </example>
    /// </remarks>
    SlowBlink = 64,

    /// <summary>
    /// Rapid blink text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var rapidBlink = AnsiDecorations.RapidBlink;
    /// Assert.Equal(128, (int)rapidBlink);
    /// </code>
    /// </example>
    /// </remarks>
    RapidBlink = 128,

    /// <summary>
    /// Strikethrough text decoration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var strike = AnsiDecorations.Strikethrough;
    /// Assert.Equal(256, (int)strike);
    /// </code>
    /// </example>
    /// </remarks>
    Strikethrough = 256,
}