using Xunit;

using Res = NeoBeard.Results.Result<string, string>;

namespace NeoBeard.Results.Tests;

public class Resuls2Tests
{
    [Fact]
    public void ValueConstructor_CreatesSuccessResult()
    {
        var result = Result.Ok<string, string>("success");
        Assert.True(result.HasValue);
        Assert.False(result.HasError);
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public void ErrorConstructor_CreatesErrorResult()
    {
        var result = Result.Fail<string, string>(Error<string>.From("error"));
        Assert.False(result.HasValue);
        Assert.True(result.HasError);
        Assert.Equal("error", result.ErrorOrDefault().Message);
    }

    [Fact]
    public void TryGetValue_WhenSuccess_ReturnsTrue()
    {
        var result = new Res("success");
        if (result.TryGetValue(out string value))
        {
            Assert.Equal("success", value);
        }
        else
        {
            Assert.Fail("Expected success, but got error");
        }
    }

    [Fact]
    public void TryGetError_WhenError_ReturnsTrue()
    {
        var result = new Res(Error<string>.From("error"));
        if (result.TryGetError(out Error<string> error))
        {
            Assert.Equal("error", error.Message);
        }
        else
        {
            Assert.Fail("Expected error, but got success");
        }

    }

    [Fact]
    public void Map_WhenSuccess_TransformsValue()
    {
        var result = Res.Ok("5");
        var mapped = result.MapResult(int.Parse);
        Assert.True(mapped.HasValue);
        Assert.Equal(5, mapped.ValueOrDefault());
    }

    [Fact]
    public void MapError_WhenError_TransformsError()
    {
        var result = Res.Fail(Error<string>.From("error"));
        var mapped = result.MapResult<string, int>(o => o, e => e.Message.Length);
        Assert.True(mapped.HasError);
        Assert.Equal("5", mapped.ErrorOrDefault().Message);
    }

    [Fact]
    public void Equals_WhenBothSuccess_ComparesByValue()
    {
        var result1 = Res.Ok("success");
        var result2 = Res.Ok("success");
        Assert.Equal(result1.Value, result2.Value);
    }

    [Fact]
    public void OrDefault_WhenError_ReturnsDefault()
    {
        var result = Res.Fail(Error<string>.From("error"));
        var value = result.Or("default");
        Assert.True(value.HasValue);
        if (value.TryGetValue(out string value2))
        {
            Assert.Equal("default", value2);
        }
        else
        {
            Assert.Fail("Expected success, but got error");
        }
    }
}
