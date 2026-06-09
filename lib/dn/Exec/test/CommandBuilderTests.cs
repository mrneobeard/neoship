namespace NeoBeard.Exec.Tests;

public static class CommandBuilderTests
{
    [Fact]
    public static void WithExecutable_ShouldSetOptionsFile()
    {
        var cmd = new Command().WithExecutable("dotnet");

        Assert.Equal("dotnet", cmd.Options.File);
    }

    [Fact]
    public static void WithExecutable_ShouldReturnSameInstance()
    {
        var cmd = new Command();

        var result = cmd.WithExecutable("dotnet");

        Assert.Same(cmd, result);
    }

    [Fact]
    public static void WithArgs_ShouldSetOptionsArgs()
    {
        var cmd = new Command().WithArgs(["--version", "--info"]);

        Assert.Equal(2, cmd.Options.Args.Count);
        Assert.Equal("--version", cmd.Options.Args[0]);
        Assert.Equal("--info", cmd.Options.Args[1]);
    }

    [Fact]
    public static void WithCwd_ShouldSetWorkingDirectory()
    {
        var cmd = new Command().WithCwd("/home/user/project");

        Assert.Equal("/home/user/project", cmd.Options.Cwd);
    }

    [Fact]
    public static void SetEnv_SingleValue_ShouldSetEnvironmentVariable()
    {
        var cmd = new Command().SetEnv("MY_VAR", "my_value");

        Assert.NotNull(cmd.Options.Env);
        Assert.Equal("my_value", cmd.Options.Env["MY_VAR"]);
    }

    [Fact]
    public static void SetEnv_MultipleValues_ShouldSetAllVariables()
    {
        var cmd = new Command().SetEnv(new Dictionary<string, string?>
        {
            ["VAR1"] = "value1",
            ["VAR2"] = "value2",
        });

        Assert.Equal(2, cmd.Options.Env!.Count);
    }

    [Fact]
    public static void WithStdout_ShouldSetStdout()
    {
        var cmd = new Command().WithStdout(Stdio.Piped);

        Assert.Equal(Stdio.Piped, cmd.Options.Stdout);
    }

    [Fact]
    public static void WithStderr_ShouldSetStderr()
    {
        var cmd = new Command().WithStderr(Stdio.Piped);

        Assert.Equal(Stdio.Piped, cmd.Options.Stderr);
    }

    [Fact]
    public static void WithStdin_ShouldSetStdin()
    {
        var cmd = new Command().WithStdin(Stdio.Piped);

        Assert.Equal(Stdio.Piped, cmd.Options.Stdin);
    }

    [Fact]
    public static void AsPiped_ShouldSetAllStreamsToPiped()
    {
        var cmd = new Command().AsPiped();

        Assert.Equal(Stdio.Piped, cmd.Options.Stdout);
        Assert.Equal(Stdio.Piped, cmd.Options.Stderr);
        Assert.Equal(Stdio.Piped, cmd.Options.Stdin);
    }

    [Fact]
    public static void AsOutput_ShouldSetStdoutAndStderrToPiped_StdinToInherit()
    {
        var cmd = new Command().AsOutput();

        Assert.Equal(Stdio.Piped, cmd.Options.Stdout);
        Assert.Equal(Stdio.Piped, cmd.Options.Stderr);
        Assert.Equal(Stdio.Inherit, cmd.Options.Stdin);
    }

    [Fact]
    public static void WithStdio_ShouldSetAllStreams()
    {
        var cmd = new Command().WithStdio(Stdio.Null);

        Assert.Equal(Stdio.Null, cmd.Options.Stdout);
        Assert.Equal(Stdio.Null, cmd.Options.Stderr);
        Assert.Equal(Stdio.Null, cmd.Options.Stdin);
    }

    [Fact]
    public static void WithVerb_ShouldSetVerb()
    {
        var cmd = new Command().WithVerb("runas");

        Assert.Equal("runas", cmd.Options.Verb);
    }

    [Fact]
    public static void AsWindowsAdmin_ShouldSetVerbToRunas()
    {
        var cmd = new Command().AsWindowsAdmin();

        Assert.Equal("runas", cmd.Options.Verb);
    }

    [Fact]
    public static void AsSudo_ShouldSetVerbToSudo()
    {
        var cmd = new Command().AsSudo();

        Assert.Equal("sudo", cmd.Options.Verb);
    }

    [Fact]
    public static void AddDisposable_ShouldAddToDisposablesList()
    {
        var disposable = new TestDisposable();

        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .AddDisposable(disposable);

        cmd.Run();

        Assert.True(disposable.Disposed);
    }

    [Fact]
    public static void Pipe_WithCommandOptions_ShouldCreatePipe()
    {
        var cmd = new Command().WithExecutable("echo");
        var nextCmd = new Command().WithExecutable("cat");

        var pipe = cmd.Pipe(nextCmd.Options);

        Assert.Equal(2, pipe.Commands.Count);
    }

    [Fact]
    public static void Pipe_WithCommandArgs_ShouldCreatePipe()
    {
        var cmd = new Command().WithExecutable("echo");
        var args = new CommandArgs(["cat"]);

        var pipe = cmd.Pipe(args);

        Assert.Equal(2, pipe.Commands.Count);
    }

    [Fact]
    public static void Pipe_WithICommandOptionsOwner_ShouldCreatePipe()
    {
        var cmd1 = new Command().WithExecutable("echo");
        var cmd2 = new Command().WithExecutable("cat");

        var pipe = cmd1.Pipe(cmd2);

        Assert.Equal(2, pipe.Commands.Count);
    }

    [Fact]
    public static void Spawn_ShouldReturnChildProcess()
    {
        var cmd = new Command().WithExecutable("dotnet").WithArgs(["--version"]);

        using var process = cmd.Spawn();

        Assert.NotNull(process);
        Assert.True(process.Id > 0);
    }

    [Fact]
    public static void Spawn_WithArgs_ShouldUseArgsAndClearAfter()
    {
        var cmd = new Command().WithExecutable("dotnet");
        var args = new CommandArgs(["--version"]);

        using var process = cmd.Spawn(args);

        Assert.NotNull(process);
        Assert.Empty(cmd.Options.Args);
    }

    [Fact]
    public static void Run_ShouldReturnOutput()
    {
        var output = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .Run();

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void Run_WithArgs_ShouldUseArgs()
    {
        var cmd = new Command().WithExecutable("dotnet");
        var args = new CommandArgs(["--version"]);

        var output = cmd.Run(args);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunAsync_ShouldReturnOutput()
    {
        var output = await new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunAsync_WithArgs_ShouldUseArgs()
    {
        var cmd = new Command().WithExecutable("dotnet");
        var args = new CommandArgs(["--version"]);

        var output = await cmd.RunAsync(args, TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void Output_ShouldReturnOutputWithCapturedStdout()
    {
        var output = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .Output();

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static void Output_WithArgs_ShouldUseArgs()
    {
        var cmd = new Command().WithExecutable("dotnet");
        var args = new CommandArgs(["--version"]);

        var output = cmd.Output(args);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask OutputAsync_ShouldReturnOutputWithCapturedStdout()
    {
        var output = await new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .OutputAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask OutputAsync_WithArgs_ShouldUseArgs()
    {
        var cmd = new Command().WithExecutable("dotnet");
        var args = new CommandArgs(["--version"]);

        var output = await cmd.OutputAsync(args, TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    private sealed class TestDisposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            this.Disposed = true;
        }
    }
}