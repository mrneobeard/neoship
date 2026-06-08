namespace NeoBeard.Ssh;

/// <summary>
/// Represents a parsed OpenSSH Host block with raw option values.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var host = new SshConfigHost(
///     ["prod"],
///     new Dictionary&lt;string, IReadOnlyList&lt;string&gt;&gt;());
/// Console.WriteLine(host.Patterns.Count);
/// </code>
/// </example>
/// </remarks>
public sealed class SshConfigHost
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshConfigHost"/> class.
    /// </summary>
    /// <param name="patterns">The host patterns for this block.</param>
    /// <param name="options">The parsed options for this block.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var host = new SshConfigHost(
    ///     ["*"],
    ///     new Dictionary&lt;string, IReadOnlyList&lt;string&gt;&gt;());
    /// </code>
    /// </example>
    /// </remarks>
    public SshConfigHost(
        IReadOnlyList<string> patterns,
        IReadOnlyDictionary<string, IReadOnlyList<string>> options)
    {
        this.Patterns = patterns;
        this.Options = options;
    }

    /// <summary>
    /// Gets the host patterns declared by the Host directive.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; patterns = host.Patterns;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> Patterns { get; }

    /// <summary>
    /// Gets the option map keyed by lowercase option name.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyDictionary&lt;string, IReadOnlyList&lt;string&gt;&gt; options = host.Options;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; }
}