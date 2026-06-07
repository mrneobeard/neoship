using System.Text;

namespace NeoBeard.DotEnv.Tests;

public static class DotEnvAdditionalCoverageTests
{
    [Fact]
    public static void Parse_Handles_CrLf_And_Trailing_Key_Without_Value()
    {
        var doc = DotEnvFile.Parse("KEY=value\r\nEMPTY");

        Assert.Equal("value", doc.Get("KEY"));
        Assert.Equal(string.Empty, doc.Get("EMPTY"));
    }

    [Fact]
    public static void Parse_Rejects_Unexpected_Trailing_Content_After_Quoted_Value()
    {
        var exception = Assert.Throws<DotEnvParseException>(() => DotEnvFile.Parse("KEY=\"value\"suffix"));

        Assert.Contains("unexpected character", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public static void ParseStreams_Merges_Later_Streams_Over_Earlier_Streams()
    {
        using var first = new MemoryStream(Encoding.UTF8.GetBytes("A=1\nB=2"));
        using var second = new MemoryStream(Encoding.UTF8.GetBytes("B=override\nC=3"));

        var doc = DotEnvFile.ParseStreams(first, second);

        Assert.Equal("1", doc.Get("A"));
        Assert.Equal("override", doc.Get("B"));
        Assert.Equal("3", doc.Get("C"));
    }

    [Fact]
    public static async Task ParseStreamAsync_Reads_Stream_And_Leaves_Stream_Open()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("KEY=value"));

        var doc = await DotEnvFile.ParseStreamAsync(stream, TestContext.Current.CancellationToken);

        Assert.Equal("value", doc.Get("KEY"));
        Assert.True(stream.CanRead);
    }

    [Fact]
    public static async Task TryParseFileAsync_Returns_Error_For_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), "neobeard-dotenv-" + Guid.NewGuid().ToString("N"));

        var result = await DotEnvFile.TryParseFileAsync(path, TestContext.Current.CancellationToken);

        Assert.False(result.IsOk);
        Assert.IsType<FileNotFoundException>(result.Error);
    }

    [Fact]
    public static async Task ParseFilesAsync_Skips_Optional_Missing_Files_And_Merges()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "KEY=value", TestContext.Current.CancellationToken);

            var doc = await DotEnvFile.ParseFilesAsync(
                new[] { path, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + "?" },
                TestContext.Current.CancellationToken);

            Assert.Equal("value", doc.Get("KEY"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public static async Task TryParseFilesAsync_Returns_Error_For_Required_Missing_File()
    {
        var path = Path.Combine(Path.GetTempPath(), "neobeard-dotenv-" + Guid.NewGuid().ToString("N"));

        var result = await DotEnvFile.TryParseFilesAsync(new[] { path }, TestContext.Current.CancellationToken);

        Assert.False(result.IsOk);
        Assert.IsType<FileNotFoundException>(result.Error);
    }
}