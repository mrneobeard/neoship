using System.Text;

namespace NeoBeard.IO.Tests;

public class IO_Tests
{
    [Fact]
    public void DisposableDirectory_Dispose_DeletesDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);

        var called = false;
        var disposable = new DisposableDirectory(path, _ => called = true);

        disposable.Dispose();

        Assert.False(Directory.Exists(path));
        Assert.True(called);
    }

    [Fact]
    public void DisposableFile_Dispose_DeletesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, "value");

        var called = false;
        var disposable = new DisposableFile(path, _ => called = true);

        disposable.Dispose();

        Assert.False(File.Exists(path));
        Assert.True(called);
    }

    [Fact]
    public void SimpleTeeTextWriter_WriteLine_WritesToBoth()
    {
        using var w1 = new StringWriter();
        using var w2 = new StringWriter();
        using var tee = new SimpleTeeTextWriter(w1, w2, leaveWriterOpen1: true, leaveWriterOpen2: true);

        tee.WriteLine("hello");

        Assert.Equal(w1.ToString(), w2.ToString());
        Assert.Contains("hello", w1.ToString());
    }

    [Fact]
    public void TeeTextWriter_Write_WritesToAllWriters()
    {
        using var w1 = new StringWriter();
        using var w2 = new StringWriter();
        using var tee = new TeeTextWriter(w1, w2);

        tee.Write('x');
        tee.WriteLine("abc");

        Assert.Equal(w1.ToString(), w2.ToString());
        Assert.Contains("x", w1.ToString());
        Assert.Contains("abc", w1.ToString());
    }
}