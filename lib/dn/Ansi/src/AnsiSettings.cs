namespace NeoBeard;

/// <summary>
/// Provides settings for ANSI terminal output configuration.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var settings = new AnsiSettings { Mode = AnsiMode.TwentyFourBit, Links = true };
/// AnsiSettings.Current = settings;
/// </code>
/// </example>
/// </remarks>
public class AnsiSettings
{
    private static Lazy<AnsiSettings> s_current = new(AnsiDetector.Detect);

    /// <summary>
    /// Gets or sets the current ANSI settings used by all Ansi methods.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// // Override settings
    /// AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };
    ///
    /// // Restore auto-detection
    /// AnsiSettings.Current = AnsiDetector.Detect();
    /// </code>
    /// </example>
    /// </remarks>
    public static AnsiSettings Current
    {
        get => s_current.Value;
        set => s_current = new Lazy<AnsiSettings>(() => value);
    }

    /// <summary>
    /// Gets or sets the ANSI color mode. Default is <see cref="AnsiMode.Auto"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings();
    /// Assert.Equal(AnsiMode.Auto, settings.Mode);
    /// settings.Mode = AnsiMode.TwentyFourBit;
    /// </code>
    /// </example>
    /// </remarks>
    public AnsiMode Mode { get; set; } = AnsiMode.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether hyperlinks are enabled. Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var settings = new AnsiSettings();
    /// Assert.True(settings.Links);
    /// settings.Links = false;
    /// </code>
    /// </example>
    /// </remarks>
    public bool Links { get; set; } = true;
}