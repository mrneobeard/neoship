using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace NeoBeard.Sys.Strings;

/// <summary>
/// Provides extension methods for <see cref="string"/> and <see cref="ReadOnlySpan{T}"/> for searching and replacing text.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// string text = "Hello world";
/// var hits = text.Search("world");
/// string result = text.Replace(hits, "everyone");
/// </code>
/// </example>
/// </remarks>
internal static partial class StringExtensions
{
    /// <summary>
    /// Searches the string for all occurrences of the specified value using the
    /// <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "abcabc".Search("abc");
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this string? source, string value)
        => Search(source, value, StringComparison.Ordinal);

    /// <summary>
    /// Searches the string for all occurrences of the specified value.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "Abcabc".Search("abc", StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this string? source, string value, StringComparison comparison)
    {
        if (source is null)
            return Array.Empty<SearchSpan>();

        return Search(source.AsSpan(), value.AsSpan(), comparison);
    }

    /// <summary>
    /// Searches the span for all occurrences of the specified value using the
    /// <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="value">The value to find.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "abcabc".AsSpan().Search("abc".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        => Search(source, value, StringComparison.Ordinal);

    /// <summary>
    /// Searches the span for all occurrences of the specified value.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "Abcabc".AsSpan().Search("abc".AsSpan(), StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(
        this ReadOnlySpan<char> source,
        ReadOnlySpan<char> value,
        StringComparison comparison)
    {
        if (source.IsEmpty || value.IsEmpty)
            return Array.Empty<SearchSpan>();

        var positions = new List<SearchSpan>();
        var span = source;
        var searchToken = value;
        int end = 0;
        while (!span.IsEmpty)
        {
            var index = span.IndexOf(searchToken, comparison);
            if (index == -1)
            {
                return positions;
            }

            int start = end + index,
                nextIndex = index + searchToken.Length;

            end = start + searchToken.Length;

            // span.IndexOf doesn't include a startingIndex parameter, so
            // shrink the search window so we can search for additional positions.
            positions.Add(new SearchSpan(start, searchToken.Length));
            span = span.Slice(nextIndex);
        }

        return positions;
    }

    /// <summary>
    /// Searches the string for all occurrences of multiple values using the
    /// <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="values">The values to find.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "abc def ghi".Search(new[] { "abc", "def" });
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this string? source, IEnumerable<string> values)
        => Search(source, values, StringComparison.Ordinal);

    /// <summary>
    /// Searches the string for all occurrences of the specified values.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="values">The values to find.</param>
    /// <param name="comparison">The comparison to use to match on the values.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "ABC def".Search(new[] { "abc", "def" }, StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this string? source, IEnumerable<string> values, StringComparison comparison)
    {
        if (source is null)
            return Array.Empty<SearchSpan>();

        return Search(source.AsSpan(), values.Select(o => o.AsMemory()), comparison);
    }

    /// <summary>
    /// Searches the span for all occurrences of the specified values using the
    /// <see cref="StringComparison.CurrentCulture"/> comparison.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="values">The values to find.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "abc def".AsSpan().Search(new[] { "abc".AsMemory(), "def".AsMemory() });
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(this ReadOnlySpan<char> source, IEnumerable<ReadOnlyMemory<char>> values)
        => Search(source, values, StringComparison.CurrentCulture);

    /// <summary>
    /// Searches the span for all occurrences of the specified values.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="values">The values to find.</param>
    /// <param name="comparison">The comparison to use to match on the values.</param>
    /// <returns>A <see cref="T:System.Collections.Generic.IReadOnlyList&lt;SearchSpan&gt;"/> that holds the location for each match found in the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hits = "abc def".AsSpan().Search(new[] { "abc".AsMemory(), "def".AsMemory() }, StringComparison.Ordinal);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<SearchSpan> Search(
        this ReadOnlySpan<char> source,
        IEnumerable<ReadOnlyMemory<char>> values,
        StringComparison comparison)
    {
        if (source.IsEmpty)
            return Array.Empty<SearchSpan>();

        // look at the longer strings first as the other searches may
        // be a subset of the longer strings
        values = values.OrderByDescending(o => o.Length);
        var positions = new List<SearchSpan>();
        foreach (var search in values)
        {
            var nextPositions = Search(source, search.Span, comparison);
            foreach (var pos in nextPositions)
            {
                // don't add a position if there is already another hit
                // with the same starting index.
                if (positions.TrueForAll(o => o.Start != pos.Start))
                    positions.Add(pos);
            }
        }

        // always order by the starting position.
        positions.Sort(
            (left, right)
                => left.Start.CompareTo(right.Start));

        return positions;
    }

    /// <summary>
    /// Returns a new string with all occurrences of the specified hits updated with specified replacement value.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="hits">The list of locations to replace.</param>
    /// <param name="replacement">The value to replace the segments of the string.</param>
    /// <returns>A new string that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc def".Replace(new[] { new SearchSpan(0, 3) }, "xyz");
    /// </code>
    /// </example>
    /// </remarks>
    public static string Replace(this string? source, IReadOnlyList<SearchSpan> hits, string replacement)
    {
        if (hits.Count == 0 || source.IsNullOrEmpty())
            return source ?? string.Empty;

        var sb = new StringBuilder();

        int startIndex = 0;
        var target = source.AsSpan();
#if NETLEGACY
        foreach (var position in hits)
        {
            var length = position.Start - startIndex;
            var precedingText = target.Slice(startIndex, length);
            if (!precedingText.IsEmpty)
                sb.Append(precedingText.ToArray());

            sb.Append(replacement);

            startIndex = position.End;
        }

        var remainingText = target.Slice(startIndex);
        if (!remainingText.IsEmpty)
            sb.Append(remainingText.ToArray());

        var result = sb.ToString();
        sb.Clear();
        return result;
#else
        foreach (var position in hits)
        {
            var length = position.Start - startIndex;
            var precedingText = target.Slice(startIndex, length);
            if (!precedingText.IsEmpty)
                sb.Append(precedingText);

            sb.Append(replacement);

            startIndex = position.End;
        }

        var remainingText = target[startIndex..];
        if (!remainingText.IsEmpty)
            sb.Append(remainingText);

        var result = sb.ToString();
        sb.Clear();
        return result;
#endif
    }

    /// <summary>
    /// Returns an updated span with all occurrences of the specified hits updated with specified replacement value.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="hits">The list of locations to replace.</param>
    /// <param name="replacement">The value to replace the segments of the string.</param>
    /// <returns>A new span that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc".AsSpan().Replace(new[] { new SearchSpan(0, 3) }, "xyz".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> Replace(
        this ReadOnlySpan<char> source,
        IReadOnlyList<SearchSpan> hits,
        ReadOnlySpan<char> replacement)
    {
        if (hits.Count == 0 || source.IsEmpty)
            return source;

        var sb = new StringBuilder();

        int startIndex = 0;
#if NETLEGACY
        foreach (var position in hits)
        {
            var length = position.Start - startIndex;
            var precedingText = source.Slice(startIndex, length);

            if (!precedingText.IsEmpty)
                sb.Append(precedingText.ToArray());

            sb.Append(replacement.ToArray());
            startIndex = position.End;
        }

        var remainingText = source.Slice(startIndex);
        if (!remainingText.IsEmpty)
            sb.Append(remainingText.ToArray());

        var result = new char[sb.Length];
        sb.CopyTo(0, result, 0, sb.Length);
        sb.Clear();
        return result;
#else
        foreach (var position in hits)
        {
            var length = position.Start - startIndex;
            var precedingText = source.Slice(startIndex, length);
            if (!precedingText.IsEmpty)
                sb.Append(precedingText);

            sb.Append(replacement);
            startIndex = position.End;
        }

        var remainingText = source[startIndex..];
        if (!remainingText.IsEmpty)
            sb.Append(remainingText);

        var result = new char[sb.Length];
        sb.CopyTo(0, result, 0, sb.Length);
        sb.Clear();
        return result;
#endif
    }

    /// <summary>
    /// Searches the span for all occurrences of the specified value and returns a new span with replaced values
    /// using the <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="replacement">The value to replace segments of the span.</param>
    /// <returns>A new span that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc".AsSpan().SearchAndReplace("a".AsSpan(), "x".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> SearchAndReplace(
        this ReadOnlySpan<char> source,
        ReadOnlySpan<char> value,
        ReadOnlySpan<char> replacement)
        => SearchAndReplace(
            source,
            value,
            replacement,
            StringComparison.Ordinal);

    /// <summary>
    /// Searches the span for all occurrences of the specified value and returns a new span with replaced values.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="replacement">The value to replace segments of the span.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A new span that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "ABC".AsSpan().SearchAndReplace("a".AsSpan(), "x".AsSpan(), StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> SearchAndReplace(
        this ReadOnlySpan<char> source,
        ReadOnlySpan<char> value,
        ReadOnlySpan<char> replacement,
        StringComparison comparison)
    {
        var positions = Search(source, value, comparison);
        if (positions.Count == 0)
            return source;

        return Replace(source, positions, replacement);
    }

    /// <summary>
    /// Searches the string for all occurrences of the specified value and returns a new string with replaced values
    /// using the <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="replacement">The value to replace segments of the string.</param>
    /// <returns>A new string that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc".SearchAndReplace("a", "x");
    /// </code>
    /// </example>
    /// </remarks>
    public static string SearchAndReplace(
        this string? source,
        string value,
        string replacement)
        => SearchAndReplace(
            source,
            value,
            replacement,
            StringComparison.Ordinal);

    /// <summary>
    /// Searches the string for all occurrences of the specified value and returns a new string with replaced values.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="value">The value to find.</param>
    /// <param name="replacement">The value to replace segments of the string.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A new string that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "ABC".SearchAndReplace("a", "x", StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static string SearchAndReplace(
        this string? source,
        string value,
        string replacement,
        StringComparison comparison)
    {
        var positions = Search(source, value, comparison);
        if (positions.Count == 0)
            return source ?? string.Empty;

        return Replace(source, positions, replacement);
    }

    /// <summary>
    /// Searches the span for all occurrences of multiple values and returns a new span with replaced values.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <param name="values">The values to find.</param>
    /// <param name="replacement">The value to replace segments of the span.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A new span that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc def".AsSpan().SearchAndReplace(new[] { "abc".AsMemory() }, "xyz".AsSpan(), StringComparison.Ordinal);
    /// </code>
    /// </example>
    /// </remarks>
    public static ReadOnlySpan<char> SearchAndReplace(
        this ReadOnlySpan<char> source,
        IEnumerable<ReadOnlyMemory<char>> values,
        ReadOnlySpan<char> replacement,
        StringComparison comparison)
    {
        var positions = Search(source, values, comparison);
        if (positions.Count == 0)
            return source;

        return Replace(source, positions, replacement);
    }

    /// <summary>
    /// Searches the string for all occurrences of multiple values and returns a new string with replaced values
    /// using the <see cref="StringComparison.Ordinal"/> comparison.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="values">The values to find.</param>
    /// <param name="replacement">The value to replace segments of the string.</param>
    /// <returns>A new string that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "abc def".SearchAndReplace(new[] { "abc", "def" }, "xyz");
    /// </code>
    /// </example>
    /// </remarks>
    public static string SearchAndReplace(
        this string? source,
        IEnumerable<string> values,
        string replacement)
        => SearchAndReplace(
            source,
            values,
            replacement,
            StringComparison.Ordinal);

    /// <summary>
    /// Searches the string for all occurrences of multiple values and returns a new string with replaced values.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="values">The values to find.</param>
    /// <param name="replacement">The value to replace segments of the string.</param>
    /// <param name="comparison">The comparison to use to match on the value.</param>
    /// <returns>A new string that has the replaced values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = "ABC def".SearchAndReplace(new[] { "abc", "def" }, "xyz", StringComparison.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    /// </remarks>
    public static string SearchAndReplace(
        this string? source,
        IEnumerable<string> values,
        string replacement,
        StringComparison comparison)
    {
        var positions = Search(source, values, comparison);
        if (positions.Count == 0)
            return source ?? string.Empty;

        return Replace(source, positions, replacement);
    }

    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> of characters to a <see cref="string"/>.
    /// </summary>
    /// <param name="source">The source span.</param>
    /// <returns>A new string containing the characters from the span.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string s = "abc".AsSpan().AsString();
    /// </code>
    /// </example>
    /// </remarks>
    public static string AsString(this ReadOnlySpan<char> source)
#if NETLEGACY
        => new string(source.ToArray());
#else
        => source.ToString();
#endif

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
    /// bool isWS = "  ".IsNullOrWhiteSpace();
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    /// bool isNullOrEmpty = "".IsNullOrEmpty();
    /// </code>
    /// </example>
    /// </remarks>
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? source)
        => string.IsNullOrEmpty(source);
}