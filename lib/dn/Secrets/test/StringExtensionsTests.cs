using NeoBeard.Sys.Strings;

namespace NeoBeard.Secrets.Tests;

public class StringExtensionsTests
{
    [Fact]
    public void Search_ShouldFindAllOccurrences()
    {
        string text = "abc abc abc";
        var hits = text.Search("abc");

        Assert.Equal(3, hits.Count);
        Assert.Equal(0, hits[0].Start);
        Assert.Equal(4, hits[1].Start);
        Assert.Equal(8, hits[2].Start);
    }

    [Fact]
    public void Search_WithOverlappingValues_ShouldFindLongestFirst()
    {
        // Search(IEnumerable<string> values) orders by length descending
        string text = "foobar";
        var hits = text.Search(new[] { "foo", "foobar" });

        Assert.Single(hits);
        Assert.Equal(0, hits[0].Start);
        Assert.Equal(6, hits[0].Length);
    }

    [Fact]
    public void SearchAndReplace_ShouldReplaceCorrectly()
    {
        string text = "The quick brown fox jumps over the lazy dog";
        string result = text.SearchAndReplace("the", "THE", StringComparison.OrdinalIgnoreCase);

        Assert.Equal("THE quick brown fox jumps over THE lazy dog", result);
    }

    [Fact]
    public void SearchAndReplace_WithMultipleValues_ShouldReplaceAll()
    {
        string text = "one two three";
        string result = text.SearchAndReplace(new[] { "one", "two" }, "number");

        Assert.Equal("number number three", result);
    }

    [Fact]
    public void Search_WithEmptySource_ShouldReturnEmpty()
    {
        Assert.Empty(((string?)null).Search("abc"));
        Assert.Empty(string.Empty.Search("abc"));
    }

    [Fact]
    public void Search_WithEmptyValue_ShouldReturnEmpty()
    {
        Assert.Empty("abc".Search(string.Empty));
    }
}