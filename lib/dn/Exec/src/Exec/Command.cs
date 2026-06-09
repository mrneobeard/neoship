using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace NeoBeard.Exec;

/// <summary>
/// A concrete command class for executing processes.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Simple command execution
/// var output = new Command(["dotnet", "--version"]).Output();
/// Console.WriteLine(output.Text());
///
/// // Fluent configuration
/// var output = new Command()
///     .WithExecutable("dotnet")
///     .WithArgs(["build", "--configuration", "Release"])
///     .WithCwd("/home/user/project")
///     .Output();
/// </code>
/// </example>
/// </remarks>
public partial class Command : Command<Command, CommandOptions>, ICommandOptionsOwner
{
    private CancellationToken? cancellationToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command();
    /// cmd.Options.File = "dotnet";
    /// cmd.Options.Args = ["--version"];
    /// </code>
    /// </example>
    /// </remarks>
    public Command()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class from command arguments.
    /// </summary>
    /// <param name="args">The command arguments, where the first element is the executable.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when args is null or empty.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet", "--version"]);
    /// var output = cmd.Output();
    /// </code>
    /// </example>
    /// </remarks>
    public Command(CommandArgs args)
        : this()
    {
        ArgumentOutOfRangeException.ThrowIfNullOrEmpty(args);
        var exe = args[0];
        args.RemoveAt(0);
        this.Options.File = exe;
        this.Options.Args = args;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Command"/> class with the specified options.
    /// </summary>
    /// <param name="options">The command options to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions
    /// {
    ///     File = "dotnet",
    ///     Args = ["--version"],
    ///     Stdout = Stdio.Piped
    /// };
    /// var cmd = new Command(options);
    /// </code>
    /// </example>
    /// </remarks>
    public Command(CommandOptions options)
    {
        this.Options = options;
    }

    /// <summary>
    /// Gets an awaiter used to await the command execution.
    /// </summary>
    /// <returns>An awaiter for the command output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new Command(["dotnet", "--version"]);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTaskAwaiter<Output> GetAwaiter()
    {
        var token = this.cancellationToken ?? default;
        return this.RunAsync(token).GetAwaiter();
    }

    /// <summary>
    /// Sets the cancellation token for command execution.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var cts = new CancellationTokenSource();
    /// var output = await new Command(["dotnet", "build"])
    ///     .WithCancellationToken(cts.Token);
    /// </code>
    /// </example>
    /// </remarks>
    public Command WithCancellationToken(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        return this;
    }
}