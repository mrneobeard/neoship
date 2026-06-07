using System.Diagnostics;

using NeoBeard.Extras;
using NeoBeard.IO.Extras;

[assembly: CaptureConsole(CaptureOut = true)]

namespace NeoBeard.IO.Tests;

public static class UnixFileSystemTests
{
    [Fact]
    public static void Chown_Works_On_Unix()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            Assert.Skip("This test is only applicable on Unix-like systems.");
            return;
        }

        var uid = Env.Get("UID");
        var sudoUser = Env.Get("SUDO_USER");
        if (sudoUser.IsNullOrWhiteSpace())
        {
            Assert.Skip("This test requires root privileges to run. Skipping test. uid: " + uid + ", sudoUser: " + sudoUser);
            return;
        }

        var tmpDir = Directory.MakeTemp("test-chown");
        using var disposableDir = new DisposableDirectory(tmpDir.FullName);
        File.Chown(disposableDir.DirectoryPath, "nobody", "nogroup", recursive: true);
        var stat = File.Stat(disposableDir.DirectoryPath);
        Assert.Equal("nobody", stat.GetUserName());
        Assert.Equal("nogroup", stat.GetGroupName());
    }

    [Fact]
    public static void MakeTempDirectory_Works()
    {
        var tempDir = Directory.MakeTemp("test");
        using var disposable = new DisposableDirectory(tempDir.FullName);
        Assert.NotNull(tempDir);
        Assert.True(tempDir.Exists, "Temporary directory was not created successfully.");
    }

    [Fact]
    public static void Stat_Gets_File_Info()
    {
        var tempFile = Path.GetTempPath() + Path.GetRandomFileName() + ".txt";
        using var disposableFile = new DisposableFile(tempFile);
        File.WriteAllText(tempFile, "Test content");
        var stat = File.Stat(tempFile);
        Assert.True(stat.IsFile, "Stat should indicate a file.");
        Assert.True(stat.Mode > 0, "File mode should be valid.");
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(-1, stat.UserId);
            Assert.Equal(-1, stat.GroupId);
        }
        else
        {
            Assert.True(stat.UserId > -1, "User ID should be valid.");
            Assert.True(stat.GroupId > -1, "Group ID should be valid.");
        }
    }
}