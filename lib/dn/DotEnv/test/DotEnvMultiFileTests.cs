using System;
using System.IO;
using System.Threading.Tasks;

namespace NeoBeard.DotEnv.Tests;

public static class DotEnvMultiFileTests
{
    [Fact]
    public static void ParseFiles_Merges_Documents()
    {
        var tempPath1 = Path.GetTempFileName();
        var tempPath2 = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath1, "KEY1=value1\nKEY2=value2");
            File.WriteAllText(tempPath2, "KEY2=override\nKEY3=value3");

            var doc = DotEnvFile.ParseFiles(tempPath1, tempPath2);

            Assert.Equal("value1", doc.Get("KEY1"));
            Assert.Equal("override", doc.Get("KEY2"));
            Assert.Equal("value3", doc.Get("KEY3"));
        }
        finally
        {
            File.Delete(tempPath1);
            File.Delete(tempPath2);
        }
    }

    [Fact]
    public static void ParseFiles_Required_Missing_Throws()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        Assert.Throws<FileNotFoundException>(() => DotEnvFile.ParseFiles(tempPath));
    }

    [Fact]
    public static void ParseFiles_Optional_Missing_Skipped()
    {
        var requiredPath = Path.GetTempFileName();
        var optionalPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(requiredPath, "KEY=value");

            var doc = DotEnvFile.ParseFiles(requiredPath, optionalPath + "?");

            Assert.Equal("value", doc.Get("KEY"));
        }
        finally
        {
            File.Delete(requiredPath);
        }
    }

    [Fact]
    public static void ParseFiles_All_Optional_Missing_Returns_Empty()
    {
        var optionalPath1 = Path.Combine(Path.GetTempPath(), $"nonexistent1_{Guid.NewGuid()}.env");
        var optionalPath2 = Path.Combine(Path.GetTempPath(), $"nonexistent2_{Guid.NewGuid()}.env");

        var doc = DotEnvFile.ParseFiles(optionalPath1 + "?", optionalPath2 + "?");

        Assert.Equal(0, doc.Count);
    }

    [Fact]
    public static void TryParseFile_Missing_Returns_Error()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        var result = DotEnvFile.TryParseFile(tempPath);

        Assert.False(result.IsOk);
        Assert.NotNull(result.Error);
        Assert.IsType<FileNotFoundException>(result.Error);
    }

    [Fact]
    public static void TryParseFile_Exists_Returns_Doc()
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, "KEY=value");

            var result = DotEnvFile.TryParseFile(tempPath);

            Assert.True(result.IsOk);
            Assert.NotNull(result.Doc);
            Assert.Equal("value", result.Doc.Get("KEY"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public static void TryParseFiles_Missing_Required_Returns_Error()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        var result = DotEnvFile.TryParseFiles(tempPath);

        Assert.False(result.IsOk);
        Assert.IsType<FileNotFoundException>(result.Error);
    }

    [Fact]
    public static void TryParseFiles_Optional_Missing_Succeeds()
    {
        var requiredPath = Path.GetTempFileName();
        var optionalPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        try
        {
            File.WriteAllText(requiredPath, "KEY=value");

            var result = DotEnvFile.TryParseFiles(requiredPath, optionalPath + "?");

            Assert.True(result.IsOk);
            Assert.Equal("value", result.Doc?.Get("KEY"));
        }
        finally
        {
            File.Delete(requiredPath);
        }
    }

    [Fact]
    public static void TryParseFiles_Parse_Error_Returns_Error()
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(tempPath, "INVALID KEY WITH SPACES=value");

            var result = DotEnvFile.TryParseFiles(tempPath);

            Assert.False(result.IsOk);
            Assert.IsType<DotEnvParseException>(result.Error);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public static Task ParseFileAsync_Missing_Throws()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        return Assert.ThrowsAsync<FileNotFoundException>(() => DotEnvFile.ParseFileAsync(tempPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task ParseFileAsync_Exists_Returns_Doc()
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempPath, "KEY=value", TestContext.Current.CancellationToken);

            var doc = await DotEnvFile.ParseFileAsync(tempPath, TestContext.Current.CancellationToken);

            Assert.Equal("value", doc.Get("KEY"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public static async Task TryParseFileAsync_Missing_Returns_Error()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.env");

        var result = await DotEnvFile.TryParseFileAsync(tempPath, TestContext.Current.CancellationToken);

        Assert.False(result.IsOk);
        Assert.IsType<FileNotFoundException>(result.Error);
    }

    [Fact]
    public static async Task TryParseFileAsync_Exists_Returns_Doc()
    {
        var tempPath = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempPath, "KEY=value", TestContext.Current.CancellationToken);

            var result = await DotEnvFile.TryParseFileAsync(tempPath, TestContext.Current.CancellationToken);

            Assert.True(result.IsOk);
            Assert.Equal("value", result.Doc?.Get("KEY"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public static async Task ParseFilesAsync_Merges_Documents()
    {
        var tempPath1 = Path.GetTempFileName();
        var tempPath2 = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempPath1, "KEY1=value1\nKEY2=value2", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(tempPath2, "KEY2=override\nKEY3=value3", TestContext.Current.CancellationToken);

            var doc = await DotEnvFile.ParseFilesAsync(new[] { tempPath1, tempPath2 }, TestContext.Current.CancellationToken);

            Assert.Equal("value1", doc.Get("KEY1"));
            Assert.Equal("override", doc.Get("KEY2"));
            Assert.Equal("value3", doc.Get("KEY3"));
        }
        finally
        {
            File.Delete(tempPath1);
            File.Delete(tempPath2);
        }
    }

    [Fact]
    public static void ParseStreams_Merges_Documents()
    {
        using var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("KEY1=value1\nKEY2=value2"));
        using var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("KEY2=override\nKEY3=value3"));

        var doc = DotEnvFile.ParseStreams(stream1, stream2);

        Assert.Equal("value1", doc.Get("KEY1"));
        Assert.Equal("override", doc.Get("KEY2"));
        Assert.Equal("value3", doc.Get("KEY3"));
    }

    [Fact]
    public static void DotEnvResult_GetOrThrow_Returns_Doc()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");

        DotEnvResult result = doc;

        var returned = result.GetOrThrow();
        Assert.Same(doc, returned);
    }

    [Fact]
    public static void DotEnvResult_GetOrThrow_With_Error_Throws()
    {
        var ex = new InvalidOperationException("test");
        var result = new DotEnvResult(null, ex);

        Assert.Throws<InvalidOperationException>(() => result.GetOrThrow());
    }
}