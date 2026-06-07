namespace NeoBeard.Exec.Tests;

public static class ShCommandTests
{
    [Fact]
    public static void RunScript()
    {
        var sh = PathFinder.Which("sh");
        Assert.SkipWhen(sh.IsNullOrWhiteSpace(), "sh is not available on this system. Skipping test.");

        // Example of how to run a command and handle the output
        var output = new ShCommand()
           .RunScript("echo 'Hello, World!'");

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunScriptAsync()
    {
        var sh = PathFinder.Which("sh");
        Assert.SkipWhen(sh.IsNullOrWhiteSpace(), "sh is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and handle the output
        var output = await new ShCommand()
           .RunScriptAsync("echo 'Hello, World!'", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void OutputScript()
    {
        var sh = PathFinder.Which("sh");
        Assert.SkipWhen(sh.IsNullOrWhiteSpace(), "sh is not available on this system. Skipping test.");

        // Example of how to run a command and get the output
        var output = new ShCommand()
           .OutputScript("echo 'Hello, World!'");

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }

    [Fact]
    public static async ValueTask OutputScriptAsync()
    {
        var sh = PathFinder.Which("sh");
        Assert.SkipWhen(sh.IsNullOrWhiteSpace(), "sh is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and get the output
        var output = await new ShCommand()
           .OutputScriptAsync("echo 'Hello, World!'", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }
}