namespace NeoBeard.Exec;

/// <summary>
/// Represents a pipeline of commands where the output of one command is piped to the next.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var output = new Command(["echo", "hello world"])
///     .Pipe(["grep", "hello"])
///     .Output();
///
/// Assert.Equal(0, output.ExitCode);
/// Assert.Contains("hello", output.Text());
/// </code>
/// </example>
/// </remarks>
public class CommandPipe
{
    /// <summary>
    /// Gets or sets the list of commands in the pipeline.
    /// </summary>
    /// <value>A list of <see cref="CommandOptions"/> representing the commands.</value>
    public List<CommandOptions> Commands { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the pipeline has enough commands to run (at least 2).
    /// </summary>
    /// <value><c>true</c> if the pipeline can run; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var pipe = new CommandPipe();
    /// Assert.False(pipe.CanRun);
    ///
    /// pipe.Pipe(new CommandOptions { File = "echo", Args = ["test"] });
    /// Assert.False(pipe.CanRun);
    ///
    /// pipe.Pipe(new CommandOptions { File = "cat" });
    /// Assert.True(pipe.CanRun);
    /// </code>
    /// </example>
    /// </remarks>
    public bool CanRun => this.Commands.Count > 1;

    /// <summary>
    /// Adds a command to the pipeline.
    /// </summary>
    /// <param name="command">The command options to add.</param>
    /// <returns>This <see cref="CommandPipe"/> instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var pipe = new CommandPipe()
    ///     .Pipe(new CommandOptions { File = "echo", Args = ["hello"] })
    ///     .Pipe(new CommandOptions { File = "cat" });
    ///
    /// Assert.Equal(2, pipe.Commands.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(CommandOptions command)
    {
        this.Commands.Add(command);
        return this;
    }

    /// <summary>
    /// Adds a command from arguments to the pipeline.
    /// </summary>
    /// <param name="args">The command arguments, where the first element is the executable.</param>
    /// <returns>This <see cref="CommandPipe"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when args is null or empty.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var pipe = new CommandPipe()
    ///     .Pipe(["echo", "hello"])
    ///     .Pipe(["grep", "hello"]);
    ///
    /// Assert.Equal(2, pipe.Commands.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(CommandArgs args)
    {
        ArgumentOutOfRangeException.ThrowIfNullOrEmpty(args);
        var exe = args[0];
        args.RemoveAt(0);

        this.Commands.Add(new CommandOptions { Args = args, File = exe });
        return this;
    }

    /// <summary>
    /// Adds a command from an <see cref="ICommandOptionsOwner"/> to the pipeline.
    /// </summary>
    /// <param name="owner">The command options owner to add.</param>
    /// <returns>This <see cref="CommandPipe"/> instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var command = new Command(["echo", "hello"]);
    /// var pipe = new CommandPipe()
    ///     .Pipe(command)
    ///     .Pipe(["cat"]);
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(ICommandOptionsOwner owner)
    {
        this.Commands.Add(owner.Options);
        return this;
    }

    /// <summary>
    /// Runs the pipeline synchronously and returns the output from the last command.
    /// </summary>
    /// <returns>The <see cref="Output"/> from the last command in the pipeline.</returns>
    /// <exception cref="ProcessException">Thrown when the pipeline has fewer than 2 commands.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new CommandPipe()
    ///     .Pipe(["echo", "test"])
    ///     .Pipe(["cat"])
    ///     .Run();
    ///
    /// Assert.Equal(0, output.ExitCode);
    /// </code>
    /// </example>
    /// </remarks>
    public Output Run()
    {
        if (this.Commands.Count < 2)
        {
            throw new ProcessException("Pipe must have at least two commands");
        }

        var last = this.Commands.Count - 1;

        ChildProcess? lastProcess = null;
        for (var i = 0; i < this.Commands.Count; i++)
        {
            var command = this.Commands[i];
            if (i == 0)
            {
                command.Stdout = Stdio.Piped;
                command.Stderr = Stdio.Piped;
                lastProcess = new ChildProcess(command);
            }
            else if (i < last)
            {
                command.Stdout = Stdio.Piped;
                command.Stderr = Stdio.Piped;
                command.Stdin = Stdio.Piped;
                var currentProcess = new ChildProcess(command);
                lastProcess!.PipeTo(currentProcess);
                lastProcess.Wait();
                lastProcess.Dispose();
                lastProcess = currentProcess;
            }
            else
            {
                command.Stdin = Stdio.Piped;
                var currentProcess = new ChildProcess(command);
                lastProcess!.PipeTo(currentProcess);
                lastProcess.Wait();
                lastProcess.Dispose();
                lastProcess = currentProcess;
            }
        }

        return lastProcess!.WaitForOutput();
    }

    /// <summary>
    /// Runs the pipeline asynchronously and returns the output from the last command.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <exception cref="ProcessException">Thrown when the pipeline has fewer than 2 commands.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new CommandPipe()
    ///     .Pipe(["echo", "test"])
    ///     .Pipe(["cat"])
    ///     .RunAsync(CancellationToken.None);
    ///
    /// Assert.Equal(0, output.ExitCode);
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (this.Commands.Count < 2)
        {
            throw new ProcessException("Pipe must have at least two commands");
        }

        var last = this.Commands.Count - 1;

        ChildProcess? lastProcess = null;
        for (var i = 0; i < this.Commands.Count; i++)
        {
            var command = this.Commands[i];
            cancellationToken.ThrowIfCancellationRequested();
            if (i == 0)
            {
                command.Stdout = Stdio.Piped;
                command.Stderr = Stdio.Piped;
                lastProcess = new ChildProcess(command);
            }
            else if (i < last)
            {
                command.Stdout = Stdio.Piped;
                command.Stderr = Stdio.Piped;
                command.Stdin = Stdio.Piped;
                var currentProcess = new ChildProcess(command);
                await lastProcess!.PipeToAsync(currentProcess, cancellationToken);
                await lastProcess.WaitAsync(cancellationToken);
                lastProcess.Dispose();
                lastProcess = currentProcess;
            }
            else
            {
                command.Stdin = Stdio.Piped;
                var currentProcess = new ChildProcess(command);
                await lastProcess!.PipeToAsync(currentProcess, cancellationToken);
                await lastProcess.WaitAsync(cancellationToken);
                lastProcess.Dispose();
                lastProcess = currentProcess;
            }
        }

        var output = await lastProcess!.WaitForOutputAsync(cancellationToken);
        lastProcess.Dispose();
        return output;
    }

    /// <summary>
    /// Runs the pipeline and captures output from the last command.
    /// </summary>
    /// <returns>The <see cref="Output"/> from the last command in the pipeline.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new CommandPipe()
    ///     .Pipe(["echo", "hello"])
    ///     .Pipe(["cat"])
    ///     .Output();
    ///
    /// Assert.Equal("hello", output.Text().Trim());
    /// </code>
    /// </example>
    /// </remarks>
    public Output Output()
    {
        if (this.CanRun)
        {
            var last = this.Commands[^1];
            last.Stdout = Stdio.Piped;
            last.Stderr = Stdio.Piped;
        }

        return this.Run();
    }

    /// <summary>
    /// Runs the pipeline asynchronously and captures output from the last command.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new CommandPipe()
    ///     .Pipe(["echo", "hello"])
    ///     .Pipe(["cat"])
    ///     .OutputAsync(CancellationToken.None);
    ///
    /// Assert.Equal("hello", output.Text().Trim());
    /// </code>
    /// </example>
    /// </remarks>
    public ValueTask<Output> OutputAsync(CancellationToken cancellationToken = default)
    {
        if (this.CanRun)
        {
            var last = this.Commands[^1];
            last.Stdout = Stdio.Piped;
            last.Stderr = Stdio.Piped;
        }

        return this.RunAsync(cancellationToken);
    }
}