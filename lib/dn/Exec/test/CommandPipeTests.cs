namespace NeoBeard.Exec.Tests;

public static class CommandPipeTests
{
    [Fact]
    public static void DefaultConstructor_ShouldInitializeEmptyCommandsList()
    {
        var pipe = new CommandPipe();

        Assert.Empty(pipe.Commands);
    }

    [Fact]
    public static void CanRun_WithLessThanTwoCommands_ShouldReturnFalse()
    {
        var pipe = new CommandPipe();

        Assert.False(pipe.CanRun);

        pipe.Pipe(new Command().WithExecutable("echo").Options);
        Assert.False(pipe.CanRun);
    }

    [Fact]
    public static void CanRun_WithTwoOrMoreCommands_ShouldReturnTrue()
    {
        var pipe = new CommandPipe()
            .Pipe(new Command().WithExecutable("echo").Options)
            .Pipe(new Command().WithExecutable("cat").Options);

        Assert.True(pipe.CanRun);
    }

    [Fact]
    public static void Pipe_WithCommandOptions_ShouldAddCommand()
    {
        var pipe = new CommandPipe();
        var options = new Command().WithExecutable("echo").Options;

        pipe.Pipe(options);

        Assert.Single(pipe.Commands);
        Assert.Same(options, pipe.Commands[0]);
    }

    [Fact]
    public static void Pipe_WithCommandOptions_ShouldReturnSamePipeInstance()
    {
        var pipe = new CommandPipe();

        var result = pipe.Pipe(new Command().WithExecutable("echo").Options);

        Assert.Same(pipe, result);
    }

    [Fact]
    public static void Pipe_WithCommandArgs_ShouldAddCommand()
    {
        var pipe = new CommandPipe();
        var args = new CommandArgs(["echo", "hello"]);

        pipe.Pipe(args);

        Assert.Single(pipe.Commands);
        Assert.Equal("echo", pipe.Commands[0].File);
        Assert.Single(pipe.Commands[0].Args);
        Assert.Equal("hello", pipe.Commands[0].Args[0]);
    }

    [Fact]
    public static void Pipe_WithNullCommandArgs_ShouldThrow()
    {
        var pipe = new CommandPipe();

        Assert.Throws<NullReferenceException>(() => pipe.Pipe((CommandArgs)null!));
    }

    [Fact]
    public static void Pipe_WithEmptyCommandArgs_ShouldThrow()
    {
        var pipe = new CommandPipe();
        var args = new CommandArgs();

        Assert.ThrowsAny<ArgumentException>(() => pipe.Pipe(args));
    }

    [Fact]
    public static void Pipe_WithICommandOptionsOwner_ShouldAddOptions()
    {
        var pipe = new CommandPipe();
        var command = new Command().WithExecutable("echo");

        pipe.Pipe(command);

        Assert.Single(pipe.Commands);
        Assert.Same(command.Options, pipe.Commands[0]);
    }

    [Fact]
    public static void Run_WithLessThanTwoCommands_ShouldThrowProcessException()
    {
        var pipe = new CommandPipe()
            .Pipe(new Command().WithExecutable("echo").Options);

        Assert.Throws<ProcessException>(() => pipe.Run());
    }

    [Fact]
    public static async ValueTask RunAsync_WithLessThanTwoCommands_ShouldThrowProcessException()
    {
        var pipe = new CommandPipe()
            .Pipe(new Command().WithExecutable("echo").Options);

        await Assert.ThrowsAsync<ProcessException>(async () => await pipe.RunAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public static void Run_WithValidPipe_ShouldReturnOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        var hasCat = PathFinder.Which("cat") != null;
        Assert.SkipWhen(!hasEcho || !hasCat, "Required commands not available");

        var args = new CommandArgs(["echo", "hello world"]);
        var pipe = new CommandPipe()
            .Pipe(args);

        var args2 = new CommandArgs(["cat"]);
        pipe.Pipe(args2);

        var output = pipe.Run();

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunAsync_WithValidPipe_ShouldReturnOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        var hasCat = PathFinder.Which("cat") != null;
        Assert.SkipWhen(!hasEcho || !hasCat, "Required commands not available");

        var args = new CommandArgs(["echo", "hello world"]);
        var pipe = new CommandPipe()
            .Pipe(args);

        var args2 = new CommandArgs(["cat"]);
        pipe.Pipe(args2);

        var output = await pipe.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void Output_ShouldRunPipeAndCaptureOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        var hasCat = PathFinder.Which("cat") != null;
        Assert.SkipWhen(!hasEcho || !hasCat, "Required commands not available");

        var args = new CommandArgs(["echo", "test output"]);
        var args2 = new CommandArgs(["cat"]);

        var output = new CommandPipe()
            .Pipe(args)
            .Pipe(args2)
            .Output();

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask OutputAsync_ShouldRunPipeAndCaptureOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        var hasCat = PathFinder.Which("cat") != null;
        Assert.SkipWhen(!hasEcho || !hasCat, "Required commands not available");

        var args = new CommandArgs(["echo", "test output"]);
        var args2 = new CommandArgs(["cat"]);

        var output = await new CommandPipe()
            .Pipe(args)
            .Pipe(args2)
            .OutputAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }
}