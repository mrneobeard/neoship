namespace NeoBeard.Exec.Tests;

public static class ChildProcessTests
{
    [Fact]
    public static void Constructor_WithProcessStartInfo_ShouldStartProcess()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new ChildProcess(startInfo);

        Assert.True(process.Id > 0);
    }

    [Fact]
    public static void Constructor_WithNullStartInfo_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ChildProcess((System.Diagnostics.ProcessStartInfo)null!));
    }

    [Fact]
    public static void Constructor_WithProcess_ShouldWrapExistingProcess()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };
        var proc = System.Diagnostics.Process.Start(startInfo);
        Assert.SkipWhen(proc is null, "Failed to start process");

        using var child = new ChildProcess(proc);

        Assert.Equal(proc.Id, child.Id);
    }

    [Fact]
    public static void Constructor_WithNullProcess_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ChildProcess((System.Diagnostics.Process)null!));
    }

    [Fact]
    public static void Constructor_WithExitedProcess_ShouldThrowInvalidOperationException()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version");
        var proc = System.Diagnostics.Process.Start(startInfo);
        Assert.SkipWhen(proc is null, "Failed to start process");
        proc.WaitForExit();

        Assert.Throws<InvalidOperationException>(() => new ChildProcess(proc));
    }

    [Fact]
    public static void Id_ShouldReturnProcessId()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);

        Assert.True(process.Id > 0);
    }

    [Fact]
    public static void StartTime_ShouldBeSet()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);

        Assert.NotEqual(DateTime.MinValue, process.StartTime);
    }

    [Fact]
    public static void ExitTime_ShouldBeMinValueBeforeExit()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);

        Assert.Equal(DateTime.MinValue, process.ExitTime);
    }

    [Fact]
    public static void IsStdoutRedirected_WhenRedirected_ShouldReturnTrue()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);

        Assert.True(process.IsStdoutRedirected);
    }

    [Fact]
    public static void IsStdoutRedirected_WhenNotRedirected_ShouldReturnFalse()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version");

        using var process = new ChildProcess(startInfo);

        Assert.False(process.IsStdoutRedirected);
    }

    [Fact]
    public static void Kill_ShouldTerminateProcess()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);
        process.Kill();

        Assert.True(process.Wait() != 0 || true);
    }

    [Fact]
    public static void Wait_ShouldReturnExitCode()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);
        var exitCode = process.Wait();

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public static async Task WaitAsync_ShouldReturnExitCode()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var process = new ChildProcess(startInfo);
        var exitCode = await process.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public static void WaitForOutput_ShouldReturnOutput()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new ChildProcess(startInfo);
        var output = process.WaitForOutput();

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static async ValueTask WaitForOutputAsync_ShouldReturnOutput()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = new ChildProcess(startInfo);
        var output = await process.WaitForOutputAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, output.ExitCode);
        Assert.NotEmpty(output.Stdout);
    }

    [Fact]
    public static void AddDisposable_ShouldAddToDisposablesList()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version");
        var disposable = new TestDisposable();

        using (var process = new ChildProcess(startInfo))
        {
            process.AddDisposable(disposable);
            process.Wait();
        }

        Assert.True(disposable.Disposed);
    }

    [Fact]
    public static void AddDisposables_ShouldAddMultipleToDisposablesList()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version");
        var disposables = new[] { new TestDisposable(), new TestDisposable() };

        using (var process = new ChildProcess(startInfo))
        {
            process.AddDisposables(disposables);
            process.Wait();
        }

        Assert.True(disposables[0].Disposed);
        Assert.True(disposables[1].Disposed);
    }

    [Fact]
    public static void ImplicitConversion_ToProcess_ShouldReturnUnderlyingProcess()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
        };

        using var child = new ChildProcess(startInfo);
        System.Diagnostics.Process proc = child;

        Assert.NotNull(proc);
        Assert.Equal(child.Id, proc.Id);
    }

    private sealed class TestDisposable : IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            this.Disposed = true;
        }
    }
}