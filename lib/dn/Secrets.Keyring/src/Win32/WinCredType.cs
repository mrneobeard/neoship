namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Specifies the type of a Windows credential.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var type = WinCredType.Generic;
/// </code>
/// </example>
/// </remarks>
public enum WinCredType
{
    /// <summary>
    /// A generic credential.
    /// </summary>
    Generic = 1,

    /// <summary>
    /// A domain password.
    /// </summary>
    DomainPassword = 2,

    /// <summary>
    /// A domain certificate.
    /// </summary>
    DomainCertificate = 3,

    /// <summary>
    /// A domain visible password.
    /// </summary>
    DomainVisiblePassword = 4,

    /// <summary>
    /// A generic certificate.
    /// </summary>
    GenericCertificate = 5,

    /// <summary>
    /// A domain extended credential.
    /// </summary>
    DomainExtended = 6,

    /// <summary>
    /// The maximum value for a credential type.
    /// </summary>
    Maximum = 7,

    /// <summary>
    /// The maximum value for an extended credential type.
    /// </summary>
    MaximumEx = Maximum + 1000,
}