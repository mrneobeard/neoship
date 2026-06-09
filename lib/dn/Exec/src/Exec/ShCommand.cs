namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing shell scripts or commands using the sh shell. This class provides functionality
/// to configure and execute sh commands, including registering path hints for locating the
/// sh executable on different platforms. It inherits from
/// <see cref="ShellCommand{TCommand, TOptions}"/> and <see cref="Command{TCommand, TOptions}"/>.
/// It uses <see cref="ShCommandOptions"/> for its configuration options.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
/// var output = new ShCommand("echo 'Hello, world!'")
///   .Output();
/// Console.WriteLine(output.Text());
/// Console.WriteLine($"Exit code: {output.ExitCode}");
/// </code>
/// </example>
/// </remarks>
public sealed class ShCommand : ShellCommand<ShCommand, ShCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShCommand"/> class with
    /// default settings for executing sh commands.
    /// </summary>
    public ShCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShCommand"/> class with the
    /// specified sh script or command.
    /// </summary>
    /// <param name="script">The sh script or command to execute.</param>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// var output = new ShCommand("echo 'Hello, world!'")
    ///  .Output();
    /// Console.WriteLine(output.Text());
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public ShCommand(string script)
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the sh path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>SH_EXE</c> can be used to specify the path to the sh executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for Windows look under Program Files for Git installations.
    ///   For Linux, it checks common locations like `/usr/bin/sh` and `/usr/local/bin/sh`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// ShCommand.RegisterPathHint();
    /// using var cmd = new ShCommand("echo hello");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("sh", (hint) =>
        {
            hint.Executable = "sh";
            hint.Variable = "SH_EXE";
            hint.Windows = [
                "${ProgramFiles}\\Git\\usr\\bin\\sh.exe",
            ];
            hint.Linux = [
                "/usr/bin/sh",
                "/usr/local/bin/sh",
            ];
        });
    }
}