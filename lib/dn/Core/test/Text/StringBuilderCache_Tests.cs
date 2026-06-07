using System.Text;

using NeoBeard.Text;

namespace NeoBeard.Text.Tests;

public class StringBuilderCache_Tests
{
    [Fact]
    public void Acquire_NoCache_HasRequestedCapacityOrMore()
    {
        var builder = StringBuilderCache.Acquire(40);

        Assert.True(builder.Capacity >= 40);
    }

    [Fact]
    public void Acquire_AfterRelease_ReusesAndClearsBuilder()
    {
        var builder = StringBuilderCache.Acquire();
        builder.Append("abc");

        StringBuilderCache.Release(builder);

        var next = StringBuilderCache.Acquire();

        Assert.Same(builder, next);
        Assert.Equal(0, next.Length);
    }

    [Fact]
    public void Acquire_UsesCachedBuilder_WhenRequestedCapacityIsSmaller()
    {
        var builder = StringBuilderCache.Acquire(64);
        builder.Append("x");
        StringBuilderCache.Release(builder);

        var next = StringBuilderCache.Acquire(16);

        Assert.Same(builder, next);
        Assert.Equal(0, next.Length);
        Assert.True(next.Capacity >= 16);
    }

    [Fact]
    public void Acquire_DoesNotUseCachedBuilder_WhenRequestedCapacityIsLarger()
    {
        var builder = StringBuilderCache.Acquire(16);
        StringBuilderCache.Release(builder);

        var next = StringBuilderCache.Acquire(200);

        Assert.NotSame(builder, next);
        Assert.True(next.Capacity >= 200);
    }

    [Fact]
    public void Release_LargeBuilder_DoesNotCache()
    {
        var builder = new StringBuilder(512);

        StringBuilderCache.Release(builder);

        var next = StringBuilderCache.Acquire(512);

        Assert.NotSame(builder, next);
    }

    [Fact]
    public void GetStringAndRelease_ReturnsText_AndReleasesBuilder()
    {
        var builder = StringBuilderCache.Acquire();
        builder.Append("hello");

        var text = StringBuilderCache.GetStringAndRelease(builder);
        var next = StringBuilderCache.Acquire();

        Assert.Equal("hello", text);
        Assert.Same(builder, next);
        Assert.Equal(0, next.Length);
    }
}