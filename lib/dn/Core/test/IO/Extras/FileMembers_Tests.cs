using System.Runtime.Versioning;
using System.Text;

using NeoBeard.IO.Extras;

namespace NeoBeard.IO.Extras.Tests;

public class FileMembers_Tests
{
    [Fact]
    public async Task WriteTextAsync_AndAppend_WritesExpectedContent()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await File.WriteTextAsync(path, "one".AsMemory(), cancellationToken);
            File.Append(path, Encoding.UTF8.GetBytes("two"));

            var text = await File.ReadAllTextAsync(path, cancellationToken);

            Assert.Equal("onetwo", text);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Stat_OnFile_ReturnsFileMetadata()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            File.WriteAllText(path, "value");

            var stat = File.Stat(path);

            Assert.True(stat.IsFile);
            Assert.False(stat.IsDirectory);
            Assert.Equal(path, stat.Path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("windows")]
    public void LStat_OnUnix_ReturnsFileMetadata()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            Assert.Skip("LStat test is only valid on Unix-like platforms.");
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            File.WriteAllText(path, "value");

            var stat = File.LStat(path);

            Assert.True(stat.IsFile);
            Assert.Equal(path, stat.Path);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}