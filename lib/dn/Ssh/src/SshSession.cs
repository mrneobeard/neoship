using System.Text;

namespace NeoBeard.Ssh;

/// <summary>
/// Represents a Go-style SSH session abstraction.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Created by SshClient.NewSession().
/// </code>
/// </example>
/// </remarks>
public sealed class SshSession
{
    private readonly SshClient client;
    private readonly SshSessionOptions options;
    private bool started;
    private SshExecResult? finalResult;

    /// <summary>
    /// Creates a session wrapper for a connected SSH client.
    /// </summary>
    /// <param name="client">The owning SSH client.</param>
    /// <param name="options">The session options.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// // Created by SshClient.NewSession().
    /// </code>
    /// </example>
    /// </remarks>
    internal SshSession(SshClient client, SshSessionOptions options)
    {
        this.client = client;
        this.options = options;
    }

    /// <summary>
    /// Applies an env key/value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = await session.SetenvAsync("LANG", "C.UTF-8");
    /// </code>
    /// </example>
    /// </remarks>
    public Task<bool> SetenvAsync(string name, string value)
    {
        return this.SetenvAsync(name, Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Bytes">The variable value encoded as UTF-8.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = await session.SetenvAsync("LANG", Encoding.UTF8.GetBytes("C.UTF-8"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task<bool> SetenvAsync(string name, byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.client.SetSessionEnvAsync(name, utf8Bytes.AsSpan(), this.options.StrictEnv ?? this.client.Config.StrictEnv);
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Bytes">The variable value encoded as UTF-8 bytes.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = await session.SetenvAsync("LANG", Encoding.UTF8.GetBytes("C.UTF-8").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<bool> SetenvAsync(string name, ReadOnlySpan<byte> utf8Bytes)
    {
        return this.client.SetSessionEnvAsync(name, utf8Bytes, this.options.StrictEnv ?? this.client.Config.StrictEnv);
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Chars">The variable value encoded as UTF-8 characters.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = await session.SetenvAsync("LANG", Encoding.UTF8.GetBytes("C.UTF-8").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<bool> SetenvAsync(string name, ReadOnlySpan<char> utf8Chars)
    {
        return this.SetenvAsync(name, EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Applies an env key/value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The variable value.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = session.Setenv("LANG", "C.UTF-8");
    /// </code>
    /// </example>
    /// </remarks>
    public bool Setenv(string name, string value)
    {
        return this.Setenv(name, Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Bytes">The variable value encoded as UTF-8.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = session.Setenv("LANG", Encoding.UTF8.GetBytes("C.UTF-8"));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Setenv(string name, byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.client.SetSessionEnv(name, utf8Bytes.AsSpan(), this.options.StrictEnv ?? this.client.Config.StrictEnv);
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Bytes">The variable value encoded as UTF-8.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = session.Setenv("LANG", Encoding.UTF8.GetBytes("C.UTF-8"));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Setenv(string name, ReadOnlySpan<byte> utf8Bytes)
    {
        return this.client.SetSessionEnv(name, utf8Bytes, this.options.StrictEnv ?? this.client.Config.StrictEnv);
    }

    /// <summary>
    /// Applies a UTF-8 encoded env value to this session.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="utf8Chars">The variable value encoded as UTF-8 characters.</param>
    /// <returns>A value indicating whether the remote accepted the request.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool ok = session.Setenv("LANG", Encoding.UTF8.GetBytes("C.UTF-8").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public bool Setenv(string name, ReadOnlySpan<char> utf8Chars)
    {
        return this.Setenv(name, EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.StartAsync("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task StartAsync(string command)
    {
        await this.StartAsync(Encoding.UTF8.GetBytes(command)).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.StartAsync(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task StartAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.StartCoreAsync(utf8Bytes);
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.StartAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task StartAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.StartAsync(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>A task that completes when start is acknowledged.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.StartAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task StartAsync(ReadOnlySpan<char> utf8Chars)
    {
        return this.StartAsync(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Start("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public void Start(string command)
    {
        this.Start(Encoding.UTF8.GetBytes(command));
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Start(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Start(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        this.StartCore(utf8Bytes);
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Start(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Start(ReadOnlySpan<byte> utf8Bytes)
    {
        this.Start(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Starts an exec request without waiting for completion.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Start(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public void Start(ReadOnlySpan<char> utf8Chars)
    {
        this.Start(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Waits for a started command and returns the final result.
    /// </summary>
    /// <returns>The command result.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// SshExecResult result = await session.WaitAsync();
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<SshExecResult> WaitAsync()
    {
        if (!this.started)
            throw new InvalidOperationException("Session not started.");

        this.finalResult ??= await this.client.CollectExecResultAsync().ConfigureAwait(false);
        return this.finalResult;
    }

    /// <summary>
    /// Waits for a started command and returns the final result.
    /// </summary>
    /// <returns>The command result.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// SshExecResult result = session.Wait();
    /// </code>
    /// </example>
    /// </remarks>
    public SshExecResult Wait()
    {
        return this.WaitCore();
    }

    /// <summary>
    /// Runs a command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>A task that completes when the command exits successfully.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.RunAsync("true");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task RunAsync(string command)
    {
        await this.RunAsync(Encoding.UTF8.GetBytes(command)).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>A task that completes when the command exits successfully.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.RunAsync(Encoding.UTF8.GetBytes("true"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.RunCoreAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>A task that completes when the command exits successfully.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.RunAsync(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.RunAsync(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>A task that completes when the command exits successfully.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.RunAsync(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task RunAsync(ReadOnlySpan<char> utf8Chars)
    {
        return this.RunAsync(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Runs a command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Run("true");
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(string command)
    {
        this.Run(Encoding.UTF8.GetBytes(command));
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Run(Encoding.UTF8.GetBytes("true"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        this.RunCore(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Run(Encoding.UTF8.GetBytes("true"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(ReadOnlySpan<byte> utf8Bytes)
    {
        this.Run(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command to completion and throws on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.Run(Encoding.UTF8.GetBytes("true").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public void Run(ReadOnlySpan<char> utf8Chars)
    {
        this.Run(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Runs a command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.OutputAsync("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<string> OutputAsync(string command)
    {
        return await this.OutputAsync(Encoding.UTF8.GetBytes(command)).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.OutputAsync(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.OutputCoreAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.OutputAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.OutputAsync(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.OutputAsync(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> OutputAsync(ReadOnlySpan<char> utf8Chars)
    {
        return this.OutputAsync(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Runs a command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.Output("whoami");
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(string command)
    {
        return this.Output(Encoding.UTF8.GetBytes(command));
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.Output(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.OutputCore(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.Output(Encoding.UTF8.GetBytes("whoami"));
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.Output(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns captured stdout, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The captured stdout text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.Output(Encoding.UTF8.GetBytes("whoami").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public string Output(ReadOnlySpan<char> utf8Chars)
    {
        return this.Output(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Runs a command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.CombinedOutputAsync("printf o; printf e 1&gt;&amp;2");
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<string> CombinedOutputAsync(string command)
    {
        return await this.CombinedOutputAsync(Encoding.UTF8.GetBytes(command)).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.CombinedOutputCoreAsync(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8 bytes.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.CombinedOutputAsync(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = await session.CombinedOutputAsync(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task<string> CombinedOutputAsync(ReadOnlySpan<char> utf8Chars)
    {
        return this.CombinedOutputAsync(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Runs a command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="command">The remote command.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.CombinedOutput("printf o; printf e 1&gt;&amp;2");
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(string command)
    {
        return this.CombinedOutput(Encoding.UTF8.GetBytes(command));
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.CombinedOutput(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2"));
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(byte[] utf8Bytes)
    {
        ArgumentNullException.ThrowIfNull(utf8Bytes);
        return this.CombinedOutputCore(utf8Bytes);
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Bytes">The remote command encoded as UTF-8.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.CombinedOutput(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2"));
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(ReadOnlySpan<byte> utf8Bytes)
    {
        return this.CombinedOutput(utf8Bytes.ToArray());
    }

    /// <summary>
    /// Runs a UTF-8 encoded command and returns combined stdout+stderr, throwing on non-zero exit.
    /// </summary>
    /// <param name="utf8Chars">The remote command encoded as UTF-8 characters.</param>
    /// <returns>The concatenated output text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string output = session.CombinedOutput(Encoding.UTF8.GetBytes("printf o; printf e 1&gt;&amp;2").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public string CombinedOutput(ReadOnlySpan<char> utf8Chars)
    {
        return this.CombinedOutput(EncodeUtf8(utf8Chars));
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>A task that completes when write is flushed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.WriteStdinAsync(Encoding.UTF8.GetBytes("hello\n"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task WriteStdinAsync(byte[] data)
    {
        return this.client.WriteChannelDataAsync(data.AsSpan());
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send as UTF-8 characters.</param>
    /// <returns>A task that completes when write is flushed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.WriteStdinAsync(Encoding.UTF8.GetBytes("hello\n").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public Task WriteStdinAsync(ReadOnlySpan<char> data)
    {
        return this.WriteStdinAsync(EncodeUtf8(data));
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>A task that completes when write is flushed.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// await session.WriteStdinAsync(Encoding.UTF8.GetBytes("hello\n"));
    /// </code>
    /// </example>
    /// </remarks>
    public Task WriteStdinAsync(ReadOnlySpan<byte> data)
    {
        return this.client.WriteChannelDataAsync(data);
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.WriteStdin(Encoding.UTF8.GetBytes("hello\n"));
    /// </code>
    /// </example>
    /// </remarks>
    public void WriteStdin(byte[] data)
    {
        this.client.WriteChannelData(data.AsSpan());
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send as UTF-8 characters.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.WriteStdin(Encoding.UTF8.GetBytes("hello\n").AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public void WriteStdin(ReadOnlySpan<char> data)
    {
        this.WriteStdin(EncodeUtf8(data));
    }

    /// <summary>
    /// Writes bytes to the remote stdin stream.
    /// </summary>
    /// <param name="data">The bytes to send.</param>
    /// <returns>Does not return a value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// session.WriteStdin(Encoding.UTF8.GetBytes("hello\n"));
    /// </code>
    /// </example>
    /// </remarks>
    public void WriteStdin(ReadOnlySpan<byte> data)
    {
        this.client.WriteChannelData(data);
    }

    private async Task StartCoreAsync(byte[] command)
    {
        if (this.started)
            throw new InvalidOperationException("Session already started.");

        this.started = true;
        await this.client.PrepareSessionEnvAsync(this.options).ConfigureAwait(false);
        await this.client.SendExecStartAsync(command.AsSpan()).ConfigureAwait(false);
    }

    private void StartCore(byte[] command)
    {
        if (this.started)
            throw new InvalidOperationException("Session already started.");

        this.started = true;
        this.client.PrepareSessionEnv(this.options);
        this.client.SendExecStart(command.AsSpan());
    }

    private SshExecResult WaitCore()
    {
        if (!this.started)
            throw new InvalidOperationException("Session not started.");

        this.finalResult ??= this.client.CollectExecResult();
        return this.finalResult;
    }

    private async Task RunCoreAsync(byte[] command)
    {
        await this.StartCoreAsync(command).ConfigureAwait(false);
        var result = await this.WaitAsync().ConfigureAwait(false);
        ThrowIfRemoteFailed(result);
    }

    private void RunCore(byte[] command)
    {
        this.StartCore(command);
        var result = this.WaitCore();
        ThrowIfRemoteFailed(result);
    }

    private async Task<string> OutputCoreAsync(byte[] command)
    {
        await this.StartCoreAsync(command).ConfigureAwait(false);
        var result = await this.WaitAsync().ConfigureAwait(false);
        ThrowIfRemoteFailed(result);
        return result.Stdout;
    }

    private string OutputCore(byte[] command)
    {
        this.StartCore(command);
        var result = this.WaitCore();
        ThrowIfRemoteFailed(result);
        return result.Stdout;
    }

    private async Task<string> CombinedOutputCoreAsync(byte[] command)
    {
        await this.StartCoreAsync(command).ConfigureAwait(false);
        var result = await this.WaitAsync().ConfigureAwait(false);
        ThrowIfRemoteFailed(result);
        return string.Concat(result.Stdout, result.Stderr);
    }

    private string CombinedOutputCore(byte[] command)
    {
        this.StartCore(command);
        var result = this.WaitCore();
        ThrowIfRemoteFailed(result);
        return string.Concat(result.Stdout, result.Stderr);
    }

    private static byte[] EncodeUtf8(ReadOnlySpan<char> value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var bytes = new byte[byteCount];
        _ = Encoding.UTF8.GetBytes(value, bytes);
        return bytes;
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
}