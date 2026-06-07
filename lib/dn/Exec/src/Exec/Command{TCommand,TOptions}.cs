using System.Runtime.Versioning;

namespace NeoBeard.Exec;

/// <summary>
/// A generic command base class that provides fluent configuration methods for process execution.
/// </summary>
/// <typeparam name="TCommand">The derived command type.</typeparam>
/// <typeparam name="TOptions">The options type for the command.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// public class MyCommand : Command&lt;MyCommand, CommandOptions&gt;
/// {
/// }
///
/// var cmd = new MyCommand()
///     .WithExecutable("dotnet")
///     .WithArgs(["--version"]);
/// var output = cmd.Output();
/// </code>
/// </example>
/// </remarks>
public class Command<TCommand, TOptions> : ICommandOptionsOwner
    where TCommand : Command<TCommand, TOptions>, new()
    where TOptions : CommandOptions, new()
{
    /// <summary>
    /// Gets or sets the command options.
    /// </summary>
    /// <value>The options for this command.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command();
    /// cmd.Options.File = "dotnet";
    /// cmd.Options.Args = ["--version"];
    /// </code>
    /// </example>
    /// </remarks>
    public TOptions Options { get; set; } = new();

    CommandOptions ICommandOptionsOwner.Options => this.Options;

    /// <summary>
    /// Sets the executable file name or path.
    /// </summary>
    /// <param name="executable">The executable file name or path.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet");
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithExecutable(string executable)
    {
        this.Options.File = executable;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the arguments for the command.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet")
    ///     .WithArgs(["build", "--configuration", "Release"]);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithArgs(CommandArgs args)
    {
        this.Options.Args = args;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the working directory for the command.
    /// </summary>
    /// <param name="cwd">The working directory path.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command("dotnet")
    ///     .WithCwd("/home/user/project");
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithCwd(string cwd)
    {
        this.Options.Cwd = cwd;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the environment variables for the command.
    /// </summary>
    /// <param name="env">A dictionary of environment variables.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command("node")
    ///     .WithEnv(new Dictionary&lt;string, string?&gt; { ["NODE_ENV"] = "production" });
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithEnv(IDictionary<string, string?> env)
    {
        this.Options.Env = env;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets a single environment variable for the command.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command("node")
    ///     .SetEnv("NODE_ENV", "production");
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand SetEnv(string name, string value)
    {
        this.Options.Env ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        this.Options.Env[name] = value;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets multiple environment variables for the command.
    /// </summary>
    /// <param name="values">A collection of key-value pairs for environment variables.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command("node")
    ///     .SetEnv(new Dictionary&lt;string, string?&gt; { ["NODE_ENV"] = "production", ["DEBUG"] = "false" });
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand SetEnv(IEnumerable<KeyValuePair<string, string?>> values)
    {
        this.Options.Env ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in values)
        {
            this.Options.Env[kvp.Key] = kvp.Value;
        }

        return (TCommand)this;
    }

    /// <summary>
    /// Adds disposable objects to be disposed when the process is disposed.
    /// </summary>
    /// <param name="disposables">The disposable objects to add.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tempFile = new DisposableFile("/tmp/temp.txt");
    /// var cmd = new Command(["cat"])
    ///     .WithDisposables([tempFile]);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithDisposables(IEnumerable<IDisposable> disposables)
    {
        this.Options.Disposables.AddRange(disposables);
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the disposable objects list for the command.
    /// </summary>
    /// <param name="disposables">The disposable objects list.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var disposables = new List&lt;IDisposable&gt; { new DisposableFile("/tmp/a.txt") };
    /// var cmd = new Command()
    ///     .WithExecutable("cat")
    ///     .SetDisposables(disposables);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand SetDisposables(IEnumerable<IDisposable> disposables)
    {
        this.Options.Disposables = new List<IDisposable>(disposables);
        return (TCommand)this;
    }

    /// <summary>
    /// Sets how standard output should be handled.
    /// </summary>
    /// <param name="stdio">The stdio configuration for standard output.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet")
    ///     .WithStdout(Stdio.Piped);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithStdout(Stdio stdio)
    {
        this.Options.Stdout = stdio;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets how standard error should be handled.
    /// </summary>
    /// <param name="stdio">The stdio configuration for standard error.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet")
    ///     .WithStderr(Stdio.Piped);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithStderr(Stdio stdio)
    {
        this.Options.Stderr = stdio;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets how standard input should be handled.
    /// </summary>
    /// <param name="stdio">The stdio configuration for standard input.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cat")
    ///     .WithStdin(Stdio.Piped);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithStdin(Stdio stdio)
    {
        this.Options.Stdin = stdio;
        return (TCommand)this;
    }

    /// <summary>
    /// Configures the command with all streams piped.
    /// </summary>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cat")
    ///     .AsPiped();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AsPiped()
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        this.Options.Stdin = Stdio.Piped;
        return (TCommand)this;
    }

    /// <summary>
    /// Configures the command to capture stdout and stderr while inheriting stdin.
    /// </summary>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet")
    ///     .AsOutput();
    /// var output = cmd.Run();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AsOutput()
    {
        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        this.Options.Stdin = Stdio.Inherit;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets all standard streams to the same configuration.
    /// </summary>
    /// <param name="stdio">The stdio configuration for all streams.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("dotnet")
    ///     .WithStdio(Stdio.Inherit);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithStdio(Stdio stdio)
    {
        this.Options.Stdout = stdio;
        this.Options.Stderr = stdio;
        this.Options.Stdin = stdio;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the verb for process start.
    /// </summary>
    /// <param name="verb">The verb to use (e.g., "runas" for elevated privileges).</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cmd")
    ///     .WithVerb("runas");
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand WithVerb(string verb)
    {
        this.Options.Verb = verb;
        return (TCommand)this;
    }

    /// <summary>
    /// Configures the command to run as Windows administrator (elevated).
    /// </summary>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cmd")
    ///     .AsWindowsAdmin();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AsWindowsAdmin()
    {
        this.Options.Verb = "runas";
        return (TCommand)this;
    }

    /// <summary>
    /// Configures the command to run with sudo on Unix systems.
    /// </summary>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("apt-get")
    ///     .AsSudo();
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AsSudo()
    {
        this.Options.Verb = "sudo";
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the user name for the process.
    /// </summary>
    /// <param name="user">The user name to run the process as.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cmd")
    ///     .WithUser("adminuser");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public TCommand WithUser(string user)
    {
        this.Options.User = user;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the password for the user account.
    /// </summary>
    /// <param name="password">The password in clear text.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cmd")
    ///     .WithUser("adminuser")
    ///     .WithPassword("mypassword");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public TCommand WithPassword(string password)
    {
        this.Options.PasswordInClearText = password;
        return (TCommand)this;
    }

    /// <summary>
    /// Sets the domain for the user account.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command()
    ///     .WithExecutable("cmd")
    ///     .WithUser("adminuser")
    ///     .WithDomain("MYDOMAIN");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public TCommand WithDomain(string domain)
    {
        this.Options.Domain = domain;
        return (TCommand)this;
    }

    /// <summary>
    /// Gets or sets the input stream. This is only used when running the command. Spawning a command will
    /// not use this stream.
    /// </summary>
    /// <value>The input stream to pipe to the process, or <c>null</c> if no input.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
    /// var cmd = new Command(["cat"])
    ///     .WithStdin(Stdio.Piped)
    ///     .WithStdout(Stdio.Piped);
    /// cmd.Input = inputStream;
    /// var output = cmd.Run();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Stream? Input { get; set; }

    /// <summary>
    /// Gets the list of disposable objects associated with this command.
    /// </summary>
    /// <value>A list of <see cref="IDisposable"/> objects.</value>
    protected List<IDisposable> Disposables { get; } = [];

    /// <summary>
    /// Adds a disposable object to be disposed when the process completes.
    /// </summary>
    /// <param name="disposable">The disposable object to add.</param>
    /// <returns>This command instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tempFile = new DisposableFile("/tmp/temp.txt");
    /// var cmd = new Command()
    ///     .WithExecutable("cat")
    ///     .AddDisposable(tempFile);
    /// </code>
    /// </example>
    /// </remarks>
    public TCommand AddDisposable(IDisposable disposable)
    {
        this.Disposables.Add(disposable);
        return (TCommand)this;
    }

    /// <summary>
    /// Creates a pipe to another command.
    /// </summary>
    /// <param name="command">The command options to pipe to.</param>
    /// <returns>A <see cref="CommandPipe"/> for chaining additional pipes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["echo", "hello"])
    ///     .Pipe(new CommandOptions { File = "grep", Args = ["hello"] })
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(CommandOptions command)
    {
        var pipe = new CommandPipe();
        pipe.Pipe(this);
        pipe.Pipe(command);
        return pipe;
    }

    /// <summary>
    /// Creates a pipe to another command specified by arguments.
    /// </summary>
    /// <param name="args">The command arguments, where the first element is the executable.</param>
    /// <returns>A <see cref="CommandPipe"/> for chaining additional pipes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["echo", "hello"])
    ///     .Pipe(["grep", "hello"])
    ///     .Output();
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(CommandArgs args)
    {
        var pipe = new CommandPipe();
        pipe.Pipe(this);
        pipe.Pipe(args);
        return pipe;
    }

    /// <summary>
    /// Creates a pipe to another command options owner.
    /// </summary>
    /// <param name="command">The command options owner to pipe to.</param>
    /// <returns>A <see cref="CommandPipe"/> for chaining additional pipes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd1 = new Command(["echo", "hello"]);
    /// var cmd2 = new Command(["grep", "hello"]);
    /// var output = cmd1.Pipe(cmd2).Output();
    /// </code>
    /// </example>
    /// </remarks>
    public CommandPipe Pipe(ICommandOptionsOwner command)
    {
        var pipe = new CommandPipe();
        pipe.Pipe(this);
        pipe.Pipe(command);
        return pipe;
    }

    /// <summary>
    /// Runs the command with specified arguments and captures output.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <returns>The <see cref="NeoBeard.Exec.Output"/> from the command execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet"]);
    /// var output = cmd.Output(["--version"]);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output Output(CommandArgs args)
    {
        var stdout = this.Options.Stdout;
        var stderr = this.Options.Stderr;

        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;

        var o = this.Run(args);

        this.Options.Stdout = stdout;
        this.Options.Stderr = stderr;
        return o;
    }

    /// <summary>
    /// Runs the command and captures output.
    /// </summary>
    /// <returns>The <see cref="NeoBeard.Exec.Output"/> from the command execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--version"]).Output();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output Output()
    {
        var stdout = this.Options.Stdout;
        var stderr = this.Options.Stderr;

        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;

        var o = this.Run();

        this.Options.Stdout = stdout;
        this.Options.Stderr = stderr;

        return o;
    }

    /// <summary>
    /// Asynchronously runs the command with specified arguments and captures output.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet"]);
    /// var output = await cmd.OutputAsync(["--version"], CancellationToken.None);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> OutputAsync(CommandArgs args, CancellationToken cancellationToken = default)
    {
        var oldStdout = this.Options.Stdout;
        var oldStderr = this.Options.Stderr;

        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        var output = await this.RunAsync(args, cancellationToken);

        this.Options.Stdout = oldStdout;
        this.Options.Stderr = oldStderr;

        return output;
    }

    /// <summary>
    /// Asynchronously runs the command and captures output.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new Command(["dotnet", "--version"]).OutputAsync(CancellationToken.None);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> OutputAsync(CancellationToken cancellationToken = default)
    {
        var oldStdout = this.Options.Stdout;
        var oldStderr = this.Options.Stderr;

        this.Options.Stdout = Stdio.Piped;
        this.Options.Stderr = Stdio.Piped;
        var output = await this.RunAsync(cancellationToken);

        this.Options.Stdout = oldStdout;
        this.Options.Stderr = oldStderr;

        return output;
    }

    /// <summary>
    /// Runs the command with the specified arguments.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <returns>The <see cref="NeoBeard.Exec.Output"/> from the command execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet"]);
    /// var output = cmd.Run(["--version"]);
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public Output Run(CommandArgs args)
    {
        var old = this.Options.Args;
        this.Options.Args = args;
        var output = this.Run();
        this.Options.Args = old;
        return output;
    }

    /// <summary>
    /// Runs the command synchronously.
    /// </summary>
    /// <returns>The <see cref="NeoBeard.Exec.Output"/> from the command execution.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--version"]).Run();
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public Output Run()
    {
        var hasInput = this.Input is not null;
        if (hasInput)
        {
            this.Options.Stdin = Stdio.Piped;
        }

        using var process = new ChildProcess(this.Options);
        if (hasInput)
        {
            process.PipeFrom(this.Input!);
        }

        process.AddDisposables(this.Disposables);
        return process.WaitForOutput();
    }

    /// <summary>
    /// Asynchronously runs the command with the specified arguments.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet"]);
    /// var output = await cmd.RunAsync(["--version"], CancellationToken.None);
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> RunAsync(CommandArgs args, CancellationToken cancellationToken = default)
    {
        var old = this.Options.Args;
        this.Options.Args = args;

        var output = await this.RunAsync(cancellationToken);

        this.Options.Args = old;

        return output;
    }

    /// <summary>
    /// Asynchronously runs the command.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> representing the async operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = await new Command(["dotnet", "--version"]).RunAsync(CancellationToken.None);
    /// Console.WriteLine($"Exit code: {output.ExitCode}");
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> RunAsync(CancellationToken cancellationToken = default)
    {
        var hasInput = this.Input is not null;
        if (hasInput)
        {
            this.Options.Stdin = Stdio.Piped;
        }

        using var process = new ChildProcess(this.Options);
        if (hasInput)
        {
            process.PipeFrom(this.Input!);
        }

        process.AddDisposables(this.Disposables);
        var output = await process.WaitForOutputAsync(cancellationToken);
        return output;
    }

    /// <summary>
    /// Spawns the command as a child process without waiting for completion.
    /// </summary>
    /// <returns>A <see cref="ChildProcess"/> instance representing the running process.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new Command(["dotnet", "watch"]).Spawn();
    ///
    /// process.Wait();
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess Spawn()
    {
        var process = new ChildProcess(this.Options);
        process.AddDisposables(this.Disposables);
        return process;
    }

    /// <summary>
    /// Spawns the command with specified arguments as a child process without waiting for completion.
    /// </summary>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <returns>A <see cref="ChildProcess"/> instance representing the running process.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var cmd = new Command(["dotnet"]);
    /// using var process = cmd.Spawn(["watch"]);
    ///
    /// process.Wait();
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess Spawn(CommandArgs args)
    {
        this.Options.Args = args;
        var process = this.Spawn();
        this.Options.Args.Clear();
        return process;
    }
}