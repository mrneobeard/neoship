namespace NeoBeard.Exec;

/// <summary>
/// A command for executing Deno scripts or commands.
/// </summary>
/// <remarks>
/// <example>
///  <code language="csharp">
///   var output = new DenoCommand("console.log('Hello, world!');")
///      .Output();
///   Console.WriteLine(output.Text());
///   Console.WriteLine($"Exit code: {output.ExitCode}");
///  </code>
/// </example>
/// </remarks>
public sealed class DenoCommand : ShellCommand<DenoCommand, DenoCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenoCommand"/> class with default settings for executing Deno commands.
    /// </summary>
    public DenoCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenoCommand"/> class with the
    /// specified Deno script or command.
    /// </summary>
    /// <param name="script">The Deno script or command to execute.</param>
    public DenoCommand(string script)
        : this()
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the Deno path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>DENO_EXE</c> can be used to specify the path to the Deno executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for Git Deno. For Linux, it
    ///   checks common locations like `/usr/bin/deno` and `/usr/local/bin/deno`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// DenoCommand.RegisterPathHint();
    /// using var cmd = new DenoCommand("console.log('hello')");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("deno", (hint) =>
        {
            hint.Variable = "DENO_EXE";
            hint.Linux = [
                "/usr/bin/deno",
                "/usr/local/bin/deno",
                "${HOME}/.deno/bin/deno",
                "${HOME}/.local/bin/deno",
            ];

            hint.Windows = [
                "${HOME}\\.deno\\bin\\deno.exe",
                "${LOCALAPPDATA}\\Microsoft\\WinGet\\Packages\\DenoLand.Deno_Microsoft.Winget.Source_8wekyb3d8bbwe\\deno.exe",
                "${LOCALAPPDATA}\\Programs\\bin\\deno.exe",
                "${HOME}\\.local\\bin\\deno.exe",
                "${ProgramFiles}\\Deno\\deno.exe",
                "${ProgramFiles(x86)}\\Deno\\deno.exe",
            ];
        });
    }
}