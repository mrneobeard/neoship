namespace NeoBeard.Exec.Tests;

public static class PathHintTests
{
    [Fact]
    public static void Constructor_WithName_ShouldSetNameProperty()
    {
        var hint = new PathHint("node");

        Assert.Equal("node", hint.Name);
    }

    [Fact]
    public static void Executable_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            Executable = "/usr/bin/node",
        };

        Assert.Equal("/usr/bin/node", hint.Executable);
    }

    [Fact]
    public static void Variable_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            Variable = "NODE_PATH",
        };

        Assert.Equal("NODE_PATH", hint.Variable);
    }

    [Fact]
    public static void CachedPath_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            CachedPath = "/usr/bin/node",
        };

        Assert.Equal("/usr/bin/node", hint.CachedPath);
    }

    [Fact]
    public static void Windows_ShouldBeInitializedAsEmptyHashSet()
    {
        var hint = new PathHint("node");

        Assert.Empty(hint.Windows);
        Assert.IsType<HashSet<string>>(hint.Windows);
    }

    [Fact]
    public static void Windows_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            Windows = { @"C:\Program Files\nodejs\node.exe" },
        };

        Assert.Single(hint.Windows);
        Assert.Contains(@"C:\Program Files\nodejs\node.exe", hint.Windows);
    }

    [Fact]
    public static void Linux_ShouldBeInitializedAsEmptyHashSet()
    {
        var hint = new PathHint("node");

        Assert.Empty(hint.Linux);
        Assert.IsType<HashSet<string>>(hint.Linux);
    }

    [Fact]
    public static void Linux_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            Linux = { "/usr/bin/node", "/usr/local/bin/node" },
        };

        Assert.Equal(2, hint.Linux.Count);
    }

    [Fact]
    public static void Darwin_ShouldBeInitializedAsEmptyHashSet()
    {
        var hint = new PathHint("node");

        Assert.Empty(hint.Darwin);
        Assert.IsType<HashSet<string>>(hint.Darwin);
    }

    [Fact]
    public static void Darwin_ShouldBeSettable()
    {
        var hint = new PathHint("node")
        {
            Darwin = { "/usr/local/bin/node" },
        };

        Assert.Single(hint.Darwin);
    }
}