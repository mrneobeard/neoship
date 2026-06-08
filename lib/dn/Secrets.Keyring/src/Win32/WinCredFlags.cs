using System.Diagnostics.CodeAnalysis;

namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Specifies the flags for a Windows credential.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var flags = WinCredFlags.PromptNow;
/// </code>
/// </example>
/// </remarks>
[SuppressMessage("Minor Code Smell", "S2344:Enumeration type names should not have \"Flags\" or \"Enum\" suffixes")]
public enum WinCredFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var flags = WinCredFlags.None;
    /// </code>
    /// </example>
    /// </remarks>
    None = 0x0,

    /// <summary>
    /// Prompt the user for credentials.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var flags = WinCredFlags.PromptNow;
    /// </code>
    /// </example>
    /// </remarks>
    PromptNow = 0x2,

    /// <summary>
    /// Use the target as the username.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var flags = WinCredFlags.UsernameTarget;
    /// </code>
    /// </example>
    /// </remarks>
    UsernameTarget = 0x4,
}