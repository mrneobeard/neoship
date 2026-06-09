namespace NeoBeard.Exec.Tests;

public static class ShellCommandTests
{
    [Fact]
    public static void WithScript_ShouldSetScript()
    {
        var cmd = new TestShellCommand().WithScript("echo hello");

        Assert.Equal("echo hello", cmd.Options.Script);
    }

    [Fact]
    public static void WithScriptArgs_ShouldSetScriptArgs()
    {
        var args = new CommandArgs(["arg1", "arg2"]);
        var cmd = new TestShellCommand().WithScriptArgs(args);

        Assert.Equal(2, cmd.Options.ScriptArgs.Count);
    }

    [Fact]
    public static void AddScriptArgs_ShouldAddArgs()
    {
        var cmd = new TestShellCommand().AddScriptArgs("arg1", "arg2");

        Assert.Equal(2, cmd.Options.ScriptArgs.Count);
    }

    [Fact]
    public static void WithRunScriptAsFile_ShouldSetFlag()
    {
        var cmd = new TestShellCommand().WithRunScriptAsFile(true);

        Assert.True(cmd.Options.UseScriptAsFile);
    }

    [Fact]
    public static void WithDefaultExtension_ShouldSetExtension()
    {
        var cmd = new TestShellCommand().WithDefaultExtension(".sh");

        Assert.Equal(".sh", cmd.Options.DefaultExtension);
    }

    [Fact]
    public static void WithCancellationToken_ShouldSetToken()
    {
        using var cts = new CancellationTokenSource();
        var cmd = new TestShellCommand().WithCancellationToken(cts.Token);

        Assert.NotNull(cmd);
    }

    [Fact]
    public static void RunScript_WithScript_ShouldExecute()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo")
            .WithScript("hello");

        var output = cmd.RunScript();

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void RunScript_WithoutScript_ShouldReturnError()
    {
        var cmd = new TestShellCommand();

        var output = cmd.RunScript();

        Assert.Equal(-1, output.ExitCode);
        Assert.NotNull(output.Error);
    }

    [Fact]
    public static async ValueTask RunScriptAsync_WithScript_ShouldExecute()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo")
            .WithScript("hello");

        var output = await cmd.RunScriptAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunScriptAsync_WithoutScript_ShouldReturnError()
    {
        var cmd = new TestShellCommand();

        var output = await cmd.RunScriptAsync(TestContext.Current.CancellationToken);

        Assert.Equal(-1, output.ExitCode);
        Assert.NotNull(output.Error);
    }

    [Fact]
    public static void OutputScript_ShouldCaptureOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo")
            .WithScript("hello world");

        var output = cmd.OutputScript();

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask OutputScriptAsync_ShouldCaptureOutput()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo");

        var output = await cmd.OutputScriptAsync("hello world", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask GetAwaiter_WithScript_ShouldRunScript()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo")
            .WithScript("hello");

        var output = await cmd;

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask GetAwaiter_WithoutScript_ShouldRunCommand()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        Assert.SkipWhen(!hasEcho, "echo not found on PATH");

        var cmd = new TestShellCommand()
            .WithExecutable("echo")
            .WithArgs(["hello"]);

        var output = await cmd;

        Assert.Equal(0, output.ExitCode);
    }

    private sealed class TestShellCommand : ShellCommand<TestShellCommand, ShellCommandOptions>
    {
        public TestShellCommand()
        {
            this.WithExecutable("sh");
        }
    }
}