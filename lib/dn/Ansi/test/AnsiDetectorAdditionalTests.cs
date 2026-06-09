using NeoBeard;

namespace NeoBeard.Tests;

[Collection("AnsiSettings")]
public sealed class AnsiDetectorAdditionalTests : IDisposable
{
    private readonly string? originalGnomestackTerm;
    private readonly string? originalTerm;

    public AnsiDetectorAdditionalTests()
    {
        this.originalGnomestackTerm = Environment.GetEnvironmentVariable("GNOMESTACK_TERM");
        this.originalTerm = Environment.GetEnvironmentVariable("TERM");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("GNOMESTACK_TERM", this.originalGnomestackTerm);
        Environment.SetEnvironmentVariable("TERM", this.originalTerm);
    }

    [Theory]
    [InlineData("none", AnsiMode.None)]
    [InlineData("no-color", AnsiMode.None)]
    [InlineData("3bit", AnsiMode.ThreeBit)]
    [InlineData("4bit", AnsiMode.FourBit)]
    [InlineData("8bit", AnsiMode.EightBit)]
    [InlineData("24bit", AnsiMode.TwentyFourBit)]
    [InlineData("unexpected", AnsiMode.FourBit)]
    public void DetectMode_Uses_Gnomestack_Term_Override(string value, AnsiMode expected)
    {
        Environment.SetEnvironmentVariable("GNOMESTACK_TERM", value);
        Environment.SetEnvironmentVariable("TERM", null);

        Assert.Equal(expected, AnsiDetector.DetectMode());
    }

    [Fact]
    public void Detect_Sets_Links_For_TwentyFourBit_Mode()
    {
        Environment.SetEnvironmentVariable("GNOMESTACK_TERM", "24bit");

        var settings = AnsiDetector.Detect();

        Assert.Equal(AnsiMode.TwentyFourBit, settings.Mode);
        Assert.True(settings.Links);
    }

    [Fact]
    public void IsTermVariableAnsiCompatible_Uses_Default_And_Custom_Patterns()
    {
        Environment.SetEnvironmentVariable("GNOMESTACK_TERM", null);
        Environment.SetEnvironmentVariable("TERM", "xterm-256color");
        Assert.True(AnsiDetector.IsTermVariableAnsiCompatible());

        Environment.SetEnvironmentVariable("TERM", "custom-terminal");
        Assert.True(AnsiDetector.IsTermVariableAnsiCompatible("custom-terminal"));

        Environment.SetEnvironmentVariable("TERM", "dumb");
        Assert.False(AnsiDetector.IsTermVariableAnsiCompatible());
    }
}