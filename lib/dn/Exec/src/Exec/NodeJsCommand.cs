namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing Node.js scripts or commands.
/// This class provides functionality to configure and execute Node.js commands,
/// including registering path hints for locating the Node.js executable on different
/// platforms. It inherits from <see cref="ShellCommand{TCommand, TOptions}"/> and
/// <see cref="Command{TCommand, TOptions}"/>. It
/// uses <see cref="NodeJsCommandOptions"/> for its configuration options.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
/// var output = new NodeJsCommand("console.log('Hello, world!');")
///   .Output();
/// Console.WriteLine(output.Text());
/// Console.WriteLine($"Exit code: {output.ExitCode}");
/// </code>
/// </example>
/// </remarks>
public sealed class NodeJsCommand : ShellCommand<NodeJsCommand, NodeJsCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeJsCommand"/> class with default settings for executing Node.js commands.
    /// </summary>
    public NodeJsCommand()
    {
    }

    public NodeJsCommand(string script)
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
    ///   The default paths for windows look under Program Files for Git NodeJs. For Linux, it
    ///   checks common locations like `/usr/bin/bash` and `/usr/local/bin/bash`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// NodeJsCommand.RegisterPathHint();
    /// using var cmd = new NodeJsCommand("console.log('hello')");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("node", (hint) =>
        {
            hint.Variable = "NODE_EXE";
            hint.Linux = [
                "/usr/bin/node",
                "/usr/local/bin/node",
                "${XDG_DATA_HOME}/fnm/aliases/default/bin/node",
                "${HOME}/.local/share/fnm/aliases/default/bin/node",
                "${HOME}/.fnm/aliases/default/bin/node",
            ];

            hint.Windows = [
                "${HOME}\\.fnm\\aliases\\default\\bin\\node.exe",
                "${LOCALAPPDATA}\\fnm\\aliases\\default\\bin\\node.exe",
                "${XDG_DATA_HOME}\\fnm\\aliases\\default\\bin\\node.exe",
                "${ProgramFiles}\\nodejs\\node.exe",
                "${ProgramFiles(x86)}\\nodejs\\node.exe",
            ];
        });
    }
}