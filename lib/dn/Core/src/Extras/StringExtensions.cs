using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using NeoBeard.Text;

namespace NeoBeard.Extras;

/// <summary>
/// Represents the StringExtensions class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var match = "Hello".ContainsFold("he");
/// </code>
/// </example>
/// </remarks>
public static class StringExtensions
{
    /// <summary>
    /// Indicates whether a specified value occurs within a string
    /// using the <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The value to seek within the source string.</param>
    /// <returns><see langword="true" /> when the string contains the given value; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var found = "Hello".ContainsFold("he");
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsFold(this string? source, string value)
    {
        if (source is null)
            return false;

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
    }

    /// <summary>
    /// Indicates whether the end of the span matches the specified value when compared using the
    /// <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The sequence to compare to the end of the source string.</param>
    /// <returns><see langword="true" /> when the string ends with the given value; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ends = "Hello".EndsWithFold("LO");
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWithFold(this string? source, string value)
    {
        if (source is null)
            return false;

        return source.EndsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Indicates whether this span is equal to the given value using
    /// the <see cref="StringComparison.OrdinalIgnoreCase"/> comparison.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="value">The value to test for equality.</param>
    /// <returns><see langword="true" /> when the span equals the value; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var eq = "Hello".AsSpan().EqualsFold("hello".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsFold(this ReadOnlySpan<char> source, ReadOnlySpan<char> value)
    {
        return source.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a string to a hyphenated format, replacing spaces, underscores,
    /// and hyphens with hyphens.
    /// </summary>
    /// <param name="value">The string to convert to hyphenated format.</param>
    /// <returns>A string in hyphenated format.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var slug = "My Value".Hyphenate();
    /// </code>
    /// </example>
    /// </remarks>
    public static string Hyphenate(this string value)
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

        return StringBuilderCache.GetStringAndRelease(builder);
    }

    /// <summary>
    /// Indicates whether or not the <see cref="string"/> value is null, empty, or white space.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <returns><see langword="true" /> if the <see cref="string"/>
    /// is null, empty, or white space; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var isBlank = " ".IsNullOrWhiteSpace();
    /// </code>
    /// </example>
    /// </remarks>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? source)
        => string.IsNullOrWhiteSpace(source);

    /// <summary>
    /// Indicates whether or not the <see cref="string"/> value is null or empty.
    /// </summary>
    /// <param name="source">The <see cref="string"/> value.</param>
    /// <returns><see langword="true" /> if the <see cref="string"/> is null or empty; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var isEmpty = string.Empty.IsNullOrEmpty();
    /// </code>
    /// </example>
    /// </remarks>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? source)
        => string.IsNullOrEmpty(source);

    /// <summary>
    /// Determines whether the start of the string matches the specified value when compared using the
    /// <see cref="StringComparison.OrdinalIgnoreCase"/>.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The sequence to compare to the start of the source string.</param>
    /// <returns><see langword="true" /> when the string starts with the given value; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var starts = "Hello".StartsWithFold("he");
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithFold(this string? source, string value)
    {
        if (source is null)
            return false;

        return source.StartsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Converts a string to a SCREAMING_SNAKE_CASE format, replacing spaces, underscores,
    /// and hyphens with underscores.
    /// </summary>
    /// <param name="value">The string to convert to SCREAMING_SNAKE_CASE.</param>
    /// <returns>A string in SCREAMING_SNAKE_CASE format.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = "My Value".ScreamingSnakeCase();
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    public static string ScreamingSnakeCase(this string value)
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

        return StringBuilderCache.GetStringAndRelease(builder);
    }

    /// <summary>
    /// Converts a string to a snake_case format, replacing spaces, underscores, and hyphens with underscores.
    /// </summary>
    /// <param name="value">The string to convert to snake_case.</param>
    /// <returns>A string in snake_case format.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = "My Value".Underscore();
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    public static string Underscore(this string value)
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

        return StringBuilderCache.GetStringAndRelease(builder);
    }
}