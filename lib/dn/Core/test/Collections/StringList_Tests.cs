using System.Collections.Specialized;

using NeoBeard.Collections.Generic;

namespace NeoBeard.Collections.Tests;

public class StringList_Tests
{
    [Fact]
    public void IndexOfFold_IgnoresCase()
    {
        var list = new StringList(new[] { "Alpha", "Beta" });

        var index = list.IndexOfFold("beta");

        Assert.Equal(1, index);
    }

    [Fact]
    public void ContainsFold_ReturnsFalse_WhenMissing()
    {
        var list = new StringList(new[] { "Alpha", "Beta" });

        var has = list.ContainsFold("gamma");

        Assert.False(has);
    }

    [Fact]
    public void Ctor_StringCollection_CopiesValues()
    {
        var set = new StringCollection();
        set.Add("one");
        set.Add("two");

        var list = new StringList(set);

        Assert.Equal(2, list.Count);
        Assert.Equal("one", list[0]);
        Assert.Equal("two", list[1]);
    }
}