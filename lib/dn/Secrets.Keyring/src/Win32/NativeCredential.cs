using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Represents the native Windows credential structure.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var cred = new NativeCredential();
/// </code>
/// </example>
/// </remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeCredential
{
    /// <summary>
    /// The flags for the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var flags = cred.Flags;
    /// </code>
    /// </example>
    /// </remarks>
    public int Flags;

    /// <summary>
    /// The type of the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var type = cred.Type;
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredType Type;

    /// <summary>
    /// The target name of the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var target = cred.TargetName;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string TargetName;

    /// <summary>
    /// The comment for the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var comment = cred.Comment;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Comment;

    /// <summary>
    /// The time the credential was last written.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var lastWritten = cred.LastWritten;
    /// </code>
    /// </example>
    /// </remarks>
    public FILETIME LastWritten;

    /// <summary>
    /// The size of the credential blob in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var size = cred.CredentialBlobSize;
    /// </code>
    /// </example>
    /// </remarks>
    public int CredentialBlobSize;

    /// <summary>
    /// A pointer to the credential blob.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var blob = cred.CredentialBlob;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr CredentialBlob;

    /// <summary>
    /// The persistence level of the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var persist = cred.Persist;
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredPersistence Persist;

    /// <summary>
    /// The number of attributes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var count = cred.AttributeCount;
    /// </code>
    /// </example>
    /// </remarks>
    public int AttributeCount;

    /// <summary>
    /// A pointer to the attributes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var attributes = cred.Attributes;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Attributes;

    /// <summary>
    /// The target alias.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alias = cred.TargetAlias;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string TargetAlias;

    /// <summary>
    /// The user name associated with the credential.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var user = cred.UserName;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string UserName;
}