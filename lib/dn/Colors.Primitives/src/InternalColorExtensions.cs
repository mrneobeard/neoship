using System.Runtime.CompilerServices;

namespace NeoBeard.Colors;

/// <summary>
/// Internal helper extensions for color value parsing utilities.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ReadOnlySpan<char> chars = "  FF80 ".AsSpan();
/// var text = chars.AsString();
/// Assert.Equal("FF80", text);
/// </code>
/// </example>
/// </remarks>
internal static class InternalColorExtensions
{
    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> of characters to a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The span to convert.</param>
    /// <returns>The string representation of <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string AsString(this ReadOnlySpan<char> value)
#if NETLEGACY
        => new(value.ToArray());
#else
        => new(value);
#endif
}
