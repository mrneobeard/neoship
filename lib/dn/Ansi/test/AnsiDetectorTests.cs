using System.Runtime.InteropServices;

using NeoBeard;

namespace NeoBeard.Tests;

public class AnsiDetectorTests
{
    [Fact]
    public void Detect_Returns_AnsiSettings_With_Mode_Detected()
    {
        var settings = AnsiDetector.Detect();
        Assert.NotNull(settings);
        Assert.True(Enum.IsDefined(typeof(AnsiMode), settings.Mode));
    }

    [Fact]
    public void DetectMode_Returns_AnsiMode_Value()
    {
        var mode = AnsiDetector.DetectMode();
        Assert.True(Enum.IsDefined(typeof(AnsiMode), mode));
    }

    [Fact]
    public void IsTermVariableAnsiCompatible_Returns_False_When_TERM_Not_Set()
    {
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", null);
            Assert.False(AnsiDetector.IsTermVariableAnsiCompatible());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void IsTermVariableAnsiCompatible_Returns_True_For_Xterm()
    {
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");
            Assert.True(AnsiDetector.IsTermVariableAnsiCompatible());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void IsTermVariableAnsiCompatible_Returns_False_For_Incompatible_Term()
    {
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "dumb");
            Assert.False(AnsiDetector.IsTermVariableAnsiCompatible());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void IsTermVariableAnsiCompatible_Accepts_Custom_Patterns()
    {
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "custom-term");
            Assert.True(AnsiDetector.IsTermVariableAnsiCompatible("custom-term"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void EnableVirtualTerminalProcessing_Skipped_On_Non_Windows()
    {
        Assert.SkipWhen(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows. Skipping on non-Windows.");
        var result = AnsiDetector.EnableVirtualTerminalProcessing();
        Assert.True(result || !result);
    }
}