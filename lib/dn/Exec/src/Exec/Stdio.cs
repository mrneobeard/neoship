namespace NeoBeard.Exec;

/// <summary>
/// Specifies how standard input, output, and error streams should be handled for a process.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var command = new Command(["dotnet", "--version"])
///     .WithStdout(Stdio.Piped)
///     .WithStderr(Stdio.Piped);
/// var output = command.Output();
/// Console.WriteLine(output.Text());
/// </code>
/// </example>
/// </remarks>
public enum Stdio
{
    /// <summary>
    /// The stream is inherited from the parent process.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// The stream is piped, allowing the parent process to read from or write to it.
    /// </summary>
    Piped = 1,

    /// <summary>
    /// The stream is discarded (redirected to /dev/null on Unix or NUL on Windows).
    /// </summary>
    Null = 2,
}