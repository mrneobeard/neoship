namespace NeoBeard.Ssh;

/// <summary>
/// Defines origin-address metadata for direct-tcpip channel requests.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new SshDirectTcpOptions("127.0.0.1", 0);
/// Console.WriteLine(options.OriginHost);
/// </code>
/// </example>
/// </remarks>
public sealed class SshDirectTcpOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshDirectTcpOptions"/> class.
    /// </summary>
    /// <param name="originHost">The origin host for channel metadata.</param>
    /// <param name="originPort">The origin port for channel metadata.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new SshDirectTcpOptions("127.0.0.1", 12345);
    /// </code>
    /// </example>
    /// </remarks>
    public SshDirectTcpOptions(string originHost = "127.0.0.1", int originPort = 0)
    {
        this.OriginHost = originHost;
        this.OriginPort = originPort;
    }

    /// <summary>
    /// Gets the origin host.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string host = options.OriginHost;
    /// </code>
    /// </example>
    /// </remarks>
    public string OriginHost { get; }

    /// <summary>
    /// Gets the origin port.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// int port = options.OriginPort;
    /// </code>
    /// </example>
    /// </remarks>
    public int OriginPort { get; }
}