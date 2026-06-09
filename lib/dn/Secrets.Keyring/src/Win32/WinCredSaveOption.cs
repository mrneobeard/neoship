namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Specifies the save options for a Windows credential.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var option = WinCredSaveOption.Selected;
/// </code>
/// </example>
/// </remarks>
public enum WinCredSaveOption
{
    /// <summary>
    /// The save option is unselected.
    /// </summary>
    Unselected,

    /// <summary>
    /// The save option is selected.
    /// </summary>
    Selected,

    /// <summary>
    /// The save option is hidden.
    /// </summary>
    Hidden,
}