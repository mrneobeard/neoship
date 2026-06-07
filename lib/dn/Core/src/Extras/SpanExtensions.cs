using System.Runtime.CompilerServices;

using NeoBeard.Text;
using NeoBeard.Text.Extras;

namespace NeoBeard.Extras;

/// <summary>
/// Provides extension methods for working with spans, including targeted span types.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var value = "Hello".AsSpan();
/// var hasEll = value.ContainsFold("ell".AsSpan());
/// </code>
/// </example>
/// </remarks>
public static class SpanExtensions
{
    /// <summary>
    /// Converts a ReadOnlySpan of characters to a string. This works on
    /// .NET Core and .NET Standard 2.0 where ToString() is not available on <c>ReadOnlySpan&lt;char&gt;</c>
    /// in .NET Standard.
    /// </summary>
    /// <param name="span">The span to convert to a string.</param>
    /// <returns>A string representation of the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = "abc".AsSpan().AsString();
    /// </code>
    /// </example>
    /// </remarks>
    public static string AsString(this ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            return string.Empty;
        }
#if NETLEGACY
        return new string(span.ToArray());
#else
        return span.ToString();
#endif
    }

    /// <summary>
    /// Converts a ReadOnlySpan of characters to a string. This works on
    /// .NET Core and .NET Standard 2.0 where ToString() is not available on <c>ReadOnlySpan&lt;char&gt;</c>
    /// in .NET Standard.
    /// </summary>
    /// <param name="span">The span to convert to a string.</param>
    /// <param name="value">The value to check for containment.</param>
    /// <returns>A string representation of the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hasNeedle = "needle".AsSpan().ContainsFold("NEE".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsFold(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        return span.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Compares two ReadOnlySpans of characters for equality, ignoring case.
    /// </summary>
    /// <param name="span">The first span to compare.</param>
    /// <param name="other">The second span to compare.</param>
    /// <returns><c>true</c> if the spans are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var equal = "abc".AsSpan().EqualFold("ABC".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualFold(this ReadOnlySpan<char> span, ReadOnlySpan<char> other)
    {
        return span.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the ReadOnlySpan ends with the specified value, ignoring case.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <param name="value">The value to check for at the end of the span.</param>
    /// <returns><c>true</c> if the span ends with the specified value; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ends = "sample".AsSpan().EndsWithFold("PLE".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithFold(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        return span.EndsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hyphenates a string by replacing spaces, underscores, and hyphens with hyphens.
    /// </summary>
    /// <param name="value">
    /// The ReadOnlySpan of characters to hyphenate.
    /// </param>
    /// <returns>The hyphenated span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var slug = "My Value".AsSpan().Hyphenate().ToString();
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> Hyphenate(this ReadOnlySpan<char> value)
    {
        var builder = StringBuilderCache.Acquire();
        var previous = char.MinValue;
        foreach (var c in value)
        {
            if (char.IsUpper(c) && builder.Length > 0 && previous != '-')
            {
                builder.Append('-');
            }

            if (c is '_' or '-' or ' ')
            {
                builder.Append('-');
                previous = '-';
                continue;
            }

            if (!char.IsLetterOrDigit(c))
                continue;

            builder.Append(char.ToLowerInvariant(c));
            previous = c;
        }

        var span = builder.AsReadOnlySpan();
        StringBuilderCache.Release(builder);
        return span;
    }

    /// <summary>
    /// Converts a ReadOnlySpan of characters to a screaming_snake_case format, replacing spaces,
    /// underscores, and hyphens with underscores.
    /// </summary>
    /// <param name="value">The ReadOnlySpan of characters to convert to screaming_snake_case.</param>
    /// <returns>A ReadOnlySpan of characters in screaming_snake_case format.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = "My Value".AsSpan().ScreamingSnakeCase().ToString();
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> ScreamingSnakeCase(this ReadOnlySpan<char> value)
    {
        var builder = StringBuilderCache.Acquire();
        var previous = char.MinValue;
        foreach (var c in value)
        {
            if (char.IsUpper(c) && builder.Length > 0 && previous != '_')
            {
                builder.Append('_');
            }

            if (c is '-' or ' ' or '_')
            {
                builder.Append('_');
                previous = '_';
                continue;
            }

            if (!char.IsLetterOrDigit(c))
                continue;

            builder.Append(char.ToUpperInvariant(c));
            previous = c;
        }

        var span = builder.AsReadOnlySpan();
        StringBuilderCache.Release(builder);
        return span;
    }

    /// <summary>
    /// Checks if the ReadOnlySpan starts with the specified value, ignoring case.
    /// </summary>
    /// <param name="span">The span to check.</param>
    /// <param name="value">The value to check for at the start of the span.</param>
    /// <returns><c>true</c> if the span starts with the specified value; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var starts = "sample".AsSpan().StartsWithFold("SAM".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithFold(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        return span.StartsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a ReadOnlySpan of characters to a snake_case format, replacing spaces, underscores, and hyphens with underscores.
    /// </summary>
    /// <param name="value">
    /// The ReadOnlySpan of characters to convert to snake_case.
    /// </param>
    /// <returns>
    /// A ReadOnlySpan of characters in snake_case format.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var snake = "My Value".AsSpan().Underscore().ToString();
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> Underscore(this ReadOnlySpan<char> value)
    {
        var builder = StringBuilderCache.Acquire();
        var previous = char.MinValue;
        foreach (var c in value)
        {
            if (char.IsUpper(c) && builder.Length > 0 && previous != '_')
            {
                builder.Append('_');
            }

            if (c is '-' or ' ' or '_')
            {
                builder.Append('_');
                previous = '_';
                continue;
            }

            if (!char.IsLetterOrDigit(c))
                continue;

            builder.Append(char.ToLowerInvariant(c));
            previous = c;
        }

        var span = builder.AsReadOnlySpan();
        StringBuilderCache.Release(builder);
        return span;
    }
}