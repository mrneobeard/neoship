using NeoBeard.Results;

using Xunit;

namespace NeoBeard.Results.Tests;

public class ValueResult_Tests
{
    [Fact]
    public void TryGetError_WhenOk_ReturnsFalse()
    {
        ValueResult<int, string> result = 42;

        Assert.False(result.TryGetError(out IError error));
        Assert.Null(error);
    }

    [Fact]
    public void TryGetError_WhenError_ReturnsValueAndTrue()
    {
        var result = new ValueResult<int, string>(Error<string>.From("error"));

        Assert.True(result.TryGetError(out Error<string> error));
        Assert.Equal("error", error.Value);
    }

    [Fact]
    public void MapError_WhenError_UsesErrorMapper()
    {
        var result = new ValueResult<int, string>(Error<string>.From("error"));

        var mapped = result.Map(x => x + 1, e => e.Value.Length);

        Assert.Equal("error".Length, mapped);
    }

    [Fact]
    public void ValueOrDefault_WhenOk_ReturnsValue()
    {
        ValueResult<int, string> result = 9;

        Assert.Equal(9, result.ValueOrDefault());
    }

    [Fact]
    public void ValueOrDefault_WhenError_ReturnsDefault()
    {
        var result = new ValueResult<int, string>(Error<string>.From("error"));

        Assert.Equal(0, result.ValueOrDefault());
    }

    [Fact]
    public void ErrorOrDefault_WithFactory_UsesFactoryWhenOk()
    {
        ValueResult<int, string> result = 1;

        var value = result.ErrorOrDefault(() => Error<string>.From("not-used"));

        Assert.Equal("not-used", value.Message);
    }
}