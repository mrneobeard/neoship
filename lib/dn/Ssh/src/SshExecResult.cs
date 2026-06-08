namespace NeoBeard.Ssh;

/// <summary>
/// Represents the outcome of an SSH command execution.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = new SshExecResult(0, null, "ok", string.Empty);
/// Console.WriteLine(result.ExitCode);
/// </code>
/// </example>
/// </remarks>
public sealed class SshExecResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshExecResult"/> class.
    /// </summary>
    /// <param name="exitCode">The exit code from the remote process.</param>
    /// <param name="signal">The optional exit signal.</param>
    /// <param name="stdout">The captured standard output.</param>
    /// <param name="stderr">The captured standard error.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = new SshExecResult(0, null, "hello", string.Empty);
    /// </code>
    /// </example>
    /// </remarks>
    public SshExecResult(int? exitCode, string? signal, string stdout, string stderr)
    {
        this.ExitCode = exitCode;
        this.Signal = signal;
        this.Stdout = stdout;
        this.Stderr = stderr;
    }

    /// <summary>
    /// Gets the remote exit code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// int? code = result.ExitCode;
    /// </code>
    /// </example>
    /// </remarks>
    public int? ExitCode { get; }

    /// <summary>
    /// Gets the remote exit signal when available.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string? signal = result.Signal;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Signal { get; }

    /// <summary>
    /// Gets the captured standard output text.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string stdout = result.Stdout;
    /// </code>
    /// </example>
    /// </remarks>
    public string Stdout { get; }

    /// <summary>
    /// Gets the captured standard error text.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string stderr = result.Stderr;
    /// </code>
    /// </example>
    /// </remarks>
    public string Stderr { get; }
}