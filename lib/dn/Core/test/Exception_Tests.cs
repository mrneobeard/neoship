namespace NeoBeard.Tests;

public static class Exception_Tests
{
    [Fact]
    public static void ResourceNotFound_Default_Uses_Default_Message()
    {
        var exception = new ResourceNotFoundException();

        Assert.Equal("The requested resource was not found.", exception.Message);
        Assert.Null(exception.ResourceName);
        Assert.Null(exception.ResourceType);
    }

    [Fact]
    public static void ResourceNotFound_With_Message_Stores_Message()
    {
        var exception = new ResourceNotFoundException("missing");

        Assert.Equal("missing", exception.Message);
    }

    [Fact]
    public static void ResourceNotFound_With_Inner_Stores_Inner_Exception()
    {
        var inner = new InvalidOperationException("inner");
        var exception = new ResourceNotFoundException("outer", inner);

        Assert.Equal("outer", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public static void ResourceNotFound_With_Resource_Stores_Resource_Metadata()
    {
        var exception = new ResourceNotFoundException("appsettings.json", "file");

        Assert.Equal("appsettings.json", exception.ResourceName);
        Assert.Equal("file", exception.ResourceType);
        Assert.Equal("The resource 'appsettings.json' of type 'file' was not found.", exception.Message);
    }

    [Fact]
    public static void EnvironmentException_Constructors_Store_Message_And_Inner_Exception()
    {
        var defaultException = new EnvironmentException();
        var messageException = new EnvironmentException("bad env");
        var inner = new InvalidOperationException("inner");
        var innerException = new EnvironmentException("outer", inner);

        Assert.NotNull(defaultException.Message);
        Assert.Equal("bad env", messageException.Message);
        Assert.Equal("outer", innerException.Message);
        Assert.Same(inner, innerException.InnerException);
    }
}