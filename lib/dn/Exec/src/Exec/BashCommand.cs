namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing bash scripts or commands with configurable options.
/// </summary>
public sealed class BashCommand : ShellCommand<BashCommand, BashCommandOptions>
{
    private static bool s_prepended = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="BashCommand"/> class with default settings for executing bash commands.
    /// </summary>
    public BashCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BashCommand"/> class with the specified bash script or command.
    /// </summary>
    /// <param name="script">The bash script or command to execute.</param>
    public BashCommand(string script)
        : this()
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the bash path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>BASH_EXE</c> can be used to specify the path to the bash executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for Git Bash. For Linux, it
    ///   checks common locations like `/usr/bin/bash` and `/usr/local/bin/bash`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// BashCommand.RegisterPathHint();
    /// using var cmd = new BashCommand("echo hello");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("bash", (hint) =>
        {
            hint.Executable = "bash";
            hint.Variable = "BASH_EXE";
            hint.Windows = [
                "C:\\Program Files\\Git\\bin\\bash.exe",
                "C:\\Program Files\\Git\\usr\\bin\\bash.exe",
            ];
            hint.Linux = [
                "/usr/bin/bash",
                "/usr/local/bin/bash",
            ];
        });
    }

    /// <summary>
    /// Prepends the Git Bash path to the system PATH environment variable on Windows.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// BashCommand.PrependWindowsGitPath();
    /// </code>
    /// </example>
    /// </remarks>
    public static void PrependWindowsGitPath()
    {
        if (s_prepended)
        {
            return;
        }

        s_prepended = true;
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var programFiles = Env.Expand("${ProgramFiles}");
        if (programFiles.IsNullOrWhiteSpace())
        {
            return;
        }

        var path = Env.Expand($"{programFiles}\\Git\\usr\\bin");
        Env.PrependPath(path);
    }
}