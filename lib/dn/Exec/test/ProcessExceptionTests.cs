namespace NeoBeard.Exec.Tests;

public static class ProcessExceptionTests
{
    [Fact]
    public static void DefaultConstructor_ShouldCreateException()
    {
        var ex = new ProcessException();

        Assert.NotNull(ex);
    }

    [Fact]
    public static void Constructor_WithExitCode_ShouldSetMessageWithExitCode()
    {
        var ex = new ProcessException(42);

        Assert.Equal("Process exited with code 42", ex.Message);
    }

    [Fact]
    public static void Constructor_WithExitCodeAndProcessName_ShouldSetMessageWithProcessName()
    {
        var ex = new ProcessException(1, "git");

        Assert.Equal("Process git exited with code 1", ex.Message);
    }

    [Fact]
    public static void Constructor_WithCustomMessage_ShouldSetCustomMessage()
    {
        var ex = new ProcessException(1, "git", "Git command failed");

        Assert.Equal("Git command failed", ex.Message);
    }

    [Fact]
    public static void Constructor_WithNullMessage_ShouldUseDefaultMessage()
    {
        var ex = new ProcessException(1, "git", null);

        Assert.Equal("Process git exited with code 1", ex.Message);
    }

    [Fact]
    public static void Constructor_WithInnerException_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new ProcessException(1, "git", "Failed", inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public static void Constructor_WithMessageOnly_ShouldSetMessage()
    {
        var ex = new ProcessException("Custom error message");

        Assert.Equal("Custom error message", ex.Message);
    }

    [Fact]
    public static void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        var inner = new InvalidOperationException("Inner error");
        var ex = new ProcessException("Custom error", inner);

        Assert.Equal("Custom error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public static void ProcessException_ShouldInheritFromSystemException()
    {
        var ex = new ProcessException();

        Assert.IsType<SystemException>(ex, false);
    }
}