using Xunit;

namespace NeoBeard.Results.Tests;

public class ResultsList_Tests
{
    [Fact]
    public void Ctor_Default_CreatesEmptyOkList()
    {
        var list = new ResultsList<string>();

        Assert.Empty(list.Results);
        Assert.True(list.IsOk);
        Assert.False(list.IsError);
    }

    [Fact]
    public void Ctor_IList_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ResultsList<string>((IList<Result<string>>)null!));
    }

    [Fact]
    public void IsError_True_WhenAnyItemIsError()
    {
        var items = new List<Result<string>>
        {
            new("ok"),
            new(new InvalidOperationException("boom")),
        };

        var list = new ResultsList<string>(items);

        Assert.True(list.IsError);
        Assert.False(list.IsOk);
    }

    [Fact]
    public void ToValues_ReturnsOnlySuccessValues_WhenThrowOnErrorFalse()
    {
        var items = new List<Result<string>>
        {
            new("first"),
            new(new InvalidOperationException("boom")),
            new("second"),
        };

        var list = new ResultsList<string>(items);

        var values = list.ToValues(throwOnError: false);

        Assert.Equal(2, values.Count);
        Assert.Equal("first", values[0]);
        Assert.Equal("second", values[1]);
    }

    [Fact]
    public void ToValues_ThrowsAggregate_WhenErrorsPresent_AndThrowOnErrorTrue()
    {
        var items = new List<Result<string>>
        {
            new("first"),
            new(new InvalidOperationException("boom")),
        };

        var list = new ResultsList<string>(items);

        var ex = Assert.Throws<AggregateException>(() => list.ToValues());
        Assert.Single(ex.InnerExceptions);
        Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);
    }

    [Fact]
    public void ToAggregateException_ReturnsMessage_WhenNoErrors()
    {
        var list = new ResultsList<string>(new List<Result<string>> { new("ok") });

        var ex = list.ToAggregateException();

        Assert.Equal("No errors present in results list.", ex.Message);
    }

    [Fact]
    public void ToAggregateException_IncludesAllErrors()
    {
        var e1 = new InvalidOperationException("one");
        var e2 = new ArgumentException("two");
        var list = new ResultsList<string>(new List<Result<string>> { new(e1), new(e2) });

        var ex = list.ToAggregateException();

        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Contains(e1, ex.InnerExceptions);
        Assert.Contains(e2, ex.InnerExceptions);
    }
}