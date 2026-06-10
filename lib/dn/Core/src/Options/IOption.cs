namespace NeoBeard.Options;

/// <summary>
/// Represents a discriminated optional value.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IOption option = Option&lt;int&gt;.Some(12);
/// if (option.HasValue) { /* ... */ }
/// </code>
/// </example>
/// </remarks>
public interface IOption
{
    /// <summary>
    /// Gets a value indicating whether the option contains a value.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when a value is present; otherwise <see langword="false"/>.
    /// </value>
    bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether the option has no value.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when no value is present; otherwise <see langword="false"/>.
    /// </value>
    bool HasNoValue { get; }

    /// <summary>
    /// Gets the raw optional value, or the corresponding none marker.
    /// </summary>
    /// <value>
    /// A value when <see cref="HasValue"/> is <see langword="true"/>; otherwise a none marker.
    /// </value>
    /// <example>
    /// <code lang="csharp">
    /// IOption option = None.Value;
    /// var payload = option.Value;
    /// </code>
    /// </example>
    object? Value { get; }
}