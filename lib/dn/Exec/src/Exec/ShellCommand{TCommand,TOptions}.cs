using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace NeoBeard.Exec;

/// <summary>
/// A base class for shell commands that can execute scripts.
/// </summary>
/// <typeparam name="TCommand">The derived command type.</typeparam>
/// <typeparam name="TOptions">The shell command options type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// public class MyShellCommand : ShellCommand&lt;MyShellCommand, ShellCommandOptions&gt;
/// {
///     public MyShellCommand()
///     {
///         this.Options.File = "bash";
///     }
/// }
///
/// var output = await new MyShellCommand()
///     .WithScript("echo hello world")
///     .RunAsync(CancellationToken.None);
/// Console.WriteLine(output.Text());
/// </code>
/// </example>
/// </remarks>
public class ShellCommand<TCommand, TOptions> : Command<TCommand, TOptions>
    where TCommand : ShellCommand<TCommand, TOptions>, new()
    where TOptions : ShellCommandOptions, new()
{
    private CancellationToken? cancellationToken;

    /// <summary>
    /// Gets an awaiter used to await the command execution.
    /// </summary>
    /// <returns>An awaiter for the command output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new BashCommand()
    ///     .WithScript("echo hello");
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTaskAwaiter<Output> GetAwaiter()
    {
        var token = this.cancellationToken ?? default;
        if (this.Options.Script.IsNullOrWhiteSpace())
            return this.RunAsync(token).GetAwaiter();

        return this.RunScriptAsync(token).GetAwaiter();
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
    /// var output = await new BashCommand()
    ///     .WithScript("sleep 10")
    ///     .WithCancellationToken(cts.Token);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithCancellationToken(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the script to execute.
    /// </summary>
    /// <param name="script">The script content to execute.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .WithScript("echo hello; echo world")
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithScript(string script)
    {
        this.Options.Script = script;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the script arguments.
    /// </summary>
    /// <param name="args">The arguments to pass to the script.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .WithScript("echo $1")
    ///     .WithScriptArgs(["hello"])
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithScriptArgs(CommandArgs args)
    {
        this.Options.ScriptArgs = args ?? [];
        return (TCommand)this;
    }

    /// <summary>
    /// Adds script arguments to the existing arguments list.
    /// </summary>
    /// <param name="args">The arguments to add.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .WithScript("echo $1 $2")
    ///     .AddScriptArgs("hello", "world")
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AddScriptArgs(params string[] args)
    {
        this.Options.ScriptArgs.AddRange(args ?? []);
        return (TCommand)this;
    }

    /// <summary>
    /// Sets whether to run the script as a temporary file instead of inline.
    /// </summary>
    /// <param name="useScriptAsFile"><c>true</c> to run as a file; otherwise, <c>false</c>.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .WithScript("echo hello")
    ///     .WithRunScriptAsFile(true)
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithRunScriptAsFile(bool useScriptAsFile = true)
    {
        this.Options.UseScriptAsFile = useScriptAsFile;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the default file extension for script files.
    /// </summary>
    /// <param name="defaultExtension">The default file extension (e.g., ".sh").</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .WithScript("echo hello")
    ///     .WithRunScriptAsFile(true)
    ///     .WithDefaultExtension(".sh")
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithDefaultExtension(string? defaultExtension)
    {
        this.Options.DefaultExtension = defaultExtension;
        return (TCommand)this;
    }

    /// <summary>
    /// Runs the script and captures output.
    /// </summary>
    /// <returns>The <see cref="Output"/> from the script execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new BashCommand();
    /// cmd.Options.Script = "echo hello";
    /// var output = cmd.OutputScript();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output OutputScript()
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        return this.RunScript();
    }

    /// <summary>
    /// Runs the specified script and captures output.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>The <see cref="Output"/> from the script execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .OutputScript("echo hello world");
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output OutputScript(string script)
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        return this.RunScript(script);
    }

    /// <summary>
    /// Asynchronously runs the specified script and captures output.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new BashCommand()
    ///     .OutputScriptAsync("echo hello world", CancellationToken.None);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTask<Output> OutputScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        return this.RunScriptAsync(script, cancellationToken);
    }

    /// <summary>
    /// Asynchronously runs the configured script and captures output.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new BashCommand();
    /// cmd.Options.Script = "echo hello";
    /// var output = await cmd.OutputScriptAsync(CancellationToken.None);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTask<Output> OutputScriptAsync(CancellationToken cancellationToken = default)
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        return this.RunScriptAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the specified script.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <returns>The <see cref="Output"/> from the script execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new BashCommand()
    ///     .RunScript("echo hello world");
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public Output RunScript(string script)
    {
        this.Options.Script = script;
        return this.Run();
    }

    /// <summary>
    /// Runs the configured script.
    /// </summary>
    /// <returns>The <see cref="Output"/> from the script execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new BashCommand();
    /// cmd.Options.Script = "echo hello";
    /// var output = cmd.RunScript();
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public Output RunScript()
    {
        if (this.Options.Script.IsNullOrWhiteSpace())
        {
            return new Output(
                this.Options.File,
                -1,
                new InvalidOperationException(" No script provided to run. The ShellCommandOptions.Script property must be set for RunScript method."),
                [],
                [],
                DateTime.UtcNow,
                DateTime.UtcNow);
        }

        return this.Run();
    }

    /// <summary>
    /// Asynchronously runs the specified script.
    /// </summary>
    /// <param name="script">The script to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new BashCommand()
    ///     .RunScriptAsync("echo hello world", CancellationToken.None);
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTask<Output> RunScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        this.Options.Script = script;
        return this.RunAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously runs the configured script.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new BashCommand();
    /// cmd.Options.Script = "echo hello";
    /// var output = await cmd.RunScriptAsync(CancellationToken.None);
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTask<Output> RunScriptAsync(CancellationToken cancellationToken = default)
    {
        if (this.Options.Script.IsNullOrWhiteSpace())
        {
            return new ValueTask<Output>(new Output(
                this.Options.File,
                -1,
                new InvalidOperationException(" No script provided to run. The ShellCommandOptions.Script property must be set for RunScriptAsync method."),
                [],
                [],
                DateTime.UtcNow,
                DateTime.UtcNow));
        }

        return this.RunAsync(cancellationToken);
    }
}