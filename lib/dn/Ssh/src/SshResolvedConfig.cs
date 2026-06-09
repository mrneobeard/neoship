namespace NeoBeard.Ssh;

/// <summary>
/// Represents a resolved OpenSSH host configuration snapshot.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var resolved = new SshResolvedConfig(
///     host: "prod",
///     hostname: "prod.example.com",
///     user: "deploy",
///     port: 22,
///     identityFile: ["~/.ssh/prod"],
///     sendEnv: ["LANG"],
///     setEnv: new Dictionary&lt;string, string&gt;(),
///     identitiesOnly: true,
///     proxyCommand: null,
///     options: new Dictionary&lt;string, IReadOnlyList&lt;string&gt;&gt;());
/// </code>
/// </example>
/// </remarks>
public sealed class SshResolvedConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshResolvedConfig"/> class.
    /// </summary>
    /// <param name="host">The original lookup host.</param>
    /// <param name="hostname">The resolved HostName value.</param>
    /// <param name="user">The resolved User value.</param>
    /// <param name="port">The resolved Port value.</param>
    /// <param name="identityFile">The accumulated IdentityFile values.</param>
    /// <param name="sendEnv">The accumulated SendEnv patterns.</param>
    /// <param name="setEnv">The merged SetEnv assignments.</param>
    /// <param name="identitiesOnly">The resolved IdentitiesOnly value.</param>
    /// <param name="proxyCommand">The resolved ProxyCommand value.</param>
    /// <param name="options">The full resolved options map.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var resolved = new SshResolvedConfig(
    ///     host: "prod",
    ///     hostname: null,
    ///     user: null,
    ///     port: null,
    ///     identityFile: Array.Empty&lt;string&gt;(),
    ///     sendEnv: Array.Empty&lt;string&gt;(),
    ///     setEnv: new Dictionary&lt;string, string&gt;(),
    ///     identitiesOnly: null,
    ///     proxyCommand: null,
    ///     options: new Dictionary&lt;string, IReadOnlyList&lt;string&gt;&gt;());
    /// </code>
    /// </example>
    /// </remarks>
    public SshResolvedConfig(
        string host,
        string? hostname,
        string? user,
        int? port,
        IReadOnlyList<string> identityFile,
        IReadOnlyList<string> sendEnv,
        IReadOnlyDictionary<string, string> setEnv,
        bool? identitiesOnly,
        string? proxyCommand,
        IReadOnlyDictionary<string, IReadOnlyList<string>> options)
    {
        this.Host = host;
        this.Hostname = hostname;
        this.User = user;
        this.Port = port;
        this.IdentityFile = identityFile;
        this.SendEnv = sendEnv;
        this.SetEnv = setEnv;
        this.IdentitiesOnly = identitiesOnly;
        this.ProxyCommand = proxyCommand;
        this.Options = options;
    }

    /// <summary>
    /// Gets the original lookup host value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string host = resolved.Host;
    /// </code>
    /// </example>
    /// </remarks>
    public string Host { get; }

    /// <summary>
    /// Gets the resolved HostName value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string? hostname = resolved.Hostname;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Hostname { get; }

    /// <summary>
    /// Gets the resolved User value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string? user = resolved.User;
    /// </code>
    /// </example>
    /// </remarks>
    public string? User { get; }

    /// <summary>
    /// Gets the resolved Port value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// int? port = resolved.Port;
    /// </code>
    /// </example>
    /// </remarks>
    public int? Port { get; }

    /// <summary>
    /// Gets the accumulated IdentityFile values.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; files = resolved.IdentityFile;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> IdentityFile { get; }

    /// <summary>
    /// Gets the accumulated SendEnv patterns.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; sendEnv = resolved.SendEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> SendEnv { get; }

    /// <summary>
    /// Gets the merged SetEnv key/value pairs.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyDictionary&lt;string, string&gt; setEnv = resolved.SetEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, string> SetEnv { get; }

    /// <summary>
    /// Gets the resolved IdentitiesOnly value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool? identitiesOnly = resolved.IdentitiesOnly;
    /// </code>
    /// </example>
    /// </remarks>
    public bool? IdentitiesOnly { get; }

    /// <summary>
    /// Gets the resolved ProxyCommand value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string? proxy = resolved.ProxyCommand;
    /// </code>
    /// </example>
    /// </remarks>
    public string? ProxyCommand { get; }

    /// <summary>
    /// Gets the full resolved options map.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyDictionary&lt;string, IReadOnlyList&lt;string&gt;&gt; options = resolved.Options;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Options { get; }
}