using NeoBeard.Extras;

namespace NeoBeard.Extras.Tests;

public class ArrayExtensions_Tests
{
    [Fact]
    public void Clear_Default_ClearsWholeArray()
    {
        var values = new[] { 1, 2, 3 };

        values.Clear();

        Assert.Equal([0, 0, 0], values);
    }

    [Fact]
    public void Clear_Range_ClearsSubset()
    {
        var values = new[] { 1, 2, 3, 4 };

        values.Clear(startIndex: 1, length: 2);

        Assert.Equal([1, 0, 0, 4], values);
    }

    [Fact]
    public void Clear_StartIndexOutOfRange_Throws()
    {
        var values = new[] { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => values.Clear(startIndex: 3, length: 1));
    }

    [Fact]
    public void Clear_LengthOutOfRange_Throws()
    {
        var values = new[] { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => values.Clear(startIndex: 1, length: 5));
    }
}