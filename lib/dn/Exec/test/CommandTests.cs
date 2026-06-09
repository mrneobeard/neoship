namespace NeoBeard.Exec.Tests;

public static class CommandTests
{
    [Fact]
    public static void Run()
    {
        // Example of how to run a command and handle the output
        var command = new Command(["dotnet", "--version"]);
        var output = command.Run();
        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void ReuseCommand()
    {
        var dotnet = new Command()
            .WithExecutable("dotnet");

        var o1 = dotnet.Run(["--version"]);
        Assert.Equal(0, o1.ExitCode);

        var o2 = dotnet.Run(["--list-sdks"]);
        Assert.Equal(0, o2.ExitCode);
    }

    [Fact]
    public static async ValueTask RunAsync()
    {
        // Example of how to run a command asynchronously and handle the output
        var command = new Command(["dotnet", "--version"]);
        var output = await command.RunAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void Output()
    {
        // Example of how to run a command and get the output
        var command = new Command(["dotnet", "--version"]);
        var output = command.Output();
        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text();
        Assert.NotEmpty(text);
        var parts = text.Trim().Split('.');
        Assert.True(parts.Length >= 2, "Expected at least two parts in the version output.");

        var num1 = parts[0].Trim();
        var num2 = parts[1].Trim();
        var major = int.Parse(num1);
        var minor = int.Parse(num2);

        Assert.NotEqual(0, major);
        Assert.NotEqual(-1, minor);
    }

    [Fact]
    public static async ValueTask OutputAsync()
    {
        // Example of how to run a command asynchronously and get the output
        var command = new Command(["dotnet", "--version"]);
        var output = await command.OutputAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text();
        Assert.NotEmpty(text);
        var parts = text.Trim().Split('.');
        Assert.True(parts.Length >= 2, "Expected at least two parts in the version output.");

        var num1 = parts[0].Trim();
        var num2 = parts[1].Trim();
        var major = int.Parse(num1);
        var minor = int.Parse(num2);

        Assert.NotEqual(0, major);
        Assert.NotEqual(-1, minor);
    }

    [Fact]
    public static void ChainWithPipe()
    {
        var hasEcho = PathFinder.Which("echo") != null;
        var hasGrep = PathFinder.Which("grep") != null;
        var hasCat = PathFinder.Which("cat") != null;

        Assert.SkipWhen(!hasEcho || !hasGrep || !hasCat, "Required commands are not available on this system. Skipping test.");

        var output = new Command(["echo", "my test"])
            .Pipe(["grep", "test"])
            .Pipe("cat")
            .Output();
        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
        Assert.Equal("my test", output.Text().Trim());
    }
}