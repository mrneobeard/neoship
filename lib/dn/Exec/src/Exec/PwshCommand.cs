namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing PowerShell Core (pwsh) scripts or
/// commands. This class provides functionality to configure and execute
/// PowerShell commands, including registering path hints for locating
/// the PowerShell Core executable on different platforms.
/// It inherits from <see cref="ShellCommand{TCommand, TOptions}"/> and
/// <see cref="Command{TCommand, TOptions}"/>. It
/// and uses <see cref="PwshCommandOptions"/> for its configuration options.
/// </summary>
/// <remarks>
/// <para>
///   It inherits from <see cref="ShellCommand{TCommand, TOptions}"/>
///   and uses <see cref="PwshCommandOptions"/> for its configuration options.
/// </para>
/// <example>
/// <code language="csharp">
/// var output = new PwshCommand("Write-Host 'Hello, world!'")
///  .Output();
/// Console.WriteLine(output.Text());
/// Console.WriteLine($"Exit code: {output.ExitCode}");
/// </code>
/// </example>
/// </remarks>
public sealed class PwshCommand : ShellCommand<PwshCommand, PwshCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PwshCommand"/> class with
    /// default settings for executing PowerShell commands.
    /// </summary>
    public PwshCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PwshCommand"/> class with the
    /// specified PowerShell script or command.
    /// </summary>
    /// <param name="script">The PowerShell script or command to execute.</param>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// var output = new PwshCommand("Write-Host 'Hello, world!'")
    ///   .Output();
    /// Console.WriteLine(output.Text());
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public PwshCommand(string script)
        : this()
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
    /// PwshCommand.RegisterPathHint();
    /// using var cmd = new PwshCommand("Write-Host 'hello'");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("pwsh", (hint) =>
        {
            hint.Executable = "pwsh";
            hint.Variable = "PWSH_EXE";
            hint.Windows = [
                "${ProgramFiles}\\PowerShell\\7\\pwsh.exe",
                "${ProgramFiles}\\PowerShell\\7-preview\\pwsh.exe",
                "${ProgramFiles}\\PowerShell\\6\\pwsh.exe",
                "${ProgramFiles}\\PowerShell\\6-preview\\pwsh.exe",
            ];
            hint.Linux = [
                "/usr/bin/pwsh",
                "/usr/local/bin/pwsh",
                "$HOME/.local/bin/pwsh",
            ];
        });
    }
}