using Xunit;

namespace NeoBeard.Results.Tests;

public class ValueResult1_Tests
{
    [Fact]
    public void Constructor_WithValue_SetsHasValue()
    {
        var result = new ValueResult<string, Exception>("test");

        Assert.True(result.HasValue);
        Assert.False(result.HasError);
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void Constructor_WithError_SetsHasError()
    {
        var ex = new Exception("error");
        var result = new ValueResult<string, Exception>(Error<Exception>.From(ex));

        Assert.False(result.HasValue);
        Assert.True(result.HasError);
        Assert.Equal(Error<Exception>.From(ex).Message, result.ErrorOrDefault().Message);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        ValueResult<int, Exception> result = 42;

        Assert.True(result.HasValue);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesErrorResult()
    {
        var ex = new Exception("test error");
        ValueResult<int, Exception> result = Error<Exception>.From(ex);

        Assert.True(result.HasError);
        Assert.Equal(Error<Exception>.From(ex).Message, result.ErrorOrDefault().Message);
    }

    [Fact]
    public void TryGetValue_OnSuccess_ReturnsTrue()
    {
        ValueResult<int, Exception> result = 42;

        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_OnError_ReturnsFalse()
    {
        ValueResult<int, Exception> result = Error<Exception>.From(new Exception("error"));

        Assert.False(result.TryGetValue(out var value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        ValueResult<int, Exception> result1 = 42;
        ValueResult<int, Exception> result2 = 42;

        Assert.Equal(result1.Value, result2.Value);
    }

    [Fact]
    public void Equals_WithDifferentValue_ReturnsFalse()
    {
        ValueResult<int, Exception> result1 = 42;
        ValueResult<int, Exception> result2 = 24;

        Assert.NotEqual(result1.Value, result2.Value);
    }
}