namespace NeoBeard.Tests;

[Collection("AnsiSettings")]
public class AnsiCodesTests : IDisposable
{
    private readonly AnsiSettings originalSettings;

    public AnsiCodesTests()
    {
        this.originalSettings = AnsiSettings.Current;
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Fact]
    public void Reset_Code_Should_Be_Zero()
    {
        Assert.Equal("0", AnsiCodes.Reset);
    }

    [Fact]
    public void DefaultForeground_Code_Should_Be_39()
    {
        Assert.Equal("39", AnsiCodes.DefaultForeground);
    }

    [Fact]
    public void DefaultBackground_Code_Should_Be_49()
    {
        Assert.Equal("49", AnsiCodes.DefaultBackground);
    }

    [Fact]
    public void StandardForegroundColors_Should_Have_Correct_Codes()
    {
        Assert.Equal("30", AnsiCodes.Black);
        Assert.Equal("31", AnsiCodes.Red);
        Assert.Equal("32", AnsiCodes.Green);
        Assert.Equal("33", AnsiCodes.Yellow);
        Assert.Equal("34", AnsiCodes.Blue);
        Assert.Equal("35", AnsiCodes.Magenta);
        Assert.Equal("36", AnsiCodes.Cyan);
        Assert.Equal("37", AnsiCodes.White);
    }

    [Fact]
    public void BrightForegroundColors_Should_Have_Correct_Codes()
    {
        Assert.Equal("90", AnsiCodes.BrightBlack);
        Assert.Equal("91", AnsiCodes.BrightRed);
        Assert.Equal("92", AnsiCodes.BrightGreen);
        Assert.Equal("93", AnsiCodes.BrightYellow);
        Assert.Equal("94", AnsiCodes.BrightBlue);
        Assert.Equal("95", AnsiCodes.BrightMagenta);
        Assert.Equal("96", AnsiCodes.BrightCyan);
        Assert.Equal("97", AnsiCodes.BrightWhite);
    }

    [Fact]
    public void StandardBackgroundColors_Should_Have_Correct_Codes()
    {
        Assert.Equal("40", AnsiCodes.BgBlack);
        Assert.Equal("41", AnsiCodes.BgRed);
        Assert.Equal("42", AnsiCodes.BgGreen);
        Assert.Equal("43", AnsiCodes.BgYellow);
        Assert.Equal("44", AnsiCodes.BgBlue);
        Assert.Equal("45", AnsiCodes.BgMagenta);
        Assert.Equal("46", AnsiCodes.BgCyan);
        Assert.Equal("47", AnsiCodes.BgWhite);
    }

    [Fact]
    public void BrightBackgroundColors_Should_Have_Correct_Codes()
    {
        Assert.Equal("100", AnsiCodes.BgBrightBlack);
        Assert.Equal("101", AnsiCodes.BgBrightRed);
        Assert.Equal("102", AnsiCodes.BgBrightGreen);
        Assert.Equal("103", AnsiCodes.BgBrightYellow);
        Assert.Equal("104", AnsiCodes.BgBrightBlue);
        Assert.Equal("105", AnsiCodes.BgBrightMagenta);
        Assert.Equal("106", AnsiCodes.BgBrightCyan);
        Assert.Equal("107", AnsiCodes.BgBrightWhite);
    }

    [Fact]
    public void StyleCodes_Should_Have_Correct_Codes()
    {
        Assert.Equal("1", AnsiCodes.Bold);
        Assert.Equal("2", AnsiCodes.Dim);
        Assert.Equal("3", AnsiCodes.Italic);
        Assert.Equal("4", AnsiCodes.Underline);
        Assert.Equal("5", AnsiCodes.Blink);
        Assert.Equal("7", AnsiCodes.Inverse);
        Assert.Equal("8", AnsiCodes.Hidden);
        Assert.Equal("9", AnsiCodes.Strikethrough);
    }

    [Fact]
    public void StyleResetCodes_Should_Have_Correct_Codes()
    {
        Assert.Equal("22", AnsiCodes.ResetBoldDim);
        Assert.Equal("23", AnsiCodes.ResetItalic);
        Assert.Equal("24", AnsiCodes.ResetUnderline);
        Assert.Equal("25", AnsiCodes.ResetBlink);
        Assert.Equal("27", AnsiCodes.ResetInverse);
        Assert.Equal("28", AnsiCodes.ResetHidden);
        Assert.Equal("29", AnsiCodes.ResetStrikethrough);
    }

    [Fact]
    public void Rgb_With_Bytes_Returns_Correct_Code()
    {
        var code = AnsiCodes.Rgb(255, 128, 64);
        Assert.Equal("38;2;255;128;64", code);
    }

    [Fact]
    public void Rgb_With_RgbStruct_Returns_Correct_Code()
    {
        var color = new NeoBeard.Colors.Rgb(255, 128, 64);
        var code = AnsiCodes.Rgb(color);
        Assert.Equal("38;2;255;128;64", code);
    }

    [Fact]
    public void Rgb8_With_Index_Returns_Correct_Code()
    {
        var code = AnsiCodes.Rgb8(196);
        Assert.Equal("38;5;196", code);
    }

    [Fact]
    public void Rgb8_With_RgbStruct_Returns_Valid_Code()
    {
        var color = new NeoBeard.Colors.Rgb(255, 0, 0);
        var code = AnsiCodes.Rgb8(color);
        Assert.StartsWith("38;5;", code);
    }

    [Fact]
    public void Rgb8_With_Bytes_Returns_Valid_Code()
    {
        var code = AnsiCodes.Rgb8(255, 0, 0);
        Assert.StartsWith("38;5;", code);
    }

    [Fact]
    public void BgRgb_With_Bytes_Returns_Correct_Code()
    {
        var code = AnsiCodes.BgRgb(255, 128, 64);
        Assert.Equal("48;2;255;128;64", code);
    }

    [Fact]
    public void BgRgb_With_RgbStruct_Returns_Correct_Code()
    {
        var color = new NeoBeard.Colors.Rgb(255, 128, 64);
        var code = AnsiCodes.BgRgb(color);
        Assert.Equal("48;2;255;128;64", code);
    }

    [Fact]
    public void BgRgb8_With_Index_Returns_Correct_Code()
    {
        var code = AnsiCodes.BgRgb8(196);
        Assert.Equal("48;5;196", code);
    }

    [Fact]
    public void BgRgb8_With_RgbStruct_Returns_Valid_Code()
    {
        var color = new NeoBeard.Colors.Rgb(255, 0, 0);
        var code = AnsiCodes.BgRgb8(color);
        Assert.StartsWith("48;5;", code);
    }

    [Fact]
    public void BgRgb8_With_Bytes_Returns_Valid_Code()
    {
        var code = AnsiCodes.BgRgb8(255, 0, 0);
        Assert.StartsWith("48;5;", code);
    }

    [Fact]
    public void Apply_With_Multiple_Codes_Combines_Them()
    {
        var text = Ansi.Apply("test", AnsiCodes.Red, AnsiCodes.Bold);
        Assert.Equal("\u001b[31;1mtest\u001b[0m", text);
    }

    [Fact]
    public void Apply_With_Rgb_And_Style_Works()
    {
        var rgbCode = AnsiCodes.Rgb(255, 128, 64);
        var text = Ansi.Apply("test", rgbCode, AnsiCodes.Bold);
        Assert.Equal("\u001b[38;2;255;128;64;1mtest\u001b[0m", text);
    }

    [Fact]
    public void Apply_With_None_Mode_Returns_Plain_Text()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };
        var text = Ansi.Apply("test", AnsiCodes.Red, AnsiCodes.Bold);
        Assert.Equal("test", text);
    }
}