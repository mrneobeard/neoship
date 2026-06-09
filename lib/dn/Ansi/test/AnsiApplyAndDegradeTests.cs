using NeoBeard.Colors;

namespace NeoBeard.Tests;

[Collection("AnsiSettings")]
public class AnsiApplyTests : IDisposable
{
    private readonly AnsiSettings originalSettings;

    public AnsiApplyTests()
    {
        this.originalSettings = AnsiSettings.Current;
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Fact]
    public void Apply_With_Funcs_Applies_Multiple_Styles()
    {
        var text = Ansi.Apply("styled", Ansi.Red, Ansi.Bold);
        Assert.Equal("\u001b[1m\u001b[31mstyled\u001b[39m\u001b[22m", text);
    }

    [Fact]
    public void Apply_With_Funcs_Applies_Color_And_Style()
    {
        var text = Ansi.Apply("text", Ansi.Green, Ansi.Underline);
        Assert.Equal("\u001b[4m\u001b[32mtext\u001b[39m\u001b[24m", text);
    }

    [Fact]
    public void Apply_With_Funcs_Applies_Three_Styles()
    {
        var text = Ansi.Apply("text", Ansi.Blue, Ansi.Italic, Ansi.Bold);
        Assert.Equal("\u001b[1m\u001b[3m\u001b[34mtext\u001b[39m\u001b[23m\u001b[22m", text);
    }

    [Fact]
    public void Apply_With_Funcs_Applies_Foreground_And_Background()
    {
        var text = Ansi.Apply("text", Ansi.Yellow, Ansi.BgBlue);
        Assert.Equal("\u001b[44m\u001b[33mtext\u001b[39m\u001b[49m", text);
    }

    [Fact]
    public void Apply_With_Funcs_Applies_Rgb_Color()
    {
        var color = new Rgb(255, 128, 64);
        var text = Ansi.Apply("text", t => Ansi.Rgb(color, t));
        Assert.Equal("\u001b[38;2;255;128;64mtext\u001b[39m", text);
    }

    [Fact]
    public void Apply_With_Funcs_None_Mode_Returns_Plain_Text()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };
        var text = Ansi.Apply("text", Ansi.Red, Ansi.Bold);
        Assert.Equal("text", text);
    }

    [Fact]
    public void Apply_With_Funcs_Empty_Array_Returns_Text()
    {
        var text = Ansi.Apply("text", Array.Empty<Func<string, string>>());
        Assert.Equal("text", text);
    }
}

[Collection("AnsiSettings")]
public class AnsiDegradeColorTests : IDisposable
{
    private const int YellowCode = 33;
    private readonly AnsiSettings originalSettings;
    private readonly Rgb orange24bit = new(255, 165, 0);
    private readonly Rgb orange8bit = new(255, 165, 0);

    public AnsiDegradeColorTests()
    {
        this.originalSettings = AnsiSettings.Current;
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Fact]
    public void DegradeColor_24Bit_Mode_Uses_True_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = colorFunc("orange text");

        Assert.Equal("\u001b[38;2;255;165;0morange text\u001b[39m", text);
    }

    [Fact]
    public void DegradeColor_8Bit_Mode_Uses_256_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.EightBit };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = colorFunc("orange text");

        // Orange (255, 165, 0) converts to index 214 in 256-color palette
        Assert.StartsWith("\u001b[38;5;214m", text);
        Assert.EndsWith("orange text\u001b[39m", text);
    }

    [Fact]
    public void DegradeColor_4Bit_Mode_Uses_Basic_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.FourBit };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = colorFunc("yellow fallback");

        Assert.Equal("\u001b[33myellow fallback\u001b[39m", text);
    }

    [Fact]
    public void DegradeColor_3Bit_Mode_Uses_Basic_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.ThreeBit };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = colorFunc("yellow fallback");

        Assert.Equal("\u001b[33myellow fallback\u001b[39m", text);
    }

    [Fact]
    public void DegradeColor_None_Mode_Returns_Plain_Text()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = colorFunc("plain text");

        Assert.Equal("plain text", text);
    }

    [Fact]
    public void DegradeColor_Can_Be_Used_With_Apply()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var colorFunc = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var text = Ansi.Apply("styled", colorFunc, Ansi.Bold);

        Assert.Equal("\u001b[1m\u001b[38;2;255;165;0mstyled\u001b[39m\u001b[22m", text);
    }

    [Fact]
    public void DegradeColor_Returns_Same_Function_Instance()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var func1 = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);
        var func2 = Ansi.DegradeColor(this.orange24bit, this.orange8bit, YellowCode);

        Assert.Equal(func1("test"), func2("test"));
    }
}

[Collection("AnsiSettings")]
public class AnsiDegradeBgColorTests : IDisposable
{
    private const int YellowBgCode = 43;
    private readonly AnsiSettings originalSettings;
    private readonly Rgb orange24bit = new(255, 165, 0);
    private readonly Rgb orange8bit = new(255, 165, 0);

    public AnsiDegradeBgColorTests()
    {
        this.originalSettings = AnsiSettings.Current;
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Fact]
    public void DegradeBgColor_24Bit_Mode_Uses_True_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var colorFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = colorFunc("orange bg");

        Assert.Equal("\u001b[48;2;255;165;0morange bg\u001b[49m", text);
    }

    [Fact]
    public void DegradeBgColor_8Bit_Mode_Uses_256_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.EightBit };

        var colorFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = colorFunc("orange bg");

        // Orange (255, 165, 0) converts to index 214 in 256-color palette
        Assert.StartsWith("\u001b[48;5;214m", text);
        Assert.EndsWith("orange bg\u001b[49m", text);
    }

    [Fact]
    public void DegradeBgColor_4Bit_Mode_Uses_Basic_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.FourBit };

        var colorFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = colorFunc("yellow bg fallback");

        Assert.Equal("\u001b[43myellow bg fallback\u001b[49m", text);
    }

    [Fact]
    public void DegradeBgColor_3Bit_Mode_Uses_Basic_Color()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.ThreeBit };

        var colorFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = colorFunc("yellow bg fallback");

        Assert.Equal("\u001b[43myellow bg fallback\u001b[49m", text);
    }

    [Fact]
    public void DegradeBgColor_None_Mode_Returns_Plain_Text()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };

        var colorFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = colorFunc("plain text");

        Assert.Equal("plain text", text);
    }

    [Fact]
    public void DegradeBgColor_Can_Be_Used_With_Apply()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var bgFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = Ansi.Apply("styled", bgFunc, Ansi.Bold);

        Assert.Equal("\u001b[1m\u001b[48;2;255;165;0mstyled\u001b[49m\u001b[22m", text);
    }

    [Fact]
    public void DegradeBgColor_Can_Be_Combined_With_DegradeColor()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var fgFunc = Ansi.DegradeColor(new Rgb(0, 0, 255), new Rgb(0, 0, 5), 34);
        var bgFunc = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var text = Ansi.Apply("styled", fgFunc, bgFunc);

        Assert.Equal("\u001b[48;2;255;165;0m\u001b[38;2;0;0;255mstyled\u001b[39m\u001b[49m", text);
    }

    [Fact]
    public void DegradeBgColor_Returns_Same_Function_Instance()
    {
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };

        var func1 = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);
        var func2 = Ansi.DegradeBgColor(this.orange24bit, this.orange8bit, YellowBgCode);

        Assert.Equal(func1("test"), func2("test"));
    }
}

[Collection("AnsiSettings")]
public class AnsiDegradeColorModeTransitionTests : IDisposable
{
    private readonly AnsiSettings originalSettings;

    public AnsiDegradeColorModeTransitionTests()
    {
        this.originalSettings = AnsiSettings.Current;
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Theory]
    [InlineData(AnsiMode.TwentyFourBit, "\u001b[38;2;255;165;0mtext\u001b[39m")]
    [InlineData(AnsiMode.EightBit, "\u001b[38;5;214mtext\u001b[39m")]
    [InlineData(AnsiMode.FourBit, "\u001b[33mtext\u001b[39m")]
    [InlineData(AnsiMode.ThreeBit, "\u001b[33mtext\u001b[39m")]
    [InlineData(AnsiMode.None, "text")]
    public void DegradeColor_Adapts_To_Mode(AnsiMode mode, string expected)
    {
        AnsiSettings.Current = new AnsiSettings { Mode = mode };

        var orange24bit = new Rgb(255, 165, 0);
        var colorFunc = Ansi.DegradeColor(orange24bit, orange24bit, 33);
        var text = colorFunc("text");

        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(AnsiMode.TwentyFourBit, "\u001b[48;2;255;165;0mtext\u001b[49m")]
    [InlineData(AnsiMode.EightBit, "\u001b[48;5;214mtext\u001b[49m")]
    [InlineData(AnsiMode.FourBit, "\u001b[43mtext\u001b[49m")]
    [InlineData(AnsiMode.ThreeBit, "\u001b[43mtext\u001b[49m")]
    [InlineData(AnsiMode.None, "text")]
    public void DegradeBgColor_Adapts_To_Mode(AnsiMode mode, string expected)
    {
        AnsiSettings.Current = new AnsiSettings { Mode = mode };

        var orange24bit = new Rgb(255, 165, 0);
        var bgFunc = Ansi.DegradeBgColor(orange24bit, orange24bit, 43);
        var text = bgFunc("text");

        Assert.Equal(expected, text);
    }
}