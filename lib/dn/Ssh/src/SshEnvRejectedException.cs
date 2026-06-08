namespace NeoBeard.Ssh;

/// <summary>
/// Represents an error where a remote SSH server rejected an env request in strict mode.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// try
/// {
///     throw new SshEnvRejectedException("APP_ENV");
/// }
/// catch (SshEnvRejectedException ex)
/// {
///     Console.WriteLine(ex.Name);
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class SshEnvRejectedException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshEnvRejectedException"/> class.
    /// </summary>
    /// <param name="name">The env variable name that was rejected.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new SshEnvRejectedException("LANG");
    /// Console.WriteLine(ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public SshEnvRejectedException(string name)
        : base($"The remote SSH server rejected env request '{name}'.")
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets the env variable name that was rejected by the remote side.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new SshEnvRejectedException("LANG");
    /// Console.WriteLine(ex.Name);
    /// </code>
    /// </example>
    /// </remarks>
    public string Name { get; }
}