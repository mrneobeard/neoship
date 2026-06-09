namespace NeoBeard.Ssh;

/// <summary>
/// Represents a non-zero remote SSH command exit.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// try
/// {
///     throw new SshRemoteException("failed", 1, null, "stderr");
/// }
/// catch (SshRemoteException ex)
/// {
///     Console.WriteLine(ex.ExitCode);
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class SshRemoteException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshRemoteException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="exitCode">The remote exit code.</param>
    /// <param name="signal">The remote signal.</param>
    /// <param name="stderr">The captured standard error.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new SshRemoteException("failed", 1, null, "error");
    /// </code>
    /// </example>
    /// </remarks>
    public SshRemoteException(string message, int? exitCode, string? signal, string stderr)
        : base(message)
    {
        this.ExitCode = exitCode;
        this.Signal = signal;
        this.Stderr = stderr;
    }

    /// <summary>
    /// Gets the remote exit code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// int? code = ex.ExitCode;
    /// </code>
    /// </example>
    /// </remarks>
    public int? ExitCode { get; }

    /// <summary>
    /// Gets the remote signal.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string? signal = ex.Signal;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Signal { get; }

    /// <summary>
    /// Gets the captured remote standard error.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string stderr = ex.Stderr;
    /// </code>
    /// </example>
    /// </remarks>
    public string Stderr { get; }
}