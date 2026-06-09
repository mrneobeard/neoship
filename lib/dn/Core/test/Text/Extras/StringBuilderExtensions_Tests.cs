using System.Text;

using NeoBeard.Text.Extras;

namespace NeoBeard.Text.Extras.Tests;

public class StringBuilderExtensions_Tests
{
    [Fact]
    public void CopyTo_WithCount_CopiesFromStart()
    {
        var builder = new StringBuilder("abcdef");
        Span<char> span = stackalloc char[3];

        builder.CopyTo(span, 3);

        Assert.Equal("abc", new string(span));
    }

    [Fact]
    public void CopyTo_AllSpan_CopiesExpectedChars()
    {
        var builder = new StringBuilder("wxyz");
        Span<char> span = stackalloc char[4];

        builder.CopyTo(span);

        Assert.Equal("wxyz", new string(span));
    }

    [Fact]
    public void ToArray_ReturnsAllChars()
    {
        var builder = new StringBuilder("hello");

        var chars = builder.ToArray();

        Assert.Equal(['h', 'e', 'l', 'l', 'o'], chars);
    }

    [Fact]
    public void AsSpan_ReturnsContent()
    {
        var builder = new StringBuilder("alpha");

        var span = builder.AsSpan();

        Assert.Equal("alpha", span.ToString());
    }

    [Fact]
    public void AsReadOnlySpan_ReturnsContent()
    {
        var builder = new StringBuilder("beta");

        var span = builder.AsReadOnlySpan();

        Assert.Equal("beta", span.ToString());
    }
}