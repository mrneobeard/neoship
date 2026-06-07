namespace NeoBeard.Exec;

/// <summary>
/// The exception that is thrown when a process exits with a non-zero exit code or fails to execute.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var output = new Command(["git", "invalid-command"]).Output();
/// try
/// {
///     output.ThrowOnBadExit();
/// }
/// catch (ProcessException ex)
/// {
///     Console.WriteLine($"Process failed: {ex.Message}");
/// }
/// </code>
/// </example>
/// </remarks>
public class ProcessException : SystemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ProcessException();
    /// Assert.Equal("Exception of type 'NeoBeard.Exec.ProcessException' was thrown.", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ProcessException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The exit code of the failed process.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ProcessException(1);
    /// Assert.Equal("Process exited with code 1", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ProcessException(int exitCode)
        : base($"Process exited with code {exitCode}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with the specified exit code, process name, message, and inner exception.
    /// </summary>
    /// <param name="exitCode">The exit code of the failed process.</param>
    /// <param name="processName">The name of the process that failed.</param>
    /// <param name="message">A message that describes the error. If null, a default message is generated.</param>
    /// <param name="inner">The exception that is the cause of this exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ProcessException(1, "git", "Git command failed", null);
    /// Assert.Equal("Git command failed", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ProcessException(int exitCode, string processName, string? message = null, Exception? inner = null)
        : base(message ?? $"Process {processName} exited with code {exitCode}", inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ProcessException("Process failed to start");
    /// Assert.Equal("Process failed to start", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ProcessException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">A message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of this exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inner = new InvalidOperationException("Inner error");
    /// var ex = new ProcessException("Process failed", inner);
    /// Assert.Equal("Process failed", ex.Message);
    /// Assert.Same(inner, ex.InnerException);
    /// </code>
    /// </example>
    /// </remarks>
    public ProcessException(string? message, System.Exception? inner)
        : base(message, inner)
    {
    }
}