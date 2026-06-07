using Xunit;

namespace NeoBeard.Results.Tests;

public class Result1_Tests
{
    [Fact]
    public void Constructor_WithValue_SetsHasValue()
    {
        var result = Result.Ok("test");
        Assert.True(result.HasValue);
        Assert.False(result.HasError);
        Assert.Equal("test", result.Value!);
    }

    [Fact]
    public void Constructor_WithError_SetsHasValueFalse()
    {
        var ex = new Exception("test error");
        var error = new Error(ex);
        var result = new Result<string>(error);
        Assert.False(result.HasValue);
        Assert.True(result.HasError);
        Assert.Equal(error, result.ErrorOrDefault());
    }

    [Fact]
    public void Map_WhenOk_TransformsValue()
    {
        var result = new Result<string>("test");
        var mapped = result.MapResult(s => s.Length);
        Assert.True(mapped.HasValue);
        Assert.Equal(4, mapped.Value);
    }

    [Fact]
    public void Map_WhenError_PropagatesError()
    {
        var ex = new Exception("test error");
        var error = new Error(ex);
        var result = new Result<string>(error);
        var mapped = result.MapResult(s => s.Length);
        Assert.True(mapped.HasError);
        Assert.Equal(error.Message, mapped.ErrorOrDefault().Message);
    }

    [Fact]
    public void Or_WhenOk_ReturnsOriginal()
    {
        var result = new Result<string>("test");
        var or = result.Or("other");
        Assert.Equal("test", or.Value);
    }

    [Fact]
    public void Or_WhenError_ReturnsAlternative()
    {
        var result = new Result<string>(new Exception());
        var or = result.Or("other");
        Assert.Equal("other", or.Value);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var result1 = new Result<string>("test");
        var result2 = new Result<string>("test");

        Assert.Equal(result1.Value, result2.Value);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        var result1 = new Result<string>("test1");
        var result2 = new Result<string>("test2");
        Assert.NotEqual(result1.Value, result2.Value);
    }

    [Fact]
    public void ImplicitOperator_FromValue_CreatesOkResult()
    {
        Result<string> result = "test";
        Assert.True(result.HasValue);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void ImplicitOperator_FromError_CreatesErrorResult()
    {
        var ex = new Exception("test error");
        var error = new Error(ex);
        Result<string> result = error;
        Assert.True(result.HasError);
        Assert.Equal(error, result.ErrorOrDefault());
    }
}
