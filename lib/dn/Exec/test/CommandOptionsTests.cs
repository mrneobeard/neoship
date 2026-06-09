namespace NeoBeard.Exec.Tests;

public static class CommandOptionsTests
{
    [Fact]
    public static void DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        var options = new CommandOptions();

        Assert.Equal(string.Empty, options.File);
        Assert.Empty(options.Args);
        Assert.Null(options.Cwd);
        Assert.Null(options.Env);
        Assert.Empty(options.Disposables);
        Assert.Equal(Stdio.Inherit, options.Stdout);
        Assert.Equal(Stdio.Inherit, options.Stderr);
        Assert.Equal(Stdio.Inherit, options.Stdin);
        Assert.Null(options.User);
        Assert.Null(options.Verb);
        Assert.False(options.LoadUserProfile);
        Assert.False(options.CreateNoWindow);
        Assert.False(options.UseShellExecute);
    }

    [Fact]
    public static void ToStartInfo_ShouldResolvePath()
    {
        var cmd = new Command().WithExecutable("dotnet");

        var startInfo = cmd.Options.ToStartInfo();

        Assert.Contains("dotnet", startInfo.FileName);
    }

    [Fact]
    public static void ToStartInfo_ShouldSetArguments()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"]);

        var startInfo = cmd.Options.ToStartInfo();

#if NET5_0_OR_GREATER
        Assert.Single(startInfo.ArgumentList);
        Assert.Equal("--version", startInfo.ArgumentList[0]);
#endif
    }

    [Fact]
    public static void ToStartInfo_ShouldSetWorkingDirectory()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithCwd("/home/user/project");

        var startInfo = cmd.Options.ToStartInfo();

        Assert.Equal("/home/user/project", startInfo.WorkingDirectory);
    }

    [Fact]
    public static void ToStartInfo_ShouldSetEnvironmentVariables()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .SetEnv("MY_VAR", "my_value");

        var startInfo = cmd.Options.ToStartInfo();

        Assert.Equal("my_value", startInfo.Environment["MY_VAR"]);
    }

    [Fact]
    public static void ToStartInfo_WithPipedStdout_ShouldRedirectStandardOutput()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithStdout(Stdio.Piped);

        var startInfo = cmd.Options.ToStartInfo();

        Assert.True(startInfo.RedirectStandardOutput);
    }

    [Fact]
    public static void ToStartInfo_WithPipedStderr_ShouldRedirectStandardError()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithStderr(Stdio.Piped);

        var startInfo = cmd.Options.ToStartInfo();

        Assert.True(startInfo.RedirectStandardError);
    }

    [Fact]
    public static void ToStartInfo_WithPipedStdin_ShouldRedirectStandardInput()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithStdin(Stdio.Piped);

        var startInfo = cmd.Options.ToStartInfo();

        Assert.True(startInfo.RedirectStandardInput);
    }

    [Fact]
    public static void ToStartInfo_WithRedirectedStreams_ShouldSetCreateNoWindow()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithStdout(Stdio.Piped);

        var startInfo = cmd.Options.ToStartInfo();

        Assert.True(startInfo.CreateNoWindow);
    }

    [Fact]
    public static void ToStartInfo_WithRedirectedStreams_ShouldDisableShellExecute()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithStdout(Stdio.Piped);

        var startInfo = cmd.Options.ToStartInfo();

        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public static void ToStartInfo_WithExistingStartInfo_ShouldModifyInPlace()
    {
        var cmd = new Command()
            .WithExecutable("dotnet")
            .WithArgs(["--version"])
            .WithCwd("/home/user");
        var startInfo = new System.Diagnostics.ProcessStartInfo();

        cmd.Options.ToStartInfo(startInfo);

        Assert.Contains("dotnet", startInfo.FileName);
        Assert.Equal("/home/user", startInfo.WorkingDirectory);
    }

    [Fact]
    public static void CreateNoWindow_ShouldBeSettable()
    {
        var options = new CommandOptions { CreateNoWindow = true };

        Assert.True(options.CreateNoWindow);
    }

    [Fact]
    public static void UseShellExecute_ShouldBeSettable()
    {
        var options = new CommandOptions { UseShellExecute = true };

        Assert.True(options.UseShellExecute);
    }

    [Fact]
    public static void Verb_ShouldBeSettable()
    {
        var options = new CommandOptions { Verb = "runas" };

        Assert.Equal("runas", options.Verb);
    }
}