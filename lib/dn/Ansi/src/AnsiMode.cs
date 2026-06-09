namespace NeoBeard;

/// <summary>
/// Defines the ANSI color mode for terminal output.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var settings = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };
/// AnsiSettings.Current = settings;
/// </code>
/// </example>
/// </remarks>
public enum AnsiMode
{
    /// <summary>
    /// Automatically detect the appropriate ANSI mode based on terminal capabilities.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings { Mode = AnsiMode.Auto };
    /// </code>
    /// </example>
    /// </remarks>
    Auto = -1,

    /// <summary>
    /// No ANSI escape codes; output plain text only.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };
    /// Assert.Equal("text", Ansi.Red("text")); // No ANSI codes applied
    /// </code>
    /// </example>
    /// </remarks>
    None = 0,

    /// <summary>
    /// 3-bit color mode (8 colors).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings { Mode = AnsiMode.ThreeBit };
    /// </code>
    /// </example>
    /// </remarks>
    ThreeBit = 1,

    /// <summary>
    /// 4-bit color mode (16 colors including bright variants).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings { Mode = AnsiMode.FourBit };
    /// </code>
    /// </example>
    /// </remarks>
    FourBit = 2,

    /// <summary>
    /// 8-bit color mode (256 colors).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings { Mode = AnsiMode.EightBit };
    /// </code>
    /// </example>
    /// </remarks>
    EightBit = 4,

    /// <summary>
    /// 24-bit true color mode (16.7 million colors).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };
    /// </code>
    /// </example>
    /// </remarks>
    TwentyFourBit = 8,
}