using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// A native GLib GError structure.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var error = new GError();
/// </code>
/// </example>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct GError
{
    /// <summary>
    /// The error domain.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var domain = error.Domain;
    /// </code>
    /// </example>
    /// </remarks>
    public uint Domain;

    /// <summary>
    /// The error code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = error.Code;
    /// </code>
    /// </example>
    /// </remarks>
    public int Code;

    /// <summary>
    /// A pointer to the error message.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var messagePtr = error.Message;
    /// </code>
    /// </example>
    /// </remarks>
    public IntPtr Message;
}