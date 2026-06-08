namespace NeoBeard.Sys.Strings;

/// <summary>
/// Struct <see cref="SearchSpan"/> represents the location of a span of text
/// within a larger body of text.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var span = new SearchSpan(0, 5);
/// </code>
/// </example>
/// </remarks>
#if DFX_CORE
public
#else
internal
#endif
    readonly struct SearchSpan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSpan"/> struct.
    /// </summary>
    /// <param name="start">The start index of the span.</param>
    /// <param name="length">The length of the span.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var span = new SearchSpan(10, 20);
    /// </code>
    /// </example>
    /// </remarks>
    public SearchSpan(int start, int length)
    {
        this.Start = start;
        this.End = start + length;
        this.Length = length;
        this.IsEmpty = length == 0;
    }

    /// <summary>
    /// Gets a value indicating whether the span is empty.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var span = new SearchSpan(0, 0);
    /// bool empty = span.IsEmpty;
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsEmpty { get; }

    /// <summary>
    /// Gets the length of the span.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var span = new SearchSpan(0, 10);
    /// int len = span.Length;
    /// </code>
    /// </example>
    /// </remarks>
    public int Length { get; }

    /// <summary>
    /// Gets the start index of the span.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var span = new SearchSpan(5, 5);
    /// int start = span.Start;
    /// </code>
    /// </example>
    /// </remarks>
    public int Start { get; }

    /// <summary>
    /// Gets the end index of the span.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var span = new SearchSpan(5, 5);
    /// int end = span.End;
    /// </code>
    /// </example>
    /// </remarks>
    public int End { get; }
}