namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Specifies the persistence of a Windows credential.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var persistence = WinCredPersistence.Enterprise;
/// </code>
/// </example>
/// </remarks>
[CLSCompliant(false)]
public enum WinCredPersistence : uint
{
    /// <summary>
    /// The credential persists for the duration of the logon session.
    /// </summary>
    Session = 1,

    /// <summary>
    /// The credential persists on the local machine.
    /// </summary>
    LocalMachine = 2,

    /// <summary>
    /// The credential persists across the enterprise.
    /// </summary>
    Enterprise = 3,
}