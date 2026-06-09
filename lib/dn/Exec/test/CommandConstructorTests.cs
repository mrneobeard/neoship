namespace NeoBeard.Exec.Tests;

public static class CommandConstructorTests
{
    [Fact]
    public static void DefaultConstructor_ShouldInitializeOptions()
    {
        var cmd = new Command();

        Assert.NotNull(cmd.Options);
        Assert.Equal(string.Empty, cmd.Options.File);
        Assert.Empty(cmd.Options.Args);
    }

    [Fact]
    public static void Constructor_WithCommandArgs_ShouldSetFileAndArgs()
    {
        var args = new CommandArgs(["dotnet", "--version", "--info"]);

        var cmd = new Command(args);

        Assert.Equal("dotnet", cmd.Options.File);
        Assert.Equal(2, cmd.Options.Args.Count);
        Assert.Equal("--version", cmd.Options.Args[0]);
        Assert.Equal("--info", cmd.Options.Args[1]);
    }

    [Fact]
    public static void Constructor_WithNullCommandArgs_ShouldThrow()
    {
        Assert.Throws<NullReferenceException>(() => new Command((CommandArgs)null!));
    }

    [Fact]
    public static void Constructor_WithEmptyCommandArgs_ShouldThrow()
    {
        Assert.ThrowsAny<ArgumentException>(() => new Command(new CommandArgs()));
    }

    [Fact]
    public static void Constructor_WithCommandOptions_ShouldUseOptions()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .WithStdout(Stdio.Piped);

        var newCmd = new Command(cmd.Options);

        Assert.Same(cmd.Options, newCmd.Options);
    }

    [Fact]
    public static async ValueTask WithCancellationToken_ShouldSetCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var output = await new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .WithCancellationToken(cts.Token)
            .RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask GetAwaiter_ShouldRunCommand()
    {
        var args = new CommandArgs(["dotnet", "--version"]);
        var output = await new Command(args)
            .WithStdout(Stdio.Piped);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask GetAwaiter_WithCancellationToken_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        var args = new CommandArgs(["dotnet", "--version"]);

        var output = await new Command(args)
            .WithStdout(Stdio.Piped)
            .WithCancellationToken(cts.Token);

        Assert.Equal(0, output.ExitCode);
    }
}