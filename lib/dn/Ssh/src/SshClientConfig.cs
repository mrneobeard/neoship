namespace NeoBeard.Ssh;

/// <summary>
/// Defines client-level SSH options used by managed session behavior and authentication.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var config = new SshClientConfig(
///     sendEnv: ["LANG", "LC_*"],
///     setEnv: new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "prod" },
///     strictEnv: false,
///     identityFiles: ["~/.ssh/id_rsa"]);
/// </code>
/// </example>
/// </remarks>
public sealed class SshClientConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshClientConfig"/> class.
    /// </summary>
    /// <param name="sendEnv">The OpenSSH-style SendEnv patterns.</param>
    /// <param name="setEnv">The fixed SetEnv values.</param>
    /// <param name="strictEnv">A value indicating whether env rejection should fail the session.</param>
    /// <param name="authMethod">The preferred authentication method.</param>
    /// <param name="privateKey">Inline private key text (PEM or OpenSSH).</param>
    /// <param name="identityFiles">Identity file paths to try in order.</param>
    /// <param name="privateKeyPassphrase">The passphrase used to decrypt encrypted private keys.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(
    ///     sendEnv: ["APP_*", "-APP_TOKEN"],
    ///     setEnv: new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "prod" },
    ///     strictEnv: true,
    ///     authMethod: SshAuthMethod.PublicKey,
    ///     identityFiles: ["~/.ssh/id_ed25519"]);
    /// </code>
    /// </example>
    /// </remarks>
    public SshClientConfig(
        IReadOnlyList<string>? sendEnv = null,
        IReadOnlyDictionary<string, string>? setEnv = null,
        bool strictEnv = false,
        SshAuthMethod? authMethod = null,
        string? privateKey = null,
        IReadOnlyList<string>? identityFiles = null,
        string? privateKeyPassphrase = null)
    {
        this.SendEnv = sendEnv ?? Array.Empty<string>();
        this.SetEnv = setEnv ?? new Dictionary<string, string>(StringComparer.Ordinal);
        this.StrictEnv = strictEnv;
        this.AuthMethod = authMethod;
        this.PrivateKey = privateKey;
        this.IdentityFiles = identityFiles ?? Array.Empty<string>();
        this.PrivateKeyPassphrase = privateKeyPassphrase;
    }

    /// <summary>
    /// Gets the OpenSSH-style SendEnv patterns.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(sendEnv: ["LANG", "LC_*"]);
    /// var patterns = config.SendEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> SendEnv { get; }

    /// <summary>
    /// Gets the fixed SetEnv values applied to each session.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(
    ///     setEnv: new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "prod" });
    /// var values = config.SetEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, string> SetEnv { get; }

    /// <summary>
    /// Gets a value indicating whether rejected env requests should fail the session.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(strictEnv: true);
    /// bool strict = config.StrictEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public bool StrictEnv { get; }

    /// <summary>
    /// Gets the preferred authentication method.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(authMethod: SshAuthMethod.PublicKey);
    /// SshAuthMethod? method = config.AuthMethod;
    /// </code>
    /// </example>
    /// </remarks>
    public SshAuthMethod? AuthMethod { get; }

    /// <summary>
    /// Gets the inline private key text.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(privateKey: "-----BEGIN PRIVATE KEY-----...");
    /// string? key = config.PrivateKey;
    /// </code>
    /// </example>
    /// </remarks>
    public string? PrivateKey { get; }

    /// <summary>
    /// Gets identity file paths to try in order.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(identityFiles: ["~/.ssh/id_rsa", "~/.ssh/id_ed25519"]);
    /// IReadOnlyList&lt;string&gt; files = config.IdentityFiles;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> IdentityFiles { get; }

    /// <summary>
    /// Gets the passphrase used for encrypted private keys.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new SshClientConfig(privateKeyPassphrase: "secret");
    /// string? passphrase = config.PrivateKeyPassphrase;
    /// </code>
    /// </example>
    /// </remarks>
    public string? PrivateKeyPassphrase { get; }
}