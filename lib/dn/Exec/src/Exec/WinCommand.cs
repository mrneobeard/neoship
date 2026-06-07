namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing Windows Command Prompt (cmd) commands. This class provides functionality
/// to configure and execute cmd commands, including registering path hints for locating the
/// cmd executable on different platforms. It inherits from <see cref="ShellCommand{TCommand, TOptions}"/> and
/// <see cref="Command{TCommand, TOptions}"/>. It uses <see cref="WinCommandOptions"/> for its configuration options.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
/// var output = new WinCommand("echo Hello, world!")
///  .Output();
/// Console.WriteLine(output.Text());
/// Console.WriteLine($"Exit code: {output.ExitCode}");
/// </code>
/// </example>
/// </remarks>
public sealed class WinCommand : ShellCommand<WinCommand, WinCommandOptions>, ICommandOptionsOwner
{
    public WinCommand()
    {
    }

    public WinCommand(string script)
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the PowerShell Core (pwsh) path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>PWSH_EXE</c> can be used to specify the path to the PowerShell Core executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for PowerShell versions 6 and 7, including
    ///   preview versions. For Linux, it checks common locations like `/usr/bin/pwsh` and `/usr/local/bin/pwsh`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// WinCommand.RegisterPathHint();
    /// using var cmd = new WinCommand("echo hello");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("cmd", (hint) =>
        {
            hint.Executable = "cmd";
            hint.Variable = "CMD_EXE";
            hint.Windows = [
            "${SystemRoot}\\System32\\cmd.exe",
            ];
        });
    }
}