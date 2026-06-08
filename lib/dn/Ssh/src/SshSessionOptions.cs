namespace NeoBeard.Ssh;

/// <summary>
/// Defines per-session options for managed SSH session setup.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new SshSessionOptions(
///     env: new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "override" },
///     strictEnv: true);
/// </code>
/// </example>
/// </remarks>
public sealed class SshSessionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshSessionOptions"/> class.
    /// </summary>
    /// <param name="env">Per-session environment overrides.</param>
    /// <param name="strictEnv">A value indicating whether env rejection should fail this session.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new SshSessionOptions(
    ///     env: new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "dev" },
    ///     strictEnv: false);
    /// </code>
    /// </example>
    /// </remarks>
    public SshSessionOptions(
        IReadOnlyDictionary<string, string>? env = null,
        bool? strictEnv = null)
    {
        this.Env = env ?? new Dictionary<string, string>(StringComparer.Ordinal);
        this.StrictEnv = strictEnv;
    }

    /// <summary>
    /// Gets the per-session environment overrides.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new SshSessionOptions(env: new Dictionary&lt;string, string&gt; { ["X"] = "1" });
    /// var env = options.Env;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, string> Env { get; }

    /// <summary>
    /// Gets a value indicating whether env rejection should fail this session.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new SshSessionOptions(strictEnv: true);
    /// bool? strict = options.StrictEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public bool? StrictEnv { get; }
}