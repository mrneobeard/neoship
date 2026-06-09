namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing Python scripts or commands. This class provides functionality
/// to configure and execute Python commands, including registering path hints for locating the
/// Python executable on different platforms. It inherits from
/// <see cref="ShellCommand{TCommand, TOptions}"/> and <see cref="Command{TCommand, TOptions}"/>.
/// It  uses <see cref="PythonCommandOptions"/> for
/// its configuration options.
/// </summary>
public sealed class PythonCommand : ShellCommand<PythonCommand, PythonCommandOptions>
{
    public PythonCommand()
    {
    }

    public PythonCommand(string script)
        : this()
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the bash path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>PYTHON_EXE</c> can be used to specify the path to the Python executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for Git Python. For Linux, it
    ///   checks common locations like `/usr/bin/python3` and `/usr/local/bin/python3`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// PythonCommand.RegisterPathHint();
    /// using var cmd = new PythonCommand("print('hello')");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("python", (hint) =>
        {
            hint.Variable = "PYTHON_EXE";
            hint.Linux = [
                "/usr/bin/python3",
                "/usr/local/bin/python3",
                "/usr/bin/python",
                "/usr/local/bin/python",
            ];

            hint.Windows = [
                "${LOCALAPPDATA}\\Programs\\Python\\Python314\\python.exe",
                "${ProgramFiles}\\Python\\Python314\\python.exe",
                "${LOCALAPPDATA}\\Programs\\Python\\Python313\\python.exe",
                "${ProgramFiles}\\Python\\Python313\\python.exe",
                "${LOCALAPPDATA}\\Programs\\Python\\Python312\\python.exe",
                "${ProgramFiles}\\Python\\Python312\\python.exe",
                "${LOCALAPPDATA}\\Programs\\Python\\Python311\\python.exe",
                "${ProgramFiles}\\Python\\Python311\\python.exe",
                "${LOCALAPPDATA}\\Programs\\Python\\Python310\\python.exe",
                "${ProgramFiles}\\Python\\Python310\\python.exe",
            ];
        });
    }
}