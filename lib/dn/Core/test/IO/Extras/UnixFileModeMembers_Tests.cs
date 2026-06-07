using NeoBeard.IO.Extras;

namespace NeoBeard.IO.Extras.Tests;

public class UnixFileModeMembers_Tests
{
    [Fact]
    public void FromOctal_755_SetsExpectedBits()
    {
        var mode = UnixFileMode.FromOctal(755);

        Assert.True(mode.HasFlag(UnixFileMode.UserRead));
        Assert.True(mode.HasFlag(UnixFileMode.UserWrite));
        Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
        Assert.True(mode.HasFlag(UnixFileMode.GroupRead));
        Assert.False(mode.HasFlag(UnixFileMode.GroupWrite));
        Assert.True(mode.HasFlag(UnixFileMode.GroupExecute));
        Assert.True(mode.HasFlag(UnixFileMode.OtherRead));
        Assert.False(mode.HasFlag(UnixFileMode.OtherWrite));
        Assert.True(mode.HasFlag(UnixFileMode.OtherExecute));
    }

    [Fact]
    public void FromOctal_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => UnixFileMode.FromOctal(1000));
    }
}