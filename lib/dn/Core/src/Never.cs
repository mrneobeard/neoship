using NeoBeard.Results;

namespace NeoBeard;

/// <summary>
/// Represents the absence of any meaningful value for APIs that must remain generic-safe.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Result result = Result.Ok(Never.Value);
/// </code>
/// </example>
/// </remarks>
public readonly struct Never
{
    /// <summary>
    /// Gets the singleton marker value of <see cref="Never"/>.
    /// </summary>
    /// <value>The default <see cref="Never"/> marker.</value>
    /// <returns>The singleton <see cref="Never"/> value.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var marker = Never.Value;
    /// var result = new Result(marker);
    /// </code>
    /// </example>
    public static Never Value { get; } = default;
}