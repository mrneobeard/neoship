namespace NeoBeard.Exec.Tests;

public static class BunCommandTests
{
    [Fact]
    public static void RunScript()
    {
        var bun = PathFinder.Which("bun");
        Assert.SkipWhen(bun.IsNullOrWhiteSpace(), "Bun is not available on this system. Skipping test.");

        // Example of how to run a command and handle the output
        var output = new BunCommand()
           .RunScript("console.log('Hello, World!')");

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunScriptAsync()
    {
        var bun = PathFinder.Which("bun");
        Assert.SkipWhen(bun.IsNullOrWhiteSpace(), "Bun is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and handle the output
        var output = await new BunCommand()
           .RunScriptAsync("console.log('Hello, World!')", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void OutputScript()
    {
        var bun = PathFinder.Which("bun");
        Assert.SkipWhen(bun.IsNullOrWhiteSpace(), "Bun is not available on this system. Skipping test.");

        // Example of how to run a command and get the output
        var output = new BunCommand()
           .OutputScript("console.log('Hello, World!')");

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }

    [Fact]
    public static async ValueTask OutputScriptAsync()
    {
        var bun = PathFinder.Which("bun");
        Assert.SkipWhen(bun.IsNullOrWhiteSpace(), "Bun is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and get the output
        var output = await new BunCommand()
           .OutputScriptAsync("console.log('Hello, World!')", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
        Console.WriteLine(output.Stdout[output.Stdout.Length - 1]);
        Console.WriteLine("stdout length: {0}", output.Stdout.Length);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }
}