using NeoBeard;

namespace NeoBeard.Tests;

public class AnsiDecorationsTests
{
    [Fact]
    public void None_Has_Value_Zero()
    {
        Assert.Equal(0, (int)AnsiDecorations.None);
    }

    [Fact]
    public void Bold_Has_Value_One()
    {
        Assert.Equal(1, (int)AnsiDecorations.Bold);
    }

    [Fact]
    public void Italic_Has_Value_Four()
    {
        Assert.Equal(4, (int)AnsiDecorations.Italic);
    }

    [Fact]
    public void Combined_Flags_Properly()
    {
        var combined = AnsiDecorations.Bold | AnsiDecorations.Italic;
        Assert.Equal((AnsiDecorations)5, combined);
    }

    [Fact]
    public void HasFlag_Returns_True_For_Set_Flag()
    {
        var combined = AnsiDecorations.Bold | AnsiDecorations.Italic;
        Assert.True(combined.HasFlag(AnsiDecorations.Bold));
        Assert.True(combined.HasFlag(AnsiDecorations.Italic));
    }

    [Fact]
    public void HasFlag_Returns_False_For_Unset_Flag()
    {
        var combined = AnsiDecorations.Bold | AnsiDecorations.Italic;
        Assert.False(combined.HasFlag(AnsiDecorations.Underline));
        Assert.False(combined.HasFlag(AnsiDecorations.Dim));
    }

    [Fact]
    public void HasFlag_Returns_True_For_None_On_Any_Value()
    {
        var combined = AnsiDecorations.Bold | AnsiDecorations.Italic;
        Assert.True(combined.HasFlag(AnsiDecorations.None));
    }

    [Fact]
    public void Multiple_Flags_Can_Be_Combined()
    {
        var combined = AnsiDecorations.Bold | AnsiDecorations.Dim | AnsiDecorations.Underline | AnsiDecorations.Strikethrough;
        Assert.True(combined.HasFlag(AnsiDecorations.Bold));
        Assert.True(combined.HasFlag(AnsiDecorations.Dim));
        Assert.True(combined.HasFlag(AnsiDecorations.Underline));
        Assert.True(combined.HasFlag(AnsiDecorations.Strikethrough));
    }

    [Fact]
    public void None_HasFlag_Only_None()
    {
        Assert.True(AnsiDecorations.None.HasFlag(AnsiDecorations.None));
        Assert.False(AnsiDecorations.None.HasFlag(AnsiDecorations.Bold));
    }
}