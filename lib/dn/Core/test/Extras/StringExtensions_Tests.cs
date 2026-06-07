using NeoBeard.Extras;

namespace NeoBeard.Extras.Tests;

public class StringExtensions_Tests
{
    [Fact]
    public void ContainsFold_MatchesIgnoringCase()
    {
        var ok = "HelloWorld".ContainsFold("world");

        Assert.True(ok);
    }

    [Fact]
    public void StartsWithFold_AndEndsWithFold_MatchIgnoringCase()
    {
        Assert.True("HelloWorld".StartsWithFold("hello"));
        Assert.True("HelloWorld".EndsWithFold("WORLD"));
    }

    [Fact]
    public void Hyphenate_ConvertsMixedInput()
    {
        var value = "My_Value Name".Hyphenate();

        Assert.Equal("my-value-name", value);
    }

    [Fact]
    public void Underscore_ConvertsMixedInput()
    {
        var value = "My-Value Name".Underscore();

        Assert.Equal("my_value_name", value);
    }

    [Fact]
    public void ScreamingSnakeCase_ConvertsMixedInput()
    {
        var value = "My-Value name".ScreamingSnakeCase();

        Assert.Equal("MY_VALUE_NAME", value);
    }

    [Fact]
    public void EqualsFold_ReadOnlySpan_MatchesIgnoringCase()
    {
        var left = "Token".AsSpan();
        var right = "token".AsSpan();

        var ok = left.EqualsFold(right);

        Assert.True(ok);
    }
}