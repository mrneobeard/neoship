#pragma warning disable SA1129
namespace NeoBeard.Exec.Tests;

public static class OutputTests
{
    [Fact]
    public static void DefaultConstructor_ShouldInitializeWithDefaultValues()
    {
        var output = new Output();

        Assert.Equal(0, output.ExitCode);
        Assert.NotNull(output.Stdout);
        Assert.NotNull(output.Stderr);
        Assert.Equal(DateTime.MinValue, output.StartTime);
        Assert.Equal(DateTime.MinValue, output.ExitTime);
        Assert.Null(output.Error);
    }

    [Fact]
    public static void Constructor_WithExitCodeAndFileName_ShouldSetProperties()
    {
        var startTime = DateTime.UtcNow.AddHours(-1);
        var exitTime = DateTime.UtcNow;

        var output = new Output(
            "dotnet",
            0,
            stdout: [1, 2, 3],
            stderr: [4, 5, 6],
            startTime: startTime,
            exitTime: exitTime);

        Assert.Equal("dotnet", output.FileName);
        Assert.Equal(0, output.ExitCode);
        Assert.Equal([1, 2, 3], output.Stdout);
        Assert.Equal([4, 5, 6], output.Stderr);
        Assert.Equal(startTime, output.StartTime);
        Assert.Equal(exitTime, output.ExitTime);
        Assert.Null(output.Error);
    }

    [Fact]
    public static void Constructor_WithError_ShouldSetErrorProperty()
    {
        var ex = new InvalidOperationException("Test error");

        var output = new Output("dotnet", 1, ex);

        Assert.Equal("dotnet", output.FileName);
        Assert.Equal(1, output.ExitCode);
        Assert.NotNull(output.Error);
        Assert.Same(ex, output.Error);
    }

    [Fact]
    public static void IsOk_WhenExitCodeIsZeroAndNoError_ShouldReturnTrue()
    {
        var output = new Output("dotnet", 0);

        Assert.True(output.IsOk);
        Assert.False(output.IsError);
    }

    [Fact]
    public static void IsOk_WhenExitCodeIsNonZero_ShouldReturnFalse()
    {
        var output = new Output("dotnet", 1);

        Assert.False(output.IsOk);
        Assert.True(output.IsError);
    }

    [Fact]
    public static void IsOk_WhenErrorIsSet_ShouldReturnFalse()
    {
        var output = new Output("dotnet", 0, new InvalidOperationException("Error"));

        Assert.False(output.IsOk);
        Assert.True(output.IsError);
    }

    [Fact]
    public static void ThrowOnBadExit_WhenExitCodeIsZero_ShouldReturnSameOutput()
    {
        var output = new Output("dotnet", 0);

        var result = output.ThrowOnBadExit();

        Assert.Equal(output, result);
    }

    [Fact]
    public static void ThrowOnBadExit_WhenExitCodeIsNonZero_ShouldThrowProcessException()
    {
        var output = new Output("dotnet", 1);

        Assert.Throws<ProcessException>(() => output.ThrowOnBadExit());
    }

    [Fact]
    public static void ThrowOnBadExit_WhenErrorIsSet_ShouldThrowProcessException()
    {
        var innerEx = new InvalidOperationException("Inner error");
        var output = new Output("dotnet", 0, innerEx);

        var ex = Assert.Throws<ProcessException>(() => output.ThrowOnBadExit());
        Assert.Contains("failed with error", ex.Message);
    }

    [Fact]
    public static void ThrowOnBadExit_WithValidator_WhenValid_ShouldReturnSameOutput()
    {
        var output = new Output("dotnet", 0);

        var result = output.ThrowOnBadExit((code, error) => true);

        Assert.Equal(output, result);
    }

    [Fact]
    public static void ThrowOnBadExit_WithValidator_WhenInvalid_ShouldThrowProcessException()
    {
        var output = new Output("dotnet", 1);

        Assert.Throws<ProcessException>(() => output.ThrowOnBadExit((code, error) => false));
    }

    [Fact]
    public static void ThrowOnBadExit_WithValidator_ShouldAllowCustomExitCodeValidation()
    {
        var output = new Output("git", 1);

        var result = output.ThrowOnBadExit((code, error) => code == 0 || code == 1);

        Assert.Equal(output, result);
    }

    [Fact]
    public static void Text_WithUtf8Encoding_ShouldReturnDecodedString()
    {
        var text = "Hello, World!";
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var output = new Output("test", 0, stdout: bytes);

        var result = output.Text();

        Assert.Equal(text, result);
    }

    [Fact]
    public static void Text_WhenStdoutIsEmpty_ShouldReturnEmptyString()
    {
        var output = new Output("test", 0);

        var result = output.Text();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public static void Text_WithCustomEncoding_ShouldUseSpecifiedEncoding()
    {
        var text = "Hello, World!";
        var encoding = System.Text.Encoding.Unicode;
        var bytes = encoding.GetBytes(text);
        var output = new Output("test", 0, stdout: bytes);

        var result = output.Text(encoding);

        Assert.Equal(text, result);
    }

    [Fact]
    public static void ErrorText_WithUtf8Encoding_ShouldReturnDecodedString()
    {
        var text = "Error occurred!";
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var output = new Output("test", 1, stderr: bytes);

        var result = output.ErrorText();

        Assert.Equal(text, result);
    }

    [Fact]
    public static void ErrorText_WhenStderrIsEmpty_ShouldReturnEmptyString()
    {
        var output = new Output("test", 0);

        var result = output.ErrorText();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public static void Lines_ShouldReturnAllLinesFromStdout()
    {
        var text = "Line1\nLine2\nLine3";
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var output = new Output("test", 0, stdout: bytes);

        var lines = output.Lines().ToList();

        Assert.Equal(3, lines.Count);
        Assert.Equal("Line1", lines[0]);
        Assert.Equal("Line2", lines[1]);
        Assert.Equal("Line3", lines[2]);
    }

    [Fact]
    public static void Lines_WhenStdoutIsEmpty_ShouldReturnEmptySequence()
    {
        var output = new Output("test", 0);

        var lines = output.Lines().ToList();

        Assert.Empty(lines);
    }

    [Fact]
    public static void ErrorLines_ShouldReturnAllLinesFromStderr()
    {
        var text = "Error1\nError2";
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var output = new Output("test", 1, stderr: bytes);

        var lines = output.ErrorLines().ToList();

        Assert.Equal(2, lines.Count);
        Assert.Equal("Error1", lines[0]);
        Assert.Equal("Error2", lines[1]);
    }

    [Fact]
    public static void ErrorLines_WhenStderrIsEmpty_ShouldReturnEmptySequence()
    {
        var output = new Output("test", 0);

        var lines = output.ErrorLines().ToList();

        Assert.Empty(lines);
    }
}