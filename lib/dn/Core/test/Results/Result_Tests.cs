using Xunit;

namespace NeoBeard.Results.Tests;

public class Result_Tests
{
    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        var result = Result.Ok();

        Assert.True(result.HasValue);
        Assert.False(result.HasError);
    }

    [Fact]
    public void Fail_CreatesErrorResult()
    {
        var exception = new Exception("test");
        var error = new Error(exception);
        var result = Result.Fail(error);

        Assert.False(result.HasValue);
        Assert.True(result.HasError);
        Assert.Equal(error, result.ErrorOrDefault());
        Assert.Same(exception, result.ErrorOrDefault().Exception);
    }

    [Fact]
    public void Try_Success_ReturnsOkResult()
    {
        var result = Result.Try(() => { });

        Assert.True(result.HasValue);
    }

    [Fact]
    public void TryCatch_Exception_ReturnsErrorResult()
    {
        var result = Result.Try(() => throw new InvalidOperationException("test"));

        Assert.True(result.HasError);
        Assert.IsType<InvalidOperationException>(result.ErrorOrDefault().Exception);
    }

    [Fact]
    public async Task TryCatchAsync_Success_ReturnsOkResult()
    {
        var result = await Result.TryAsync(() => Task.CompletedTask);

        Assert.True(result.HasValue);
    }

    [Fact]
    public async Task TryCatchAsync_Exception_ReturnsErrorResult()
    {
        var result = await Result.TryAsync(() => throw new InvalidOperationException("test"));

        Assert.True(result.HasError);
        Assert.IsType<InvalidOperationException>(result.ErrorOrDefault().Exception);
    }

    [Fact]
    public void TryGetError_OnError_ReturnsTrue()
    {
        var exception = new Exception("test");
        var result = Result.Fail(exception);
        if (result.TryGetError(out Error error))
        {
            Assert.Same(exception, error.Exception);
        }
        else
        {
            Assert.Fail("Expected error, but got success");
        }
    }
}