namespace NeoBeard.Exec.Tests;

public static class BashCommandTests
{
    [Fact]
    public static void RunScript()
    {
        BashCommand.PrependWindowsGitPath();
        try
        {
            var bash = PathFinder.Which("bash");
            Console.WriteLine(bash);
            Console.WriteLine(string.Empty);
            Assert.SkipWhen(bash.IsNullOrWhiteSpace(), "Bash is not available on this system. Skipping test.");

            // Example of how to run a command and handle the output
            var output = new BashCommand()
            .RunScript("echo \"Hello, World!\"");

            Assert.Equal(0, output.ExitCode);
        }
        catch (Exception ex)
        {
            var cmd = new BashCommand()
                .WithScript("echo 'Hello, World!'");

            var si = cmd.Options.ToStartInfo();
#if NET10_0_OR_GREATER
            var args = new CommandArgs(si.ArgumentList);
#else
            CommandArgs args = si.Arguments;
#endif

            args.Insert(0, si.FileName);
            var msg = $"Context: {args} \n\n{ex.Message}";
            throw new InvalidOperationException(msg, ex);
        }
    }

    [Fact]
    public static async ValueTask RunScriptAsync()
    {
        BashCommand.PrependWindowsGitPath();
        var bash = PathFinder.Which("bash");
        Assert.SkipWhen(bash.IsNullOrWhiteSpace(), "Bash is not available on this system. Skipping test.");

        try
        {
            var output = await new BashCommand()
                .RunScriptAsync("echo Hello, World!", TestContext.Current.CancellationToken);

            Assert.Equal(0, output.ExitCode);
        }
        catch (Exception ex)
        {
            var cmd = new BashCommand()
                .WithScript("echo Hello, World!");

            var si = cmd.Options.ToStartInfo();
#if NET10_0_OR_GREATER
            var args = new CommandArgs(si.ArgumentList);
#else
            CommandArgs args = si.Arguments;
#endif

            args.Insert(0, si.FileName);
            var msg = $"Context: {args} \n\n{ex.Message}";
            throw new InvalidOperationException(msg, ex);
        }
    }

    [Fact]
    public static void OutputScript()
    {
        var errorText = string.Empty;
        var outputText = string.Empty;
        try
        {
            BashCommand.PrependWindowsGitPath();
            var bash = PathFinder.Which("bash");
            Assert.SkipWhen(bash.IsNullOrWhiteSpace(), "Bash is not available on this system. Skipping test.");

            // Example of how to run a command and get the output
            var output = new BashCommand()
            .WithRunScriptAsFile()
            .OutputScript("echo Hello, World!");

            Console.WriteLine(output.ErrorText());
            errorText = output.ErrorText();
            outputText = output.Text(System.Text.Encoding.ASCII);
            Assert.Equal(0, output.ExitCode);
            Assert.NotEmpty(output.Stdout);

            var text = output.Text().Trim();
            Assert.NotEmpty(text);
            Assert.Equal("Hello, World!", text);
        }
        catch (Exception ex)
        {
            var cmd = new BashCommand()
                .WithRunScriptAsFile()
                .WithScript("echo 'Hello, World!'");

            var si = cmd.Options.ToStartInfo();
#if NET10_0_OR_GREATER
            var args = new CommandArgs(si.ArgumentList);
#else
            CommandArgs args = si.Arguments;
#endif

            var msg = $"Context: {args} \n\n error{errorText} \n\n std:{outputText} \n\n{ex.Message}";
            throw new InvalidOperationException(msg, ex);
        }
    }

    [Fact]
    public static async ValueTask OutputScriptAsync()
    {
        BashCommand.PrependWindowsGitPath();
        var bash = PathFinder.Which("bash");
        Assert.SkipWhen(bash.IsNullOrWhiteSpace(), "Bash is not available on this system. Skipping test.");

        // Example of how to run a command asynchronously and get the output
        var output = await new BashCommand()
           .OutputScriptAsync("echo 'Hello, World!'", TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);

        var text = output.Text().Trim();
        Assert.NotEmpty(text);
        Assert.Equal("Hello, World!", text);
    }
}