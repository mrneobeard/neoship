namespace NeoBeard.Exec.Tests;

public static class WinCommandTests
{
    [Fact]
    public static void RunScript()
    {
        var cmd = PathFinder.Which("cmd");
        Assert.SkipWhen(cmd.IsNullOrWhiteSpace(), "cmd is not available on this system. Skipping test.");

        var cmd1 = new WinCommand()
            .WithScript("echo Hello, World!");

        try
        {
            // Example of how to run a command and handle the output
            var output = cmd1.RunScript();
            Assert.Equal(0, output.ExitCode);
        }
        catch (Exception ex)
        {
            var si = cmd1.Options.ToStartInfo();
#if NET10_0_OR_GREATER
            var args = new CommandArgs(si.ArgumentList);
#else
            CommandArgs args = si.Arguments;
#endif
            args.Insert(0, si.FileName);
            var msg = $"Context: {args} \n {cmd1.Options.Script} \n\n{ex.Message}";
            throw new InvalidOperationException(msg, ex);
        }
    }

    [Fact]
    public static async ValueTask RunScriptAsync()
    {
        var cmd = PathFinder.Which("cmd");
        Assert.SkipWhen(cmd.IsNullOrWhiteSpace(), "cmd is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and handle the output
        var output = await new WinCommand()
           .RunScriptAsync("echo Hello, World!", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
    }

    [Fact]
    public static void OutputScript()
    {
        var cmd = PathFinder.Which("cmd");
        Assert.SkipWhen(cmd.IsNullOrWhiteSpace(), "cmd is not available on this system. Skipping test.");

        // Example of how to run a command and get the output
        var cmd1 = new WinCommand()
            .WithScript("echo Hello World!");
        try
        {
            var output = cmd1.OutputScript();
            Assert.Equal(0, output.ExitCode);
            Assert.NotEmpty(output.Stdout);

            var text = output.Text().Trim();
            Assert.NotEmpty(text);
            Assert.Equal("Hello World!", text);
        }
        catch (Exception ex)
        {
            var si = cmd1.Options.ToStartInfo();
#if NET10_0_OR_GREATER
            var args = new CommandArgs(si.ArgumentList);
#else
            CommandArgs args = si.Arguments;
#endif
            args.Insert(0, si.FileName);
            var msg = $"Context: {args} \n {cmd1.Options.Script} \n\n{ex.Message}";
            throw new InvalidOperationException(msg, ex);
        }
    }

    [Fact]
    public static async ValueTask OutputScriptAsync()
    {
        var cmd = PathFinder.Which("cmd");
        Assert.SkipWhen(cmd.IsNullOrWhiteSpace(), "cmd is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and get the output
        var output = await new WinCommand()
           .OutputScriptAsync("echo Hello World!", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello World!", text);
    }
}