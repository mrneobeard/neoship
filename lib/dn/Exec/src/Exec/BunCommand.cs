namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing bun scripts or commands with configurable options.
/// </summary>
/// <remarks>
/// <example>
///   <code language="csharp">
///     var output = new BunCommand("console.log('Hello, world!');")
///        .Output();
///     Console.WriteLine(output.Text());
///     Console.WriteLine($"Exit code: {output.ExitCode}");
///   </code>
/// </example>
/// </remarks>
public sealed class BunCommand : ShellCommand<BunCommand, BunCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BunCommand"/> class with default settings for executing bun commands.
    /// </summary>
    public BunCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BunCommand"/> class with the
    /// specified bun script or command.
    /// </summary>
    /// <param name="script">The bun script or command to execute.</param>
    public BunCommand(string script)
        : this()
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the bun path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>BUN_EXE</c> can be used to specify the path to the bun executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for Git Bun. For Linux, it
    ///   checks common locations like `/usr/bin/bun` and `/usr/local/bin/bun`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// BunCommand.RegisterPathHint();
    /// using var cmd = new BunCommand("console.log('hello')");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("bun", (hint) =>
        {
            hint.Variable = "BUN_EXE";
            hint.Linux = [
                "/usr/bin/bun",
                "/usr/local/bin/bun",
                "${HOME}/.bun/bin/bun",
                "${HOME}/.local/bin/bun",
            ];

            hint.Windows = [
                "${HOME}\\.bun\\bin\\bun.exe",
                "${LOCALAPPDATA}\\Microsoft\\WinGet\\Links\\bun.exe",
                "${LOCALAPPDATA}\\Programs\\bin\\bun.exe",
                "${HOME}\\.local\\bin\\bun.exe",
                "${ProgramFiles}\\bun\\bun.exe",
                "${ProgramFiles(x86)}\\bun\\bun.exe",
            ];
        });
    }
}