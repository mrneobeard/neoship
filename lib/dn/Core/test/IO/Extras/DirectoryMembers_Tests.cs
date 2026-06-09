using NeoBeard.IO.Extras;

namespace NeoBeard.IO.Extras.Tests;

public class DirectoryMembers_Tests
{
    [Fact]
    public void Ensure_CreatesMissingDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var info = Directory.Ensure(path);

            Assert.True(info.Exists);
            Assert.True(Directory.Exists(path));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void Read_MissingDirectory_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Assert.Throws<DirectoryNotFoundException>(() => Directory.Read(path).ToList());
    }

    [Fact]
    public void Copy_RecursiveFalse_CopiesTopLevelFilesOnly()
    {
        var source = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "a.txt"), "a");
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            File.WriteAllText(Path.Combine(source, "sub", "b.txt"), "b");

            Directory.Copy(source, dest, recursive: false);

            Assert.True(File.Exists(Path.Combine(dest, "a.txt")));
            Assert.False(Directory.Exists(Path.Combine(dest, "sub")));
        }
        finally
        {
            if (Directory.Exists(source))
                Directory.Delete(source, true);
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);
        }
    }

    [Fact]
    public void Copy_RecursiveTrue_CopiesSubdirectories()
    {
        var source = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var dest = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            File.WriteAllText(Path.Combine(source, "sub", "b.txt"), "b");

            Directory.Copy(source, dest, recursive: true);

            Assert.True(File.Exists(Path.Combine(dest, "sub", "b.txt")));
        }
        finally
        {
            if (Directory.Exists(source))
                Directory.Delete(source, true);
            if (Directory.Exists(dest))
                Directory.Delete(dest, true);
        }
    }
}