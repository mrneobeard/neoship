namespace NeoBeard.Exec.Tests;

public static class PathFinderTests
{
    [Fact]
    public static void Default_ShouldReturnSingletonInstance()
    {
        var instance1 = PathFinder.Default;
        var instance2 = PathFinder.Default;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public static void Has_WhenNotRegistered_ShouldReturnFalse()
    {
        var uniqueName = $"test_not_registered_{Guid.NewGuid()}";

        var result = PathFinder.Default.Has(uniqueName);

        Assert.False(result);
    }

    [Fact]
    public static void Has_WhenRegistered_ShouldReturnTrue()
    {
        var uniqueName = $"test_registered_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);
        PathFinder.Default.Register(uniqueName, hint);

        var result = PathFinder.Default.Has(uniqueName);

        Assert.True(result);
    }

    [Fact]
    public static void Register_ShouldAddEntryToPathFinder()
    {
        var uniqueName = $"test_register_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);
        PathFinder.Default.Register(uniqueName, hint);

        Assert.True(PathFinder.Default.Has(uniqueName));
    }

    [Fact]
    public static void Register_ShouldSetVariableWhenNotProvided()
    {
        var uniqueName = "mytool";
        var hint = new PathHint(uniqueName);
        hint.Variable = null;

        PathFinder.Default.Register($"test_var_{Guid.NewGuid()}", hint);

        Assert.NotNull(hint.Variable);
        Assert.Contains("_PATH", hint.Variable);
    }

    [Fact]
    public static void Register_WithFactory_ShouldCreateEntryIfNotExists()
    {
        var uniqueName = $"test_factory_{Guid.NewGuid()}";
        PathFinder.Default.Register(uniqueName, () => new PathHint(uniqueName)
        {
            Executable = "test_exe",
        });

        Assert.True(PathFinder.Default.Has(uniqueName));
    }

    [Fact]
    public static void Register_WithFactory_ShouldNotOverwriteExisting()
    {
        var uniqueName = $"test_factory_no_overwrite_{Guid.NewGuid()}";
        var original = new PathHint(uniqueName) { Executable = "original" };
        PathFinder.Default.Register(uniqueName, original);

        PathFinder.Default.Register(uniqueName, () => new PathHint(uniqueName) { Executable = "new" });

        var entry = PathFinder.Default[uniqueName];
        Assert.Equal("original", entry!.Executable);
    }

    [Fact]
    public static void Indexer_Get_WhenNotExists_ShouldReturnNull()
    {
        var result = PathFinder.Default[$"nonexistent_{Guid.NewGuid()}"];

        Assert.Null(result);
    }

    [Fact]
    public static void Indexer_Get_WhenExists_ShouldReturnEntry()
    {
        var uniqueName = $"test_indexer_get_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);
        PathFinder.Default.Register(uniqueName, hint);

        var result = PathFinder.Default[uniqueName];

        Assert.Same(hint, result);
    }

    [Fact]
    public static void Indexer_Set_WithNull_ShouldRemoveEntry()
    {
        var uniqueName = $"test_indexer_remove_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);
        PathFinder.Default.Register(uniqueName, hint);

        PathFinder.Default[uniqueName] = null;

        Assert.False(PathFinder.Default.Has(uniqueName));
    }

    [Fact]
    public static void Indexer_Set_WithValue_ShouldAddEntry()
    {
        var uniqueName = $"test_indexer_set_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);

        PathFinder.Default[uniqueName] = hint;

        Assert.True(PathFinder.Default.Has(uniqueName));
        Assert.Same(hint, PathFinder.Default[uniqueName]);
    }

    [Fact]
    public static void Find_WithFullyQualifiedPath_ShouldReturnSamePath()
    {
        var path = "/usr/bin/dotnet";

        var result = PathFinder.Default.Find(path);

        Assert.Equal(path, result);
    }

    [Fact]
    public static void FindOrThrow_WhenFound_ShouldReturnPath()
    {
        var dotnet = PathFinder.Which("dotnet");
        Assert.SkipWhen(dotnet is null, "dotnet not found on PATH");

        var result = PathFinder.Default.FindOrThrow("dotnet");

        Assert.NotNull(result);
    }

    [Fact]
    public static void FindOrThrow_WhenNotFound_ShouldThrowFileNotFoundException()
    {
        var uniqueName = $"nonexistent_{Guid.NewGuid()}_cmd";

        Assert.Throws<FileNotFoundException>(() => PathFinder.Default.FindOrThrow(uniqueName));
    }

    [Fact]
    public static void Update_WhenExists_ShouldUpdateEntry()
    {
        var uniqueName = $"test_update_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName);
        PathFinder.Default.Register(uniqueName, hint);

        PathFinder.Default.Update(uniqueName, h => h.Executable = "updated_exe");

        Assert.Equal("updated_exe", hint.Executable);
    }

    [Fact]
    public static void Update_WhenNotExists_ShouldNotThrow()
    {
        var uniqueName = $"test_update_nonexistent_{Guid.NewGuid()}";

        PathFinder.Default.Update(uniqueName, h => h.Executable = "should_not_be_set");
    }

    [Fact]
    public static void RegisterOrUpdate_WhenNotExists_ShouldCreateAndRegister()
    {
        var uniqueName = $"test_register_or_update_{Guid.NewGuid()}";

        PathFinder.Default.RegisterOrUpdate(uniqueName, h => h.Executable = "new_exe");

        Assert.True(PathFinder.Default.Has(uniqueName));
        var entry = PathFinder.Default[uniqueName];
        Assert.Equal("new_exe", entry!.Executable);
    }

    [Fact]
    public static void RegisterOrUpdate_WhenExists_ShouldUpdate()
    {
        var uniqueName = $"test_register_or_update_exists_{Guid.NewGuid()}";
        var hint = new PathHint(uniqueName) { Executable = "original" };
        PathFinder.Default.Register(uniqueName, hint);

        PathFinder.Default.RegisterOrUpdate(uniqueName, h => h.Executable = "updated");

        Assert.Equal("updated", hint.Executable);
    }

    [Fact]
    public static void Which_WithNullCommand_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PathFinder.Which(null!));
    }

    [Fact]
    public static void Which_WithEmptyCommand_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PathFinder.Which(string.Empty));
    }

    [Fact]
    public static void Which_WithExistingCommand_ShouldReturnPath()
    {
        var dotnet = PathFinder.Which("dotnet");
        Assert.SkipWhen(dotnet is null, "dotnet not found on PATH");

        Assert.NotNull(dotnet);
        Assert.True(System.IO.File.Exists(dotnet));
    }

    [Fact]
    public static void Which_WithNonExistentCommand_ShouldReturnNull()
    {
        var result = PathFinder.Which($"nonexistent_{Guid.NewGuid()}_cmd");

        Assert.Null(result);
    }
}