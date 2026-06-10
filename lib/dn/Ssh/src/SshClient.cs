using System.Buffers.Binary;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Ssh;

/// <summary>
/// Provides a managed SSH client with Go-style session APIs.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var client = await SshClient.ConnectAsync(
///     "127.0.0.1",
///     22,
///     "test",
///     password: "password");
///
/// string whoami = await client.OutputAsync("whoami");
/// Console.WriteLine(whoami.Trim());
/// </code>
/// </example>
/// </remarks>
public sealed class SshClient : IDisposable
{
    private const uint DefaultWindowSize = 1 << 20;
    private const uint DefaultMaxPacketSize = 32768;

    private readonly TcpClient tcpClient;
    private readonly NetworkStream stream;
    private readonly byte[] sessionId;
    private readonly byte[] hostKeyBlob;
    private readonly SshClientConfig config;
    private readonly SequenceNumber sendSeq;
    private readonly SequenceNumber recvSeq;
    private readonly PacketCipher writeCipher;
    private readonly PacketCipher readCipher;
    private readonly Queue<byte> channelDataBuffer = new();
    private uint nextChannel;
    private uint localChannel;
    private uint remoteChannel;
    private bool channelOpen;
    private ulong sentBytes;
    private ulong recvBytes;
    private bool disposed;

    private SshClient(
        TcpClient tcpClient,
        NetworkStream stream,
        byte[] sessionId,
        byte[] hostKeyBlob,
        SshClientConfig config,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        this.tcpClient = tcpClient;
        this.stream = stream;
        this.sessionId = sessionId;
        this.hostKeyBlob = hostKeyBlob;
        this.config = config;
        this.sendSeq = sendSeq;
        this.recvSeq = recvSeq;
        this.writeCipher = writeCipher;
        this.readCipher = readCipher;
    }

    /// <summary>
    /// Gets the client configuration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// SshClientConfig cfg = client.Config;
    /// </code>
    /// </example>
    /// </remarks>
    public SshClientConfig Config => this.config;

    /// <summary>
    /// Creates and authenticates an SSH client with password or public-key auth.
    /// </summary>
    /// <param name="host">The target host.</param>
    /// <param name="port">The target port.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password, used for password auth or as a fallback.</param>
    /// <param name="config">The client config.</param>
    /// <returns>The connected SSH client.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var client = await SshClient.ConnectAsync("127.0.0.1", 22, "test", "password");
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<SshClient> ConnectAsync(
        string host,
        int port,
        string username,
        string? password = null,
        SshClientConfig? config = null)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(host, port).ConfigureAwait(false);
        var stream = tcp.GetStream();

        var clientVersion = Encoding.ASCII.GetBytes("SSH-2.0-neobeard-dn-ssh-0.1\r\n");
        await stream.WriteAsync(clientVersion).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);

        var serverVersion = Encoding.ASCII.GetBytes(await ReadVersionLineAsync(stream).ConfigureAwait(false));
        var sendSeq = new SequenceNumber();
        var recvSeq = new SequenceNumber();

        var clientKexInit = BuildKexInitPayload();
        await WritePacketAsync(stream, 20, clientKexInit, sendSeq).ConfigureAwait(false);

        var serverPacket = await ReadPacketAsync(stream, recvSeq).ConfigureAwait(false);
        if (serverPacket.Type != 20)
            throw new InvalidOperationException($"Expected SSH_MSG_KEXINIT (20) but got {serverPacket.Type}.");

        var algorithms = NegotiateAlgorithms(serverPacket.Payload);
        var hostKex = algorithms.Kex switch
        {
            "ecdh-sha2-nistp256" => await PerformP256KexAsync(
                stream,
                clientVersion,
                serverVersion,
                clientKexInit,
                serverPacket.Payload,
                sendSeq,
                recvSeq).ConfigureAwait(false),
            "diffie-hellman-group14-sha256" => await PerformGroup14Sha256KexAsync(
                stream,
                clientVersion,
                serverVersion,
                clientKexInit,
                serverPacket.Payload,
                sendSeq,
                recvSeq).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Unsupported negotiated SSH key exchange algorithm: {algorithms.Kex}"),
        };

        var sessionId = hostKex.ExchangeHash;
        var ciphers = CreatePacketCiphers(hostKex.SharedSecret, hostKex.ExchangeHash, sessionId);

        await WriteServiceRequestAsync(stream, "ssh-userauth", sendSeq, ciphers.Write).ConfigureAwait(false);
        var accept = await ReadPacketAsync(stream, recvSeq, ciphers.Read).ConfigureAwait(false);
        if (accept.Type != 6)
            throw new InvalidOperationException("SSH userauth service was not accepted.");

        var effectiveConfig = config ?? new SshClientConfig();
        await AuthenticateAsync(stream, username, password, effectiveConfig, sessionId, sendSeq, recvSeq, ciphers.Write, ciphers.Read).ConfigureAwait(false);

        return new SshClient(
            tcp,
            stream,
            sessionId,
            hostKex.HostKeyBlob,
            effectiveConfig,
            sendSeq,
            recvSeq,
            ciphers.Write,
            ciphers.Read);
    }

    private static SshClient ConnectCore(
        string host,
        int port,
        string username,
        string? password = null,
        SshClientConfig? config = null)
    {
        var tcp = new TcpClient();
        try
        {
            tcp.Connect(host, port);
            var stream = tcp.GetStream();

            var clientVersion = Encoding.ASCII.GetBytes("SSH-2.0-neobeard-dn-ssh-0.1\r\n");
            stream.Write(clientVersion);
            stream.Flush();

            var serverVersion = Encoding.ASCII.GetBytes(ReadVersionLine(stream));
            var sendSeq = new SequenceNumber();
            var recvSeq = new SequenceNumber();

            var clientKexInit = BuildKexInitPayload();
            WritePacket(stream, 20, clientKexInit, sendSeq);

            var serverPacket = ReadPacket(stream, recvSeq);
            if (serverPacket.Type != 20)
                throw new InvalidOperationException($"Expected SSH_MSG_KEXINIT (20) but got {serverPacket.Type}.");

            var algorithms = NegotiateAlgorithms(serverPacket.Payload);
            var hostKex = algorithms.Kex switch
            {
                "ecdh-sha2-nistp256" => PerformP256Kex(
                    stream,
                    clientVersion,
                    serverVersion,
                    clientKexInit,
                    serverPacket.Payload,
                    sendSeq,
                    recvSeq),
                "diffie-hellman-group14-sha256" => PerformGroup14Sha256Kex(
                    stream,
                    clientVersion,
                    serverVersion,
                    clientKexInit,
                    serverPacket.Payload,
                    sendSeq,
                    recvSeq),
                _ => throw new NotSupportedException($"Unsupported negotiated SSH key exchange algorithm: {algorithms.Kex}"),
            };

            var sessionId = hostKex.ExchangeHash;
            var ciphers = CreatePacketCiphers(hostKex.SharedSecret, hostKex.ExchangeHash, sessionId);

            WriteServiceRequest(stream, "ssh-userauth", sendSeq, ciphers.Write);
            var accept = ReadPacket(stream, recvSeq, ciphers.Read);
            if (accept.Type != 6)
                throw new InvalidOperationException("SSH userauth service was not accepted.");

            var effectiveConfig = config ?? new SshClientConfig();
            Authenticate(stream, username, password, effectiveConfig, sessionId, sendSeq, recvSeq, ciphers.Write, ciphers.Read);

            return new SshClient(
                tcp,
                stream,
                sessionId,
                hostKex.HostKeyBlob,
                effectiveConfig,
                sendSeq,
                recvSeq,
                ciphers.Write,
                ciphers.Read);
        }
        catch
        {
            tcp.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates and authenticates an SSH client with password or public-key auth.
    /// </summary>
    /// <param name="host">The target host.</param>
    /// <param name="port">The target port.</param>
    /// <param name="username">The username.</param>
    /// <param name="password">The password, used for password auth or as a fallback.</param>
    /// <param name="config">The client config.</param>
    /// <returns>The connected SSH client.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var client = SshClient.Connect("127.0.0.1", 22, "test", "password");
    /// </code>
    /// </example>
    /// </remarks>
    public static SshClient Connect(string host, int port, string username, string? password = null, SshClientConfig? config = null)
    {
        return ConnectCore(host, port, username, password, config);
    }

    /// <summary>
    /// Creates a new session object.
    /// </summary>
    /// <param name="options">Optional session options.</param>
    /// <returns>A new session instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// SshSession session = client.NewSession();
    /// </code>
    /// </example>
    /// </remarks>
    public SshSession NewSession(SshSessionOptions? options = null)
    {
        return new SshSession(this, options ?? new SshSessionOptions());
    }

    /// <summary>
    /// Runs a command and returns captured stdout.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.OutputAsync("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(string command)
    {
        var session = this.NewSession();
        return session.OutputAsync(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.OutputAsync(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        var session = this.NewSession();
        return session.OutputAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.OutputAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        return session.OutputAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.OutputAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        return session.OutputAsync(utf8Chars);
    }

    /// <summary>
    /// Runs a command and returns captured stdout.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.Output("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(string command)
    {
        var session = this.NewSession();
        return session.Output(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.Output(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        return session.Output(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The command stdout.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.Output(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        return session.Output(utf8Chars);
    }

    /// <summary>
    /// Runs a command and returns stdout+stderr.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.CombinedOutputAsync("printf o; printf e 1&gt;&amp;2");
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(string command)
    {
        var session = this.NewSession();
        return session.CombinedOutputAsync(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns stdout+stderr.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        var session = this.NewSession();
        return session.CombinedOutputAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns stdout+stderr.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        return session.CombinedOutputAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns stdout+stderr.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = await client.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        return session.CombinedOutputAsync(utf8Chars);
    }

    /// <summary>
    /// Runs a command and returns stdout+stderr.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.CombinedOutput("printf o; printf e 1&gt;&amp;2");
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(string command)
    {
        var session = this.NewSession();
        return session.CombinedOutput(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns stdout+stderr.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.CombinedOutput(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2"));
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        return session.CombinedOutput(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns stdout+stderr.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The concatenated output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string value = client.CombinedOutput(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        return session.CombinedOutput(utf8Chars);
    }

    /// <summary>
    /// Runs a command and requires a zero exit code.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>A task that completes on success.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.RunAsync("true");
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(string command)
    {
        var session = this.NewSession();
        return session.RunAsync(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and requires a zero exit code.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>A task that completes on success.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.RunAsync(Encoding.UTF8.GetBytes("true"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        var session = this.NewSession();
        return session.RunAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and requires a zero exit code.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>A task that completes on success.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.RunAsync(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        return session.RunAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and requires a zero exit code.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>A task that completes on success.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.RunAsync(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        return session.RunAsync(utf8Chars);
    }

    /// <summary>
    /// Runs a command and requires a zero exit code.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.Run("true");
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(string command)
    {
        var session = this.NewSession();
        session.Run(command);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and requires a zero exit code.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.Run(Encoding.UTF8.GetBytes("true"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(ReadOnlySpan<byte> utf8Bytes)
    {
        var session = this.NewSession();
        session.Run(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and requires a zero exit code.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.Run(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(ReadOnlySpan<char> utf8Chars)
    {
        var session = this.NewSession();
        session.Run(utf8Chars);
    }

    /// <summary>
    /// Uploads a file using SCP.
    /// </summary>
    /// <param name="remotePath">The remote destination path.</param>
    /// <param name="data">The file bytes.</param>
    /// <param name="mode">The file mode as octal integer.</param>
    /// <returns>A task that completes when upload succeeds.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.ScpUploadAsync("/tmp/hello.txt", Encoding.UTF8.GetBytes("hello"));
    /// </code>
    /// </example>
    /// </remarks>
    public async Task ScpUploadAsync(string remotePath, byte[] data, int mode = 0x1A4)
    {
        ArgumentNullException.ThrowIfNull(remotePath);
        ArgumentNullException.ThrowIfNull(data);

        await this.SendExecStartAsync($"scp -t '{EscapeForSingleQuotedShell(remotePath)}'").ConfigureAwait(false);
        await this.ExpectScpOkAsync().ConfigureAwait(false);

        var filename = GetFileName(remotePath);
        var header = Encoding.ASCII.GetBytes($"C{Convert.ToString(mode, 8).PadLeft(4, '0')} {data.Length} {filename}\n");
        await this.WriteChannelDataAsync(header).ConfigureAwait(false);
        await this.ExpectScpOkAsync().ConfigureAwait(false);

        await this.WriteChannelDataAsync(data).ConfigureAwait(false);
        await this.WriteChannelDataAsync([0]).ConfigureAwait(false);
        await this.ExpectScpOkAsync().ConfigureAwait(false);
        await this.WriteChannelEofAsync().ConfigureAwait(false);
        ThrowIfRemoteFailed(await this.CollectExecResultAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// Uploads a file using SCP.
    /// </summary>
    /// <param name="remotePath">The remote destination path.</param>
    /// <param name="data">The file bytes.</param>
    /// <param name="mode">The file mode as octal integer.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.ScpUpload("/tmp/hello.txt", Encoding.UTF8.GetBytes("hello"));
    /// </code>
    /// </example>
    /// </remarks>
    public void ScpUpload(string remotePath, byte[] data, int mode = 0x1A4)
    {
        ArgumentNullException.ThrowIfNull(remotePath);
        ArgumentNullException.ThrowIfNull(data);

        this.SendExecStartCore($"scp -t '{EscapeForSingleQuotedShell(remotePath)}'");
        this.ExpectScpOkCore();

        var filename = GetFileName(remotePath);
        var header = Encoding.ASCII.GetBytes($"C{Convert.ToString(mode, 8).PadLeft(4, '0')} {data.Length} {filename}\n");
        this.WriteChannelDataCore(header);
        this.ExpectScpOkCore();

        this.WriteChannelDataCore(data);
        this.WriteChannelDataCore([0]);
        this.ExpectScpOkCore();
        this.WriteChannelEofCore();
        ThrowIfRemoteFailed(this.CollectExecResultCore());
    }

    /// <summary>
    /// Downloads a file using SCP.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <returns>The file bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] data = await client.ScpDownloadAsync("/tmp/hello.txt");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<byte[]> ScpDownloadAsync(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        await this.SendExecStartAsync($"scp -f '{EscapeForSingleQuotedShell(remotePath)}'").ConfigureAwait(false);
        await this.WriteChannelDataAsync([0]).ConfigureAwait(false);

        var header = await this.ReadScpLineAsync().ConfigureAwait(false);
        if (!header.StartsWith("C", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected SCP response: {header}");

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new InvalidOperationException($"Invalid SCP file header: {header}");

        if (!int.TryParse(parts[1], out var size) || size < 0)
            throw new InvalidOperationException($"Invalid SCP file size in header: {header}");

        await this.WriteChannelDataAsync([0]).ConfigureAwait(false);
        var data = await this.ReadChannelDataExactAsync(size).ConfigureAwait(false);
        await this.ExpectScpOkAsync().ConfigureAwait(false);
        await this.WriteChannelDataAsync([0]).ConfigureAwait(false);
        ThrowIfRemoteFailed(await this.CollectExecResultAsync().ConfigureAwait(false));
        return data;
    }

    /// <summary>
    /// Downloads a file using SCP.
    /// </summary>
    /// <param name="remotePath">The remote file path.</param>
    /// <returns>The file bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] data = client.ScpDownload("/tmp/hello.txt");
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] ScpDownload(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        this.SendExecStartCore($"scp -f '{EscapeForSingleQuotedShell(remotePath)}'");
        this.WriteChannelDataCore([0]);

        var header = this.ReadScpLineCore();
        if (!header.StartsWith("C", StringComparison.Ordinal))
            throw new InvalidOperationException($"Unexpected SCP response: {header}");

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            throw new InvalidOperationException($"Invalid SCP file header: {header}");

        if (!int.TryParse(parts[1], out var size) || size < 0)
            throw new InvalidOperationException($"Invalid SCP file size in header: {header}");

        this.WriteChannelDataCore([0]);
        var data = this.ReadChannelDataExactCore(size);
        this.ExpectScpOkCore();
        this.WriteChannelDataCore([0]);
        ThrowIfRemoteFailed(this.CollectExecResultCore());
        return data;
    }

    /// <summary>
    /// Lists remote directory entries using an exec-backed helper.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <returns>The entry names in the remote directory.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; entries = await client.ListFilesAsync("/tmp");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<IReadOnlyList<string>> ListFilesAsync(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        var output = await this.OutputAsync($"find '{EscapeForSingleQuotedShell(remotePath)}' -maxdepth 1 -mindepth 1 -printf '%f\\n' | sort").ConfigureAwait(false);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Lists remote directory entries using an exec-backed helper.
    /// </summary>
    /// <param name="remotePath">The remote directory path.</param>
    /// <returns>The entry names in the remote directory.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; entries = client.ListFiles("/tmp");
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> ListFiles(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        var output = this.Output($"find '{EscapeForSingleQuotedShell(remotePath)}' -maxdepth 1 -mindepth 1 -printf '%f\\n' | sort");
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Opens a direct-tcpip channel to a remote host:port and returns the initial banner bytes.
    /// </summary>
    /// <param name="host">The target host.</param>
    /// <param name="port">The target port.</param>
    /// <param name="options">Direct-tcpip options.</param>
    /// <returns>The first channel data payload if any, otherwise an empty array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] data = await client.DialTcpAsync("127.0.0.1", 22);
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<byte[]> DialTcpAsync(string host, int port, SshDirectTcpOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        options ??= new SshDirectTcpOptions();

        var channel = await this.OpenChannelAsync(
            "direct-tcpip",
            BuildDirectTcpOpenPayload(
            host,
            port,
            options.OriginHost,
            options.OriginPort)).ConfigureAwait(false);

        while (true)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 94)
            {
                var reader = new PacketReader(packet.Payload);
                var recipient = reader.ReadUInt32();
                if (recipient != channel.Local)
                    continue;

                return reader.ReadStringBytes();
            }

            if (packet.Type == 97)
                return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Opens a direct-tcpip channel to a remote host:port and returns the initial banner bytes.
    /// </summary>
    /// <param name="host">The target host.</param>
    /// <param name="port">The target port.</param>
    /// <param name="options">Direct-tcpip options.</param>
    /// <returns>The first channel data payload if any, otherwise an empty array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] data = client.DialTcp("127.0.0.1", 22);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] DialTcp(string host, int port, SshDirectTcpOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        options ??= new SshDirectTcpOptions();

        var channel = this.OpenChannelCore(
            "direct-tcpip",
            BuildDirectTcpOpenPayload(
                host,
                port,
                options.OriginHost,
                options.OriginPort));

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 94)
            {
                var reader = new PacketReader(packet.Payload);
                var recipient = reader.ReadUInt32();
                if (recipient != channel.Local)
                    continue;

                return reader.ReadStringBytes();
            }

            if (packet.Type == 97)
                return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Releases network resources.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.Dispose();
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        if (this.disposed)
            return;

        this.disposed = true;
        this.stream.Dispose();
        this.tcpClient.Dispose();
    }

    /// <summary>
    /// Prepares the session environment using synchronous channel operations.
    /// </summary>
    /// <param name="options">The session options to apply.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.PrepareSessionEnv(options);
    /// </code>
    /// </example>
    /// </remarks>
    internal void PrepareSessionEnv(SshSessionOptions options)
    {
        this.PrepareSessionEnvCore(options);
    }

    /// <summary>
    /// Sends a session environment request using synchronous channel operations.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <param name="strict">True when rejection should throw.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool accepted = client.SetSessionEnv("LANG", "C.UTF-8", true);
    /// </code>
    /// </example>
    /// </remarks>
    internal bool SetSessionEnv(string name, string value, bool strict)
    {
        return this.SetSessionEnv(name, Encoding.UTF8.GetBytes(value), strict);
    }

    /// <summary>
    /// Sends a session environment request using synchronous channel operations.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="valueUtf8">The environment variable value encoded as UTF-8 bytes.</param>
    /// <param name="strict">True when rejection should throw.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool accepted = client.SetSessionEnv("LANG", Encoding.UTF8.GetBytes("C.UTF-8").AsSpan(), true);
    /// </code>
    /// </example>
    /// </remarks>
    internal bool SetSessionEnv(string name, ReadOnlySpan<byte> valueUtf8, bool strict)
    {
        return this.SetSessionEnv(name, valueUtf8.ToArray(), strict);
    }

    /// <summary>
    /// Starts an exec request using synchronous channel operations.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.SendExecStart("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    internal void SendExecStart(string command)
    {
        this.SendExecStart(Encoding.UTF8.GetBytes(command));
    }

    /// <summary>
    /// Starts an exec request using synchronous channel operations.
    /// </summary>
    /// <param name="commandUtf8">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.SendExecStart(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    internal void SendExecStart(ReadOnlySpan<byte> commandUtf8)
    {
        this.SendExecStart(commandUtf8.ToArray());
    }

    /// <summary>
    /// Waits for a started exec request using synchronous channel operations.
    /// </summary>
    /// <returns>The command result.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// SshExecResult result = client.CollectExecResult();
    /// </code>
    /// </example>
    /// </remarks>
    internal SshExecResult CollectExecResult()
    {
        return this.CollectExecResultCore();
    }

    /// <summary>
    /// Sends a session environment request using synchronous channel operations.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="valueUtf8">The environment variable value encoded as UTF-8.</param>
    /// <param name="strict">True when rejection should throw.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool accepted = client.SetSessionEnv("LANG", Encoding.UTF8.GetBytes("C.UTF-8"), true);
    /// </code>
    /// </example>
    /// </remarks>
    internal bool SetSessionEnv(string name, byte[] valueUtf8, bool strict)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(valueUtf8);
        this.EnsureSessionChannelCore();

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "env",
            true,
            BuildString(name),
            BuildString(valueUtf8));

        WritePacket(this.stream, 98, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return true;

            if (packet.Type == 100)
            {
                if (strict)
                    throw new SshEnvRejectedException(name);

                return false;
            }
        }
    }

    /// <summary>
    /// Sends a session environment request using synchronous channel operations.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="valueUtf8">The environment variable value encoded as UTF-8.</param>
    /// <param name="strict">True when rejection should throw.</param>
    /// <returns>A task that completes when the request is processed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool accepted = await client.SetSessionEnvAsync("LANG", Encoding.UTF8.GetBytes("C.UTF-8"), true);
    /// </code>
    /// </example>
    /// </remarks>
    internal async Task<bool> SetSessionEnvAsync(string name, byte[] valueUtf8, bool strict)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(valueUtf8);
        await this.EnsureSessionChannelAsync().ConfigureAwait(false);

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "env",
            true,
            BuildString(name),
            BuildString(valueUtf8));

        await WritePacketAsync(this.stream, 98, payload, this.sendSeq, this.writeCipher).ConfigureAwait(false);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return true;

            if (packet.Type == 100)
            {
                if (strict)
                    throw new SshEnvRejectedException(name);

                return false;
            }
        }
    }

    /// <summary>
    /// Sends a session environment request using asynchronous channel operations.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="valueUtf8">The environment variable value encoded as UTF-8 bytes.</param>
    /// <param name="strict">True when rejection should throw.</param>
    /// <returns>A task that completes when the request is processed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool accepted = await client.SetSessionEnvAsync("LANG", Encoding.UTF8.GetBytes("C.UTF-8").AsSpan(), true);
    /// </code>
    /// </example>
    /// </remarks>
    internal Task<bool> SetSessionEnvAsync(string name, ReadOnlySpan<byte> valueUtf8, bool strict)
    {
        return this.SetSessionEnvAsync(name, valueUtf8.ToArray(), strict);
    }

    /// <summary>
    /// Starts an exec request using synchronous channel operations.
    /// </summary>
    /// <param name="commandUtf8">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.SendExecStart(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    internal void SendExecStart(byte[] commandUtf8)
    {
        ArgumentNullException.ThrowIfNull(commandUtf8);
        this.EnsureSessionChannelCore();

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "exec",
            true,
            BuildString(commandUtf8));

        WritePacket(this.stream, 98, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return;

            if (packet.Type == 100)
                throw new InvalidOperationException("SSH exec request was rejected by remote.");
        }
    }

    /// <summary>
    /// Starts an exec request using asynchronous channel operations.
    /// </summary>
    /// <param name="commandUtf8">The remote command encoded as UTF-8.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.SendExecStartAsync(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    internal async Task SendExecStartAsync(byte[] commandUtf8)
    {
        ArgumentNullException.ThrowIfNull(commandUtf8);
        await this.EnsureSessionChannelAsync().ConfigureAwait(false);

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "exec",
            true,
            BuildString(commandUtf8));

        await WritePacketAsync(this.stream, 98, payload, this.sendSeq, this.writeCipher).ConfigureAwait(false);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return;

            if (packet.Type == 100)
                throw new InvalidOperationException("SSH exec request was rejected by remote.");
        }
    }

    /// <summary>
    /// Starts an exec request using asynchronous channel operations.
    /// </summary>
    /// <param name="commandUtf8">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.SendExecStartAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    internal Task SendExecStartAsync(ReadOnlySpan<byte> commandUtf8)
    {
        return this.SendExecStartAsync(commandUtf8.ToArray());
    }

    /// <summary>
    /// Writes bytes to the active channel using synchronous channel operations.
    /// </summary>
    /// <param name="bytes">The bytes to send.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.WriteChannelData(bytes);
    /// </code>
    /// </example>
    /// </remarks>
    internal void WriteChannelData(byte[] bytes)
    {
        this.WriteChannelDataCore(bytes);
    }

    /// <summary>
    /// Writes bytes to the active channel using synchronous channel operations.
    /// </summary>
    /// <param name="bytes">The bytes to send.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.WriteChannelData(bytes.AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    internal void WriteChannelData(ReadOnlySpan<byte> bytes)
    {
        this.WriteChannelData(bytes.ToArray());
    }

    /// <summary>
    /// Sends EOF on the active channel using synchronous channel operations.
    /// </summary>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// client.WriteChannelEof();
    /// </code>
    /// </example>
    /// </remarks>
    internal void WriteChannelEof()
    {
        this.WriteChannelEofCore();
    }

    /// <summary>
    /// Reads an exact number of bytes from the active channel using synchronous channel operations.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The requested bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// byte[] data = client.ReadChannelDataExact(1);
    /// </code>
    /// </example>
    /// </remarks>
    internal byte[] ReadChannelDataExact(int length)
    {
        return this.ReadChannelDataExactCore(length);
    }

    internal async Task PrepareSessionEnvAsync(SshSessionOptions options)
    {
        await this.EnsureSessionChannelAsync().ConfigureAwait(false);

        var plan = SshEnvironmentPlanner.BuildPlan(this.config, options);
        foreach (var kv in plan.Variables)
        {
            var accepted = await this.SetSessionEnvAsync(kv.Key, kv.Value, plan.StrictEnv).ConfigureAwait(false);
            if (!accepted && plan.StrictEnv)
                throw new SshEnvRejectedException(kv.Key);
        }
    }

    internal async Task<bool> SetSessionEnvAsync(string name, string value, bool strict)
    {
        ArgumentNullException.ThrowIfNull(value);
        return await this.SetSessionEnvAsync(name, Encoding.UTF8.GetBytes(value), strict).ConfigureAwait(false);
    }

    internal async Task SendExecStartAsync(string command)
    {
        await this.SendExecStartAsync(Encoding.UTF8.GetBytes(command)).ConfigureAwait(false);
    }

    internal async Task<SshExecResult> CollectExecResultAsync()
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        int? code = null;
        string? signal = null;

        while (true)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;

            if (packet.Type == 94)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                stdout.Append(Encoding.UTF8.GetString(reader.ReadStringBytes()));
                continue;
            }

            if (packet.Type == 95)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                stderr.Append(Encoding.UTF8.GetString(reader.ReadStringBytes()));
                continue;
            }

            if (packet.Type == 98)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                var requestType = reader.ReadString();
                _ = reader.ReadBoolean();
                if (requestType == "exit-status")
                {
                    code = checked((int)reader.ReadUInt32());
                }
                else if (requestType == "exit-signal")
                {
                    signal = reader.ReadString();
                }

                continue;
            }

            if (packet.Type == 97)
            {
                this.channelOpen = false;
                return new SshExecResult(code, signal, stdout.ToString(), stderr.ToString());
            }
        }
    }

    internal async Task WriteChannelDataAsync(byte[] bytes)
    {
        var payload = BuildChannelDataPayload(this.remoteChannel, bytes);
        await WritePacketAsync(this.stream, 94, payload, this.sendSeq, this.writeCipher).ConfigureAwait(false);
        this.sentBytes += (ulong)payload.Length;
    }

    /// <summary>
    /// Writes bytes to the active channel using asynchronous channel operations.
    /// </summary>
    /// <param name="bytes">The bytes to send.</param>
    /// <returns>A task that completes when the write is flushed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await client.WriteChannelDataAsync(bytes.AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    internal Task WriteChannelDataAsync(ReadOnlySpan<byte> bytes)
    {
        return this.WriteChannelDataAsync(bytes.ToArray());
    }

    internal Task WriteChannelEofAsync()
    {
        return WritePacketAsync(this.stream, 96, BuildUInt32(this.remoteChannel), this.sendSeq, this.writeCipher);
    }

    internal async Task<byte[]> ReadChannelDataExactAsync(int length)
    {
        var output = new byte[length];
        var offset = 0;
        while (offset < length && this.channelDataBuffer.Count > 0)
            output[offset++] = this.channelDataBuffer.Dequeue();

        while (offset < length)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type != 94)
            {
                if (packet.Type == 97)
                {
                    this.channelOpen = false;
                    throw new InvalidOperationException("SSH channel closed before enough data was read.");
                }

                continue;
            }

            var reader = new PacketReader(packet.Payload);
            _ = reader.ReadUInt32();
            var chunk = reader.ReadStringBytes();
            var toCopy = Math.Min(chunk.Length, length - offset);
            chunk.AsSpan(0, toCopy).CopyTo(output.AsSpan(offset, toCopy));
            offset += toCopy;
            for (var i = toCopy; i < chunk.Length; i++)
                this.channelDataBuffer.Enqueue(chunk[i]);
        }

        return output;
    }

    private void PrepareSessionEnvCore(SshSessionOptions options)
    {
        this.EnsureSessionChannelCore();

        var plan = SshEnvironmentPlanner.BuildPlan(this.config, options);
        foreach (var kv in plan.Variables)
        {
            var accepted = this.SetSessionEnvCore(kv.Key, kv.Value, plan.StrictEnv);
            if (!accepted && plan.StrictEnv)
                throw new SshEnvRejectedException(kv.Key);
        }
    }

    private bool SetSessionEnvCore(string name, string value, bool strict)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        this.EnsureSessionChannelCore();

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "env",
            true,
            BuildString(name),
            BuildString(value));

        WritePacket(this.stream, 98, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return true;

            if (packet.Type == 100)
            {
                if (strict)
                    throw new SshEnvRejectedException(name);

                return false;
            }
        }
    }

    private void SendExecStartCore(string command)
    {
        this.EnsureSessionChannelCore();

        var payload = BuildChannelRequestPayload(
            this.remoteChannel,
            "exec",
            true,
            BuildString(command));

        WritePacket(this.stream, 98, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 99)
                return;

            if (packet.Type == 100)
                throw new InvalidOperationException("SSH exec request was rejected by remote.");
        }
    }

    private SshExecResult CollectExecResultCore()
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        int? code = null;
        string? signal = null;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;

            if (packet.Type == 94)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                stdout.Append(Encoding.UTF8.GetString(reader.ReadStringBytes()));
                continue;
            }

            if (packet.Type == 95)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                stderr.Append(Encoding.UTF8.GetString(reader.ReadStringBytes()));
                continue;
            }

            if (packet.Type == 98)
            {
                var reader = new PacketReader(packet.Payload);
                _ = reader.ReadUInt32();
                var requestType = reader.ReadString();
                _ = reader.ReadBoolean();
                if (requestType == "exit-status")
                {
                    code = checked((int)reader.ReadUInt32());
                }
                else if (requestType == "exit-signal")
                {
                    signal = reader.ReadString();
                }

                continue;
            }

            if (packet.Type == 97)
            {
                this.channelOpen = false;
                return new SshExecResult(code, signal, stdout.ToString(), stderr.ToString());
            }
        }
    }

    private void WriteChannelDataCore(byte[] bytes)
    {
        var payload = BuildChannelDataPayload(this.remoteChannel, bytes);
        WritePacket(this.stream, 94, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;
    }

    private void WriteChannelEofCore()
    {
        WritePacket(this.stream, 96, BuildUInt32(this.remoteChannel), this.sendSeq, this.writeCipher);
    }

    private byte[] ReadChannelDataExactCore(int length)
    {
        var output = new byte[length];
        var offset = 0;
        while (offset < length && this.channelDataBuffer.Count > 0)
            output[offset++] = this.channelDataBuffer.Dequeue();

        while (offset < length)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type != 94)
            {
                if (packet.Type == 97)
                {
                    this.channelOpen = false;
                    throw new InvalidOperationException("SSH channel closed before enough data was read.");
                }

                continue;
            }

            var reader = new PacketReader(packet.Payload);
            _ = reader.ReadUInt32();
            var chunk = reader.ReadStringBytes();
            var toCopy = Math.Min(chunk.Length, length - offset);
            chunk.AsSpan(0, toCopy).CopyTo(output.AsSpan(offset, toCopy));
            offset += toCopy;
            for (var i = toCopy; i < chunk.Length; i++)
                this.channelDataBuffer.Enqueue(chunk[i]);
        }

        return output;
    }

    private void EnsureSessionChannelCore()
    {
        if (this.channelOpen)
            return;

        var channel = this.OpenChannelCore("session", Array.Empty<byte>());
        this.localChannel = channel.Local;
        this.remoteChannel = channel.Remote;
        this.channelOpen = true;
    }

    private ChannelInfo OpenChannelCore(string channelType, byte[] typePayload)
    {
        var local = this.nextChannel++;
        var payload = Concat(
            BuildChannelOpenPayload(channelType, local, DefaultWindowSize, DefaultMaxPacketSize),
            typePayload);

        WritePacket(this.stream, 90, payload, this.sendSeq, this.writeCipher);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = ReadPacket(this.stream, this.recvSeq, this.readCipher);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 91)
            {
                var reader = new PacketReader(packet.Payload);
                var recipient = reader.ReadUInt32();
                if (recipient != local)
                    throw new InvalidOperationException("Channel confirmation recipient mismatch.");

                var remote = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                return new ChannelInfo(local, remote);
            }

            if (packet.Type == 92)
                throw new InvalidOperationException($"SSH channel open failed: {channelType}.");
        }
    }

    private string ReadScpLineCore()
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var one = this.ReadChannelDataExactCore(1);
            if (one[0] == (byte)'\n')
                break;

            ms.WriteByte(one[0]);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void ExpectScpOkCore()
    {
        var code = this.ReadChannelDataExactCore(1);
        if (code[0] == 0)
            return;

        var message = this.ReadScpLineCore();
        throw new InvalidOperationException($"SCP error {code[0]}: {message}");
    }

    private static string GetFileName(string path)
    {
        var normalized = path.Replace('\\', '/');
        var index = normalized.LastIndexOf('/');
        return index >= 0 ? normalized[(index + 1)..] : normalized;
    }

    private static string EscapeForSingleQuotedShell(string value)
    {
        return value.Replace("'", "'\\''", StringComparison.Ordinal);
    }

    private static void ThrowIfRemoteFailed(SshExecResult result)
    {
        if (result.ExitCode == 0 && string.IsNullOrEmpty(result.Signal))
            return;

        var preview = result.Stderr.Length > 400 ? result.Stderr[..400] : result.Stderr;
        throw new SshRemoteException(
            string.IsNullOrWhiteSpace(preview)
                ? $"SSH remote command failed (code: {result.ExitCode?.ToString() ?? "null"})."
                : $"SSH remote command failed (code: {result.ExitCode?.ToString() ?? "null"}): {preview}",
            result.ExitCode,
            result.Signal,
            result.Stderr);
    }

    private static byte[] BuildDirectTcpOpenPayload(
        string targetHost,
        int targetPort,
        string originHost,
        int originPort)
    {
        return Concat(
            BuildString(targetHost),
            BuildUInt32(checked((uint)targetPort)),
            BuildString(originHost),
            BuildUInt32(checked((uint)originPort)));
    }

    private static byte[] BuildChannelOpenPayload(string type, uint senderChannel, uint windowSize, uint maxPacket)
    {
        return Concat(
            BuildString(type),
            BuildUInt32(senderChannel),
            BuildUInt32(windowSize),
            BuildUInt32(maxPacket));
    }

    private static byte[] BuildChannelDataPayload(uint recipientChannel, byte[] data)
    {
        return Concat(BuildUInt32(recipientChannel), BuildString(data));
    }

    private static byte[] BuildChannelRequestPayload(uint recipientChannel, string requestType, bool wantReply, params byte[][] data)
    {
        var prefix = Concat(
            BuildUInt32(recipientChannel),
            BuildString(requestType),
            [wantReply ? (byte)1 : (byte)0]);

        return Concat(prefix, Concat(data));
    }

    private static byte[] BuildKexInitPayload()
    {
        var cookie = RandomNumberGenerator.GetBytes(16);
        return Concat(
            cookie,
            BuildNameList(["ecdh-sha2-nistp256", "diffie-hellman-group14-sha256"]),
            BuildNameList(["rsa-sha2-512", "rsa-sha2-256", "ssh-rsa"]),
            BuildNameList(["aes128-ctr"]),
            BuildNameList(["aes128-ctr"]),
            BuildNameList(["hmac-sha2-256"]),
            BuildNameList(["hmac-sha2-256"]),
            BuildNameList(["none"]),
            BuildNameList(["none"]),
            BuildNameList([]),
            BuildNameList([]),
            [0],
            BuildUInt32(0));
    }

    private static KexOutcome PerformGroup14Sha256Kex(
        NetworkStream stream,
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq)
    {
        var p = Group14Prime();
        var g = new BigInteger([2], isUnsigned: true, isBigEndian: true);
        var x = new BigInteger(RandomNumberGenerator.GetBytes(32), isUnsigned: true, isBigEndian: true);
        var e = BigInteger.ModPow(g, x, p);

        WritePacket(stream, 30, BuildMpint(e), sendSeq);

        var reply = ReadPacket(stream, recvSeq);
        if (reply.Type != 31)
            throw new InvalidOperationException($"Expected SSH_MSG_KEXDH_REPLY (31) but got {reply.Type}.");

        var reader = new PacketReader(reply.Payload);
        var hostKeyBlob = reader.ReadStringBytes();
        var hostReader = new PacketReader(hostKeyBlob);
        var hostType = hostReader.ReadString();
        if (hostType != "ssh-rsa")
            throw new NotSupportedException($"Unsupported SSH host key algorithm for managed verification: {hostType}");

        var f = ReadMpint(reader.ReadStringBytes());
        var signatureBlob = reader.ReadStringBytes();
        var k = BigInteger.ModPow(f, x, p);

        var exchangeHash = ComputeExchangeHash(clientVersion, serverVersion, clientKexInit, serverKexInit, hostKeyBlob, e, f, k);
        VerifyHostSignature(hostKeyBlob, signatureBlob, exchangeHash);

        WritePacket(stream, 21, Array.Empty<byte>(), sendSeq);
        var newKeys = ReadPacket(stream, recvSeq);
        if (newKeys.Type != 21)
            throw new InvalidOperationException("Expected SSH_MSG_NEWKEYS after KEX.");

        return new KexOutcome(exchangeHash, hostKeyBlob, k);
    }

    private static async Task<KexOutcome> PerformGroup14Sha256KexAsync(
        NetworkStream stream,
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq)
    {
        var p = Group14Prime();
        var g = new BigInteger([2], isUnsigned: true, isBigEndian: true);
        var x = new BigInteger(RandomNumberGenerator.GetBytes(32), isUnsigned: true, isBigEndian: true);
        var e = BigInteger.ModPow(g, x, p);

        await WritePacketAsync(stream, 30, BuildMpint(e), sendSeq).ConfigureAwait(false);

        var reply = await ReadPacketAsync(stream, recvSeq).ConfigureAwait(false);
        if (reply.Type != 31)
            throw new InvalidOperationException($"Expected SSH_MSG_KEXDH_REPLY (31) but got {reply.Type}.");

        var reader = new PacketReader(reply.Payload);
        var hostKeyBlob = reader.ReadStringBytes();
        var hostReader = new PacketReader(hostKeyBlob);
        var hostType = hostReader.ReadString();
        if (hostType != "ssh-rsa")
            throw new NotSupportedException($"Unsupported SSH host key algorithm for managed verification: {hostType}");

        var f = ReadMpint(reader.ReadStringBytes());
        var signatureBlob = reader.ReadStringBytes();
        var k = BigInteger.ModPow(f, x, p);

        var exchangeHash = ComputeExchangeHash(clientVersion, serverVersion, clientKexInit, serverKexInit, hostKeyBlob, e, f, k);
        VerifyHostSignature(hostKeyBlob, signatureBlob, exchangeHash);

        await WritePacketAsync(stream, 21, Array.Empty<byte>(), sendSeq).ConfigureAwait(false);
        var newKeys = await ReadPacketAsync(stream, recvSeq).ConfigureAwait(false);
        if (newKeys.Type != 21)
            throw new InvalidOperationException("Expected SSH_MSG_NEWKEYS after KEX.");

        return new KexOutcome(exchangeHash, hostKeyBlob, k);
    }

    private static KexOutcome PerformP256Kex(
        NetworkStream stream,
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq)
    {
        using var clientKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var clientParameters = clientKey.ExportParameters(false);
        var qClient = Concat(
            [4],
            clientParameters.Q.X ?? throw new CryptographicException("ECDH public X coordinate is missing."),
            clientParameters.Q.Y ?? throw new CryptographicException("ECDH public Y coordinate is missing."));

        WritePacket(stream, 30, BuildString(qClient), sendSeq);

        var reply = ReadPacket(stream, recvSeq);
        if (reply.Type != 31)
            throw new InvalidOperationException($"Expected SSH_MSG_KEXDH_REPLY (31) but got {reply.Type}.");

        var reader = new PacketReader(reply.Payload);
        var hostKeyBlob = reader.ReadStringBytes();
        var qServer = reader.ReadStringBytes();
        var signatureBlob = reader.ReadStringBytes();
        if (qServer.Length != 65 || qServer[0] != 4)
            throw new InvalidOperationException("Invalid SSH ECDH P-256 server public key.");

        using var serverKey = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = qServer[1..33],
                Y = qServer[33..65],
            },
        });

        var secret = clientKey.DeriveRawSecretAgreement(serverKey.PublicKey);
        var k = new BigInteger(secret, isUnsigned: true, isBigEndian: true);
        var exchangeHash = ComputeP256ExchangeHash(clientVersion, serverVersion, clientKexInit, serverKexInit, hostKeyBlob, qClient, qServer, k);
        VerifyHostSignature(hostKeyBlob, signatureBlob, exchangeHash);

        WritePacket(stream, 21, Array.Empty<byte>(), sendSeq);
        var newKeys = ReadPacket(stream, recvSeq);
        if (newKeys.Type != 21)
            throw new InvalidOperationException("Expected SSH_MSG_NEWKEYS after KEX.");

        return new KexOutcome(exchangeHash, hostKeyBlob, k);
    }

    private static async Task<KexOutcome> PerformP256KexAsync(
        NetworkStream stream,
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq)
    {
        using var clientKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var clientParameters = clientKey.ExportParameters(false);
        var qClient = Concat(
            [4],
            clientParameters.Q.X ?? throw new CryptographicException("ECDH public X coordinate is missing."),
            clientParameters.Q.Y ?? throw new CryptographicException("ECDH public Y coordinate is missing."));

        await WritePacketAsync(stream, 30, BuildString(qClient), sendSeq).ConfigureAwait(false);

        var reply = await ReadPacketAsync(stream, recvSeq).ConfigureAwait(false);
        if (reply.Type != 31)
            throw new InvalidOperationException($"Expected SSH_MSG_KEXDH_REPLY (31) but got {reply.Type}.");

        var reader = new PacketReader(reply.Payload);
        var hostKeyBlob = reader.ReadStringBytes();
        var qServer = reader.ReadStringBytes();
        var signatureBlob = reader.ReadStringBytes();
        if (qServer.Length != 65 || qServer[0] != 4)
            throw new InvalidOperationException("Invalid SSH ECDH P-256 server public key.");

        using var serverKey = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = qServer[1..33],
                Y = qServer[33..65],
            },
        });

        var secret = clientKey.DeriveRawSecretAgreement(serverKey.PublicKey);
        var k = new BigInteger(secret, isUnsigned: true, isBigEndian: true);
        var exchangeHash = ComputeP256ExchangeHash(clientVersion, serverVersion, clientKexInit, serverKexInit, hostKeyBlob, qClient, qServer, k);
        VerifyHostSignature(hostKeyBlob, signatureBlob, exchangeHash);

        await WritePacketAsync(stream, 21, Array.Empty<byte>(), sendSeq).ConfigureAwait(false);
        var newKeys = await ReadPacketAsync(stream, recvSeq).ConfigureAwait(false);
        if (newKeys.Type != 21)
            throw new InvalidOperationException("Expected SSH_MSG_NEWKEYS after KEX.");

        return new KexOutcome(exchangeHash, hostKeyBlob, k);
    }

    private static async Task AuthenticateAsync(
        NetworkStream stream,
        string username,
        string? password,
        SshClientConfig config,
        byte[] sessionId,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        if (config.AuthMethod != SshAuthMethod.Password)
        {
            foreach (var keyText in LoadPrivateKeys(config))
            {
                try
                {
                    var key = SshPrivateKey.Parse(keyText, config.PrivateKeyPassphrase);
                    await PublicKeyAuthAsync(stream, username, key, sessionId, sendSeq, recvSeq, writeCipher, readCipher).ConfigureAwait(false);
                    return;
                }
                catch when (password is not null && config.AuthMethod != SshAuthMethod.PublicKey)
                {
                    break;
                }
            }
        }

        if (password is not null && config.AuthMethod != SshAuthMethod.PublicKey)
        {
            await PasswordAuthAsync(stream, username, password, sendSeq, recvSeq, writeCipher, readCipher).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException("SSH authentication requires a password or usable private key.");
    }

    private static IEnumerable<string> LoadPrivateKeys(SshClientConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.PrivateKey))
            yield return config.PrivateKey;

        foreach (var file in config.IdentityFiles)
        {
            var path = ExpandIdentityPath(file);
            if (File.Exists(path))
                yield return File.ReadAllText(path);
        }
    }

    private static string ExpandIdentityPath(string path)
    {
        if (path.StartsWith("~/", StringComparison.Ordinal) || path == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return path == "~" ? home : Path.Combine(home, path[2..]);
        }

        return path;
    }

    private static void Authenticate(
        NetworkStream stream,
        string username,
        string? password,
        SshClientConfig config,
        byte[] sessionId,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        if (config.AuthMethod != SshAuthMethod.Password)
        {
            foreach (var keyText in LoadPrivateKeys(config))
            {
                try
                {
                    var key = SshPrivateKey.Parse(keyText, config.PrivateKeyPassphrase);
                    PublicKeyAuth(stream, username, key, sessionId, sendSeq, recvSeq, writeCipher, readCipher);
                    return;
                }
                catch when (password is not null && config.AuthMethod != SshAuthMethod.PublicKey)
                {
                    break;
                }
            }
        }

        if (password is not null && config.AuthMethod != SshAuthMethod.PublicKey)
        {
            PasswordAuth(stream, username, password, sendSeq, recvSeq, writeCipher, readCipher);
            return;
        }

        throw new InvalidOperationException("SSH authentication requires a password or usable private key.");
    }

    private static void PasswordAuth(
        NetworkStream stream,
        string username,
        string password,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        var payload = Concat(
            BuildString(username),
            BuildString("ssh-connection"),
            BuildString("password"),
            [0],
            BuildString(password));

        WritePacket(stream, 50, payload, sendSeq, writeCipher);

        while (true)
        {
            var packet = ReadPacket(stream, recvSeq, readCipher);
            if (packet.Type == 52)
                return;

            if (packet.Type == 53)
                continue;

            if (packet.Type == 51)
                throw new InvalidOperationException("SSH password authentication failed.");
        }
    }

    private static void PublicKeyAuth(
        NetworkStream stream,
        string username,
        SshPrivateKey key,
        byte[] sessionId,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        var unsigned = Concat(
            [50],
            BuildString(username),
            BuildString("ssh-connection"),
            BuildString("publickey"),
            [1],
            BuildString(key.Algorithm),
            BuildString(key.PublicKeyBlob));

        var signature = key.Sign(Concat(BuildString(sessionId), unsigned));
        WritePacket(stream, 50, Concat(unsigned.AsSpan(1).ToArray(), BuildString(signature)), sendSeq, writeCipher);

        while (true)
        {
            var packet = ReadPacket(stream, recvSeq, readCipher);
            if (packet.Type == 52)
                return;

            if (packet.Type == 53 || packet.Type == 60)
                continue;

            if (packet.Type == 51)
                throw new InvalidOperationException("SSH public key authentication failed.");
        }
    }

    private static void WriteServiceRequest(NetworkStream stream, string service, SequenceNumber sendSeq, PacketCipher? cipher = null)
    {
        WritePacket(stream, 5, BuildString(service), sendSeq, cipher);
    }

    private static string ReadVersionLine(NetworkStream stream)
    {
        using var ms = new MemoryStream();
        var one = new byte[1];
        while (true)
        {
            var n = stream.Read(one, 0, 1);
            if (n == 0)
                throw new InvalidOperationException("SSH stream closed before version exchange.");

            ms.WriteByte(one[0]);
            if (one[0] == (byte)'\n')
            {
                var line = Encoding.ASCII.GetString(ms.ToArray());
                if (line.StartsWith("SSH-", StringComparison.Ordinal))
                    return line;

                ms.SetLength(0);
            }
        }
    }

    private static void WritePacket(NetworkStream stream, byte type, byte[] payload, SequenceNumber seq, PacketCipher? cipher = null)
    {
        var body = Concat([type], payload);
        var blockSize = cipher?.BlockSize ?? 8;
        var padding = blockSize - ((body.Length + 5) % blockSize);
        if (padding < 4)
            padding += blockSize;

        var packetLength = body.Length + padding + 1;
        var packet = new byte[4 + packetLength];
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), checked((uint)packetLength));
        packet[4] = (byte)padding;
        body.CopyTo(packet.AsSpan(5));
        if (padding > 0)
            RandomNumberGenerator.Fill(packet.AsSpan(5 + body.Length, padding));

        var seqValue = seq.Value;
        byte[] output;
        if (cipher is null)
        {
            output = packet;
        }
        else
        {
            var encrypted = cipher.Transform(packet);
            var mac = HMACSHA256.HashData(cipher.MacKey, Concat(BuildUInt32(seqValue), packet));
            output = Concat(encrypted, mac);
        }

        stream.Write(output);
        stream.Flush();
        seq.Increment();
    }

    private static SshPacket ReadPacket(NetworkStream stream, SequenceNumber seq, PacketCipher? cipher = null)
    {
        var seqValue = seq.Value;
        var header = ReadExact(stream, 4);
        if (cipher is not null)
            header = cipher.Transform(header);

        var packetLength = BinaryPrimitives.ReadUInt32BigEndian(header);
        var packetBody = ReadExact(stream, checked((int)packetLength));
        if (cipher is not null)
        {
            var encryptedBody = packetBody;
            packetBody = cipher.Transform(encryptedBody);
            var mac = ReadExact(stream, cipher.MacSize);
            var expected = HMACSHA256.HashData(cipher.MacKey, Concat(BuildUInt32(seqValue), header, packetBody));
            if (!CryptographicOperations.FixedTimeEquals(mac, expected.AsSpan(0, cipher.MacSize)))
                throw new CryptographicException("SSH packet MAC verification failed.");
        }

        seq.Increment();

        var paddingLength = packetBody[0];
        var payloadLength = checked((int)packetLength - paddingLength - 1);
        var payload = packetBody.AsSpan(1, payloadLength).ToArray();
        var type = payload[0];
        var data = payload.AsSpan(1).ToArray();
        return new SshPacket(type, data);
    }

    private static byte[] ReadExact(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var n = stream.Read(buffer, offset, length - offset);
            if (n == 0)
                throw new InvalidOperationException("SSH stream closed unexpectedly.");

            offset += n;
        }

        return buffer;
    }

    private static async Task PasswordAuthAsync(
        NetworkStream stream,
        string username,
        string password,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        var payload = Concat(
            BuildString(username),
            BuildString("ssh-connection"),
            BuildString("password"),
            [0],
            BuildString(password));

        await WritePacketAsync(stream, 50, payload, sendSeq, writeCipher).ConfigureAwait(false);

        while (true)
        {
            var packet = await ReadPacketAsync(stream, recvSeq, readCipher).ConfigureAwait(false);
            if (packet.Type == 52)
                return;

            if (packet.Type == 53)
                continue;

            if (packet.Type == 51)
                throw new InvalidOperationException("SSH password authentication failed.");
        }
    }

    private static async Task PublicKeyAuthAsync(
        NetworkStream stream,
        string username,
        SshPrivateKey key,
        byte[] sessionId,
        SequenceNumber sendSeq,
        SequenceNumber recvSeq,
        PacketCipher writeCipher,
        PacketCipher readCipher)
    {
        var unsigned = Concat(
            [50],
            BuildString(username),
            BuildString("ssh-connection"),
            BuildString("publickey"),
            [1],
            BuildString(key.Algorithm),
            BuildString(key.PublicKeyBlob));

        var signature = key.Sign(Concat(BuildString(sessionId), unsigned));
        await WritePacketAsync(stream, 50, Concat(unsigned.AsSpan(1).ToArray(), BuildString(signature)), sendSeq, writeCipher).ConfigureAwait(false);

        while (true)
        {
            var packet = await ReadPacketAsync(stream, recvSeq, readCipher).ConfigureAwait(false);
            if (packet.Type == 52)
                return;

            if (packet.Type == 53 || packet.Type == 60)
                continue;

            if (packet.Type == 51)
                throw new InvalidOperationException("SSH public key authentication failed.");
        }
    }

    private static async Task<string> ReadVersionLineAsync(NetworkStream stream)
    {
        using var ms = new MemoryStream();
        var one = new byte[1];
        while (true)
        {
            var n = await stream.ReadAsync(one).ConfigureAwait(false);
            if (n == 0)
                throw new InvalidOperationException("SSH stream closed before version exchange.");

            ms.WriteByte(one[0]);
            if (one[0] == (byte)'\n')
            {
                var line = Encoding.ASCII.GetString(ms.ToArray());
                if (line.StartsWith("SSH-", StringComparison.Ordinal))
                    return line;

                ms.SetLength(0);
            }
        }
    }

    private static Task WriteServiceRequestAsync(NetworkStream stream, string service, SequenceNumber sendSeq, PacketCipher? cipher = null)
    {
        return WritePacketAsync(stream, 5, BuildString(service), sendSeq, cipher);
    }

    private static async Task WritePacketAsync(NetworkStream stream, byte type, byte[] payload, SequenceNumber seq, PacketCipher? cipher = null)
    {
        var body = Concat([type], payload);
        var blockSize = cipher?.BlockSize ?? 8;
        var padding = blockSize - ((body.Length + 5) % blockSize);
        if (padding < 4)
            padding += blockSize;

        var packetLength = body.Length + padding + 1;
        var packet = new byte[4 + packetLength];
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(0, 4), checked((uint)packetLength));
        packet[4] = (byte)padding;
        body.CopyTo(packet.AsSpan(5));
        if (padding > 0)
            RandomNumberGenerator.Fill(packet.AsSpan(5 + body.Length, padding));

        var seqValue = seq.Value;
        byte[] output;
        if (cipher is null)
        {
            output = packet;
        }
        else
        {
            var encrypted = cipher.Transform(packet);
            var mac = HMACSHA256.HashData(cipher.MacKey, Concat(BuildUInt32(seqValue), packet));
            output = Concat(encrypted, mac);
        }

        await stream.WriteAsync(output).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
        seq.Increment();
    }

    private static async Task<SshPacket> ReadPacketAsync(NetworkStream stream, SequenceNumber seq, PacketCipher? cipher = null)
    {
        var seqValue = seq.Value;
        var header = await ReadExactAsync(stream, 4).ConfigureAwait(false);
        if (cipher is not null)
            header = cipher.Transform(header);

        var packetLength = BinaryPrimitives.ReadUInt32BigEndian(header);
        var packetBody = await ReadExactAsync(stream, checked((int)packetLength)).ConfigureAwait(false);
        if (cipher is not null)
        {
            var encryptedBody = packetBody;
            packetBody = cipher.Transform(encryptedBody);
            var mac = await ReadExactAsync(stream, cipher.MacSize).ConfigureAwait(false);
            var expected = HMACSHA256.HashData(cipher.MacKey, Concat(BuildUInt32(seqValue), header, packetBody));
            if (!CryptographicOperations.FixedTimeEquals(mac, expected.AsSpan(0, cipher.MacSize)))
                throw new CryptographicException("SSH packet MAC verification failed.");
        }

        seq.Increment();

        var paddingLength = packetBody[0];
        var payloadLength = checked((int)packetLength - paddingLength - 1);
        var payload = packetBody.AsSpan(1, payloadLength).ToArray();
        var type = payload[0];
        var data = payload.AsSpan(1).ToArray();
        return new SshPacket(type, data);
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(offset, length - offset)).ConfigureAwait(false);
            if (n == 0)
                throw new InvalidOperationException("SSH stream closed unexpectedly.");

            offset += n;
        }

        return buffer;
    }

    private static byte[] ComputeExchangeHash(
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        byte[] hostKeyBlob,
        BigInteger e,
        BigInteger f,
        BigInteger k)
    {
        var data = Concat(
            BuildString(TrimCrLf(clientVersion)),
            BuildString(TrimCrLf(serverVersion)),
            BuildString(Concat([20], clientKexInit)),
            BuildString(Concat([20], serverKexInit)),
            BuildString(hostKeyBlob),
            BuildMpint(e),
            BuildMpint(f),
            BuildMpint(k));

        return SHA256.HashData(data);
    }

    private static byte[] ComputeP256ExchangeHash(
        byte[] clientVersion,
        byte[] serverVersion,
        byte[] clientKexInit,
        byte[] serverKexInit,
        byte[] hostKeyBlob,
        byte[] qClient,
        byte[] qServer,
        BigInteger k)
    {
        var data = Concat(
            BuildString(TrimCrLf(clientVersion)),
            BuildString(TrimCrLf(serverVersion)),
            BuildString(Concat([20], clientKexInit)),
            BuildString(Concat([20], serverKexInit)),
            BuildString(hostKeyBlob),
            BuildString(qClient),
            BuildString(qServer),
            BuildMpint(k));

        return SHA256.HashData(data);
    }

    private static (PacketCipher Write, PacketCipher Read) CreatePacketCiphers(BigInteger sharedSecret, byte[] exchangeHash, byte[] sessionId)
    {
        var c2sIv = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'A', 16);
        var s2cIv = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'B', 16);
        var c2sKey = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'C', 16);
        var s2cKey = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'D', 16);
        var c2sMac = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'E', 32);
        var s2cMac = DeriveKey(sharedSecret, exchangeHash, sessionId, (byte)'F', 32);
        return (new PacketCipher(c2sKey, c2sIv, c2sMac), new PacketCipher(s2cKey, s2cIv, s2cMac));
    }

    private static byte[] DeriveKey(BigInteger sharedSecret, byte[] exchangeHash, byte[] sessionId, byte letter, int length)
    {
        var material = new List<byte>();
        var k = BuildMpint(sharedSecret);
        var previous = Array.Empty<byte>();
        while (material.Count < length)
        {
            var seed = previous.Length == 0
                ? Concat(k, exchangeHash, [letter], sessionId)
                : Concat(k, exchangeHash, previous);
            previous = SHA256.HashData(seed);
            material.AddRange(previous);
        }

        return material.Take(length).ToArray();
    }

    private static void VerifyHostSignature(byte[] hostKeyBlob, byte[] signatureBlob, byte[] exchangeHash)
    {
        var hostReader = new PacketReader(hostKeyBlob);
        var hostType = hostReader.ReadString();
        if (!string.Equals(hostType, "ssh-rsa", StringComparison.Ordinal))
            throw new NotSupportedException($"Unsupported host key type: {hostType}");

        var eBytes = hostReader.ReadStringBytes();
        var nBytes = hostReader.ReadStringBytes();

        var sigReader = new PacketReader(signatureBlob);
        var sigType = sigReader.ReadString();
        var sigRaw = sigReader.ReadStringBytes();

        var hashName = sigType switch
        {
            "rsa-sha2-256" => HashAlgorithmName.SHA256,
            "rsa-sha2-512" => HashAlgorithmName.SHA512,
            "ssh-rsa" => HashAlgorithmName.SHA1,
            _ => throw new NotSupportedException($"Unsupported host signature type: {sigType}"),
        };

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = UnsignedMpintToUnsignedBytes(nBytes),
            Exponent = UnsignedMpintToUnsignedBytes(eBytes),
        });

        if (!rsa.VerifyData(exchangeHash, sigRaw, hashName, RSASignaturePadding.Pkcs1))
            throw new InvalidOperationException("SSH host key signature verification failed.");
    }

    private static BigInteger Group14Prime()
    {
        const string hex =
            "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" +
            "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B" +
            "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9" +
            "A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE6" +
            "49286651ECE45B3DC2007CB8A163BF0598DA48361C55D39A69163FA8" +
            "FD24CF5F83655D23DCA3AD961C62F356208552BB9ED529077096966D" +
            "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3BE39E772C" +
            "180E86039B2783A2EC07A28FB5C55DF06F4C52C9DE2BCBF695581718" +
            "3995497CEA956AE515D2261898FA051015728E5A8AACAA68FFFFFFFF" +
            "FFFFFFFF";

        return new BigInteger(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
    }

    private static byte[] BuildNameList(IEnumerable<string> values)
    {
        return BuildString(string.Join(",", values));
    }

    private static NegotiatedAlgorithms NegotiateAlgorithms(byte[] serverKexInit)
    {
        var reader = new PacketReader(serverKexInit);
        _ = reader.ReadBytes(16);
        var kex = Pick(["ecdh-sha2-nistp256", "diffie-hellman-group14-sha256"], reader.ReadNameList(), "key exchange");
        var hostKey = Pick(["rsa-sha2-512", "rsa-sha2-256", "ssh-rsa"], reader.ReadNameList(), "host key");
        var cipherClientToServer = Pick(["aes128-ctr"], reader.ReadNameList(), "client cipher");
        var cipherServerToClient = Pick(["aes128-ctr"], reader.ReadNameList(), "server cipher");
        var macClientToServer = Pick(["hmac-sha2-256"], reader.ReadNameList(), "client MAC");
        var macServerToClient = Pick(["hmac-sha2-256"], reader.ReadNameList(), "server MAC");
        var compressionClientToServer = Pick(["none"], reader.ReadNameList(), "client compression");
        var compressionServerToClient = Pick(["none"], reader.ReadNameList(), "server compression");
        return new NegotiatedAlgorithms(
            kex,
            hostKey,
            cipherClientToServer,
            cipherServerToClient,
            macClientToServer,
            macServerToClient,
            compressionClientToServer,
            compressionServerToClient);
    }

    private static string Pick(IEnumerable<string> local, IReadOnlyCollection<string> remote, string label)
    {
        foreach (var name in local)
        {
            if (remote.Contains(name))
                return name;
        }

        throw new NotSupportedException($"No mutually supported SSH {label} algorithm. Server offered: {string.Join(", ", remote)}");
    }

    private static byte[] BuildString(string value)
    {
        return BuildString(Encoding.UTF8.GetBytes(value));
    }

    private static byte[] BuildString(ReadOnlySpan<byte> value)
    {
        return Concat(BuildUInt32(checked((uint)value.Length)), value.ToArray());
    }

    private static byte[] BuildString(byte[] value)
    {
        return BuildString(value.AsSpan());
    }

    private static byte[] BuildUInt32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return bytes;
    }

    private static byte[] BuildMpint(BigInteger value)
    {
        var bytes = UnsignedBigEndian(value);
        if (bytes.Length == 0)
            bytes = [0];

        if ((bytes[0] & 0x80) != 0)
            bytes = Concat([0], bytes);

        return BuildString(bytes);
    }

    private static BigInteger ReadMpint(byte[] bytes)
    {
        return new BigInteger(UnsignedMpintToUnsignedBytes(bytes), isUnsigned: true, isBigEndian: true);
    }

    private static byte[] UnsignedMpintToUnsignedBytes(byte[] bytes)
    {
        if (bytes.Length > 1 && bytes[0] == 0)
            return bytes[1..];

        return bytes;
    }

    private static byte[] UnsignedBigEndian(BigInteger value)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        return bytes.Length == 0 ? [0] : bytes;
    }

    private static byte[] TrimCrLf(byte[] value)
    {
        var end = value.Length;
        while (end > 0 && (value[end - 1] == (byte)'\r' || value[end - 1] == (byte)'\n'))
            end--;

        return end == value.Length ? value : value[..end];
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        if (arrays.Length == 0)
            return Array.Empty<byte>();

        var total = arrays.Sum(static a => a.Length);
        var output = new byte[total];
        var offset = 0;
        foreach (var arr in arrays)
        {
            Buffer.BlockCopy(arr, 0, output, offset, arr.Length);
            offset += arr.Length;
        }

        return output;
    }

    private async Task EnsureSessionChannelAsync()
    {
        if (this.channelOpen)
            return;

        var channel = await this.OpenChannelAsync("session", Array.Empty<byte>()).ConfigureAwait(false);
        this.localChannel = channel.Local;
        this.remoteChannel = channel.Remote;
        this.channelOpen = true;
    }

    private async Task<ChannelInfo> OpenChannelAsync(string channelType, byte[] typePayload)
    {
        var local = this.nextChannel++;
        var payload = Concat(
            BuildChannelOpenPayload(channelType, local, DefaultWindowSize, DefaultMaxPacketSize),
            typePayload);

        await WritePacketAsync(this.stream, 90, payload, this.sendSeq, this.writeCipher).ConfigureAwait(false);
        this.sentBytes += (ulong)payload.Length;

        while (true)
        {
            var packet = await ReadPacketAsync(this.stream, this.recvSeq, this.readCipher).ConfigureAwait(false);
            this.recvBytes += (ulong)packet.Payload.Length;
            if (packet.Type == 91)
            {
                var reader = new PacketReader(packet.Payload);
                var recipient = reader.ReadUInt32();
                if (recipient != local)
                    throw new InvalidOperationException("Channel confirmation recipient mismatch.");

                var remote = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                _ = reader.ReadUInt32();
                return new ChannelInfo(local, remote);
            }

            if (packet.Type == 92)
                throw new InvalidOperationException($"SSH channel open failed: {channelType}.");
        }
    }

    private async Task<string> ReadScpLineAsync()
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var one = await this.ReadChannelDataExactAsync(1).ConfigureAwait(false);
            if (one[0] == (byte)'\n')
                break;

            ms.WriteByte(one[0]);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private async Task ExpectScpOkAsync()
    {
        var code = await this.ReadChannelDataExactAsync(1).ConfigureAwait(false);
        if (code[0] == 0)
            return;

        var message = await this.ReadScpLineAsync().ConfigureAwait(false);
        throw new InvalidOperationException($"SCP error {code[0]}: {message}");
    }

    private readonly record struct SshPacket(byte type, byte[] payload)
    {
        public byte Type { get; } = type;

        public byte[] Payload { get; } = payload;
    }

    private readonly record struct KexOutcome(byte[] exchangeHash, byte[] hostKeyBlob, BigInteger sharedSecret)
    {
        public byte[] ExchangeHash { get; } = exchangeHash;

        public byte[] HostKeyBlob { get; } = hostKeyBlob;

        public BigInteger SharedSecret { get; } = sharedSecret;
    }

    private readonly record struct ChannelInfo(uint local, uint remote)
    {
        public uint Local { get; } = local;

        public uint Remote { get; } = remote;
    }

    private readonly record struct NegotiatedAlgorithms(
        string kex,
        string hostKey,
        string cipherClientToServer,
        string cipherServerToClient,
        string macClientToServer,
        string macServerToClient,
        string compressionClientToServer,
        string compressionServerToClient)
    {
        public string Kex { get; } = kex;

        public string HostKey { get; } = hostKey;

        public string CipherClientToServer { get; } = cipherClientToServer;

        public string CipherServerToClient { get; } = cipherServerToClient;

        public string MacClientToServer { get; } = macClientToServer;

        public string MacServerToClient { get; } = macServerToClient;

        public string CompressionClientToServer { get; } = compressionClientToServer;

        public string CompressionServerToClient { get; } = compressionServerToClient;
    }

    private sealed class SequenceNumber
    {
        private uint value;

        public uint Value => this.value;

        public void Increment()
        {
            this.value++;
        }
    }

    private sealed class PacketCipher
    {
        private readonly ICryptoTransform encryptor;
        private readonly byte[] counter;
        private byte[] streamBlock = [];
        private int streamOffset;

        public PacketCipher(byte[] key, byte[] iv, byte[] macKey)
        {
            using var aes = Aes.Create();
#pragma warning disable SCS0013 // SSH AES-CTR uses the block cipher primitive directly.
            aes.Mode = CipherMode.ECB;
#pragma warning restore SCS0013
            aes.Padding = PaddingMode.None;
            aes.Key = key;
            this.encryptor = aes.CreateEncryptor();
            this.counter = iv.ToArray();
            this.MacKey = macKey;
        }

        public int BlockSize => 16;

        public int MacSize => 32;

        public byte[] MacKey { get; }

        public byte[] Transform(byte[] input)
        {
            var output = new byte[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                if (this.streamOffset >= this.streamBlock.Length)
                {
                    this.streamBlock = new byte[16];
                    this.encryptor.TransformBlock(this.counter, 0, this.counter.Length, this.streamBlock, 0);
                    IncrementCounter(this.counter);
                    this.streamOffset = 0;
                }

                output[i] = (byte)(input[i] ^ this.streamBlock[this.streamOffset++]);
            }

            return output;
        }

        private static void IncrementCounter(byte[] counter)
        {
            for (var i = counter.Length - 1; i >= 0; i--)
            {
                counter[i]++;
                if (counter[i] != 0)
                    return;
            }
        }
    }

    private sealed class PacketReader
    {
        private readonly byte[] buffer;
        private int offset;

        public PacketReader(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public uint ReadUInt32()
        {
            if (this.offset + 4 > this.buffer.Length)
                throw new InvalidOperationException("Unexpected end of SSH packet.");

            var value = BinaryPrimitives.ReadUInt32BigEndian(this.buffer.AsSpan(this.offset, 4));
            this.offset += 4;
            return value;
        }

        public bool ReadBoolean()
        {
            if (this.offset >= this.buffer.Length)
                throw new InvalidOperationException("Unexpected end of SSH packet.");

            return this.buffer[this.offset++] != 0;
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(this.ReadStringBytes());
        }

        public IReadOnlyList<string> ReadNameList()
        {
            var value = this.ReadString();
            return value.Length == 0 ? Array.Empty<string>() : value.Split(',');
        }

        public byte[] ReadBytes(int length)
        {
            if (this.offset + length > this.buffer.Length)
                throw new InvalidOperationException("Unexpected end of SSH packet.");

            var data = this.buffer.AsSpan(this.offset, length).ToArray();
            this.offset += length;
            return data;
        }

        public byte[] ReadStringBytes()
        {
            var length = checked((int)this.ReadUInt32());
            if (this.offset + length > this.buffer.Length)
                throw new InvalidOperationException("Unexpected end of SSH packet.");

            var data = this.buffer.AsSpan(this.offset, length).ToArray();
            this.offset += length;
            return data;
        }
    }
}