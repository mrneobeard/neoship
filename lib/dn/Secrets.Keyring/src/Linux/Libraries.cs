namespace NeoBeard.Secrets.Linux;

/// <summary>
/// Contains the names of the native libraries used by the Linux secret service.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var lib = Libraries.Glib;
/// </code>
/// </example>
/// </remarks>
internal static class Libraries
{
    /// <summary>
    /// The name of the GLib library.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var lib = Libraries.Glib;
    /// </code>
    /// </example>
    /// </remarks>
    public const string Glib = "libglib-2.0.so.0";
}