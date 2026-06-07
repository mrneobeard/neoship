using System.Text;

namespace NeoBeard.Text.Extras;

/// <summary>
/// Represents the EncodingMembers class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class EncodingMembers
{
    extension(Encoding)
    {
        /// <summary>
        ///  Gets a UTF-8 encoding without a byte order mark (BOM).
        /// </summary>
        public static Encoding UTF8NoBom => s_Utf8NoBom;
    }

    private static readonly Encoding s_Utf8NoBom = new UTF8Encoding(false, true);
}