namespace NeoBeard.Exec;

/// <summary>
/// Represents a command for executing Ruby scripts or commands. This class provides functionality
/// to configure and execute Ruby commands, including registering path hints for locating the
/// Ruby executable on different platforms. It inherits from
/// <see cref="ShellCommand{TCommand, TOptions}"/> and <see cref="Command{TCommand, TOptions}"/>.
/// It uses <see cref="RubyCommandOptions"/> for its configuration options.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
/// var output = new RubyCommand("puts 'Hello, world!'")
///   .Output();
/// Console.WriteLine(output.Text());
/// Console.WriteLine($"Exit code: {output.ExitCode}");
/// </code>
/// </example>
/// </remarks>
public sealed class RubyCommand : ShellCommand<RubyCommand, RubyCommandOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RubyCommand"/> class with
    /// default settings for executing Ruby commands.
    /// </summary>
    public RubyCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RubyCommand"/> class with the
    /// specified Ruby script or command.
    /// </summary>
    /// <param name="script">The Ruby script or command to execute.</param>
    /// <remarks>
    /// <example>
    /// <code language="csharp">
    /// var output = new RubyCommand("puts 'Hello, world!'")
    ///  .Output();
    /// Console.WriteLine(output.Text());
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public RubyCommand(string script)
        : this()
    {
        this.Options.Script = script;
    }

    /// <summary>
    /// Registers the bash path hint with the default path finder.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   The environment variable <c>PYTHON_EXE</c> can be used to specify the path to the Ruby executable
    ///   and it takes precedence over the default paths.
    /// </para>
    /// <para>
    ///   The default paths for windows look under Program Files for Git Ruby. For Linux, it
    ///   checks common locations like `/usr/bin/python3` and `/usr/local/bin/python3`.
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// RubyCommand.RegisterPathHint();
    /// using var cmd = new RubyCommand("puts 'hello'");
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public static void RegisterPathHint()
    {
        PathFinder.Default.RegisterOrUpdate("ruby", (hint) =>
        {
            hint.Variable = "RUBY_EXE";
            hint.Linux = [
                "/usr/bin/ruby",
                "/usr/local/bin/ruby",
            ];

            hint.Windows = [
                "C:\\Ruby31-x64\\bin\\ruby.exe",
                "C:\\Ruby32-x64\\bin\\ruby.exe",
                "C:\\Ruby33-x64\\bin\\ruby.exe",
            ];
        });
    }
}