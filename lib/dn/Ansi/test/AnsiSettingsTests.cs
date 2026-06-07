using NeoBeard;

namespace NeoBeard.Tests;

public class AnsiSettingsTests
{
    [Fact]
    public void Current_Getter_Returns_Detected_Settings()
    {
        var settings = AnsiSettings.Current;
        Assert.NotNull(settings);
        Assert.True(Enum.IsDefined(typeof(AnsiMode), settings.Mode));
    }

    [Fact]
    public void Current_Setter_Allows_Overriding()
    {
        var originalSettings = AnsiSettings.Current;
        try
        {
            var customSettings = new AnsiSettings { Mode = AnsiMode.None, Links = false };
            AnsiSettings.Current = customSettings;

            Assert.Same(customSettings, AnsiSettings.Current);
            Assert.Equal(AnsiMode.None, AnsiSettings.Current.Mode);
            Assert.False(AnsiSettings.Current.Links);
        }
        finally
        {
            AnsiSettings.Current = originalSettings;
        }
    }

    [Fact]
    public void Mode_Default_Is_Auto()
    {
        var settings = new AnsiSettings();
        Assert.Equal(AnsiMode.Auto, settings.Mode);
    }

    [Fact]
    public void Links_Default_Is_True()
    {
        var settings = new AnsiSettings();
        Assert.True(settings.Links);
    }
}