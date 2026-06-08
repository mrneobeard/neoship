using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// Represents a secret item in the Linux secret service.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var item = new SecretItem();
/// </code>
/// </example>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct SecretItem
{
    /// <summary>
    /// The label of the secret item.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var label = item.Label;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.LPStr)]
    public string Label;

    /// <summary>
    /// A pointer to the secret data.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretPtr = item.Secret;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Secret;

    /// <summary>
    /// A value indicating whether the item is locked.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var isLocked = item.Locked;
    /// </code>
    /// </example>
    /// </remarks>
    [MarshalAs(UnmanagedType.I1)]
    public bool Locked;

    /// <summary>
    /// The creation time as a Unix timestamp.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var created = item.Created;
    /// </code>
    /// </example>
    /// </remarks>
    public long Created;

    /// <summary>
    /// The last modification time as a Unix timestamp.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var modified = item.Modified;
    /// </code>
    /// </example>
    /// </remarks>
    public long Modified;

    /// <summary>
    /// A pointer to the item's attributes (GHashTable).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var attributesPtr = item.Attributes;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Attributes;
}