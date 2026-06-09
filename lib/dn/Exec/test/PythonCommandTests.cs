namespace NeoBeard.Exec.Tests;

public static class PythonCommandTests
{
    [Fact]
    public static void RunScript()
    {
        var python = PathFinder.Which("python");
        Assert.SkipWhen(python.IsNullOrWhiteSpace(), "Python is not available on this system. Skipping test.");

        // Example of how to run a command and handle the output
        var output = new PythonCommand()
           .RunScript("print('Hello, World!')");

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static async ValueTask RunScriptAsync()
    {
        var python = PathFinder.Which("python");
        Assert.SkipWhen(python.IsNullOrWhiteSpace(), "Python is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and handle the output
        var output = await new PythonCommand()
           .RunScriptAsync("print('Hello, World!')", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void OutputScript()
    {
        var python = PathFinder.Which("python");
        Assert.SkipWhen(python.IsNullOrWhiteSpace(), "Python is not available on this system. Skipping test.");

        // Example of how to run a command and get the output
        var output = new PythonCommand()
           .OutputScript("print ('Hello, World!')");

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }

    [Fact]
    public static async ValueTask OutputScriptAsync()
    {
        var python = PathFinder.Which("python");
        Assert.SkipWhen(python.IsNullOrWhiteSpace(), "Python is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and get the output
        var output = await new PythonCommand()
           .OutputScriptAsync("print('Hello, World!')", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
        Console.WriteLine(output.Stdout[output.Stdout.Length - 1]);
        Console.WriteLine("stdout length: {0}", output.Stdout.Length);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }
}