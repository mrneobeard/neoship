using NeoBeard;
using NeoBeard.Colors;

namespace NeoBeard.Tests;

[Collection("AnsiSettings")]
public class AnsiTests : IDisposable
{
    private readonly AnsiSettings originalSettings;

    public AnsiTests()
    {
        this.originalSettings = AnsiSettings.Current;
        AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.TwentyFourBit };
    }

    public void Dispose()
    {
        AnsiSettings.Current = this.originalSettings;
    }

    [Fact]
    public void Red_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Red("This is red text");
        TestContext.Current.TestOutputHelper!.WriteLine(formatted);
        Assert.Equal("\u001b[31mThis is red text\u001b[39m", formatted);
    }

    [Fact]
    public void Green_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Green("This is green text");
        Assert.Equal("\u001b[32mThis is green text\u001b[39m", formatted);
    }

    [Fact]
    public void Blue_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Blue("This is blue text");
        Assert.Equal("\u001b[34mThis is blue text\u001b[39m", formatted);
    }

    [Fact]
    public void White_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.White("white");
        Assert.Equal("\u001b[37mwhite\u001b[39m", formatted);
    }

    [Fact]
    public void Black_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Black("black");
        Assert.Equal("\u001b[30mblack\u001b[39m", formatted);
    }

    [Fact]
    public void Yellow_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Yellow("yellow");
        Assert.Equal("\u001b[33myellow\u001b[39m", formatted);
    }

    [Fact]
    public void Magenta_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Magenta("magenta");
        Assert.Equal("\u001b[35mmagenta\u001b[39m", formatted);
    }

    [Fact]
    public void Cyan_Should_Apply_Ansi_Format()
    {
        var formatted = Ansi.Cyan("cyan");
        Assert.Equal("\u001b[36mcyan\u001b[39m", formatted);
    }

    [Fact]
    public void BrightColors_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[90mtext\u001b[39m", Ansi.BrightBlack("text"));
        Assert.Equal("\u001b[91mtext\u001b[39m", Ansi.BrightRed("text"));
        Assert.Equal("\u001b[92mtext\u001b[39m", Ansi.BrightGreen("text"));
        Assert.Equal("\u001b[93mtext\u001b[39m", Ansi.BrightYellow("text"));
        Assert.Equal("\u001b[94mtext\u001b[39m", Ansi.BrightBlue("text"));
        Assert.Equal("\u001b[95mtext\u001b[39m", Ansi.BrightMagenta("text"));
        Assert.Equal("\u001b[96mtext\u001b[39m", Ansi.BrightCyan("text"));
        Assert.Equal("\u001b[97mtext\u001b[39m", Ansi.BrightWhite("text"));
    }

    [Fact]
    public void BackgroundColors_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[47mtext\u001b[49m", Ansi.BgWhite("text"));
        Assert.Equal("\u001b[40mtext\u001b[49m", Ansi.BgBlack("text"));
        Assert.Equal("\u001b[41mtext\u001b[49m", Ansi.BgRed("text"));
        Assert.Equal("\u001b[42mtext\u001b[49m", Ansi.BgGreen("text"));
        Assert.Equal("\u001b[43mtext\u001b[49m", Ansi.BgYellow("text"));
        Assert.Equal("\u001b[44mtext\u001b[49m", Ansi.BgBlue("text"));
        Assert.Equal("\u001b[45mtext\u001b[49m", Ansi.BgMagenta("text"));
        Assert.Equal("\u001b[46mtext\u001b[49m", Ansi.BgCyan("text"));
    }

    [Fact]
    public void BrightBackgroundColors_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[100mtext\u001b[49m", Ansi.BgBrightBlack("text"));
        Assert.Equal("\u001b[101mtext\u001b[49m", Ansi.BgBrightRed("text"));
        Assert.Equal("\u001b[102mtext\u001b[49m", Ansi.BgBrightGreen("text"));
        Assert.Equal("\u001b[103mtext\u001b[49m", Ansi.BgBrightYellow("text"));
        Assert.Equal("\u001b[104mtext\u001b[49m", Ansi.BgBrightBlue("text"));
        Assert.Equal("\u001b[105mtext\u001b[49m", Ansi.BgBrightMagenta("text"));
        Assert.Equal("\u001b[106mtext\u001b[49m", Ansi.BgBrightCyan("text"));
        Assert.Equal("\u001b[107mtext\u001b[49m", Ansi.BgBrightWhite("text"));
    }

    [Fact]
    public void StyleMethods_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[1mbold\u001b[22m", Ansi.Bold("bold"));
        Assert.Equal("\u001b[2mdim\u001b[22m", Ansi.Dim("dim"));
        Assert.Equal("\u001b[3mitalic\u001b[23m", Ansi.Italic("italic"));
        Assert.Equal("\u001b[4munderline\u001b[24m", Ansi.Underline("underline"));
        Assert.Equal("\u001b[9mstrike\u001b[29m", Ansi.Strikethrough("strike"));
        Assert.Equal("\u001b[5mblink\u001b[25m", Ansi.Blink("blink"));
        Assert.Equal("\u001b[7minverse\u001b[27m", Ansi.Inverse("inverse"));
        Assert.Equal("\u001b[8mhidden\u001b[28m", Ansi.Hidden("hidden"));
    }

    [Fact]
    public void Reset_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[0mreset\u001b[0m", Ansi.Reset("reset"));
        Assert.Equal("\u001b[0m", Ansi.Reset());
    }

    [Fact]
    public void Emoji_Should_Emit_Emoji()
    {
        // This test assumes AnsiSettings.Current.Mode != AnsiMode.None
        Assert.Equal("😀", Ansi.Emoji("😀"));
    }

    [Fact]
    public void Link_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b]8;;link\u001b\\link\u001b]8;;\u001b\\", Ansi.Link("link"));
    }

    [Fact]
    public void Rgb8_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[38;5;123mtext\u001b[39m", Ansi.Rgb8(123, "text"));
    }

    [Fact]
    public void Rgb_Should_Apply_Ansi_Format_With_UInt()
    {
        Assert.Equal("\u001b[38;2;18;52;86mtext\u001b[39m", Ansi.Rgb(0x123456, "text"));
    }

    [Fact]
    public void BgRgb8_Should_Apply_Ansi_Format()
    {
        Assert.Equal("\u001b[48;5;123mtext\u001b[49m", Ansi.BgRgb8(123, "text"));
    }

    [Fact]
    public void BgRgb_Should_Apply_Ansi_Format_With_UInt()
    {
        Assert.Equal("\u001b[48;2;18;52;86mtext\u001b[49m", Ansi.BgRgb(0x123456, "text"));
    }

    [Fact]
    public void Rgb_Should_Apply_Ansi_Format_With_Rgb_Struct()
    {
        var color = new Rgb(255, 128, 64);
        Assert.Equal("\u001b[38;2;255;128;64mtext\u001b[39m", Ansi.Rgb(color, "text"));
    }

    [Fact]
    public void BgRgb_Should_Apply_Ansi_Format_With_Rgb_Struct()
    {
        var color = new Rgb(255, 128, 64);
        Assert.Equal("\u001b[48;2;255;128;64mtext\u001b[49m", Ansi.BgRgb(color, "text"));
    }

    [Collection("AnsiSettings")]
    public class AnsiModeNoneTests : IDisposable
    {
        private readonly AnsiSettings originalSettings;

        public AnsiModeNoneTests()
        {
            this.originalSettings = AnsiSettings.Current;
            AnsiSettings.Current = new AnsiSettings { Mode = AnsiMode.None };
        }

        public void Dispose()
        {
            AnsiSettings.Current = this.originalSettings;
        }

        [Fact]
        public void Red_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Red("text"));
        }

        [Fact]
        public void Green_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Green("text"));
        }

        [Fact]
        public void Blue_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Blue("text"));
        }

        [Fact]
        public void Bold_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Bold("text"));
        }

        [Fact]
        public void Italic_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Italic("text"));
        }

        [Fact]
        public void Underline_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Underline("text"));
        }

        [Fact]
        public void BgRed_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.BgRed("text"));
        }

        [Fact]
        public void Rgb_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Rgb(0x123456, "text"));
        }

        [Fact]
        public void Rgb_With_Struct_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            var color = new Rgb(255, 128, 64);
            Assert.Equal("text", Ansi.Rgb(color, "text"));
        }

        [Fact]
        public void BgRgb_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.BgRgb(0x123456, "text"));
        }

        [Fact]
        public void BgRgb_With_Struct_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            var color = new Rgb(255, 128, 64);
            Assert.Equal("text", Ansi.BgRgb(color, "text"));
        }

        [Fact]
        public void Rgb8_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Rgb8(123, "text"));
        }

        [Fact]
        public void BgRgb8_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.BgRgb8(123, "text"));
        }

        [Fact]
        public void Reset_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("text", Ansi.Reset("text"));
        }

        [Fact]
        public void Reset_No_Args_Should_Return_Empty_When_Mode_Is_None()
        {
            Assert.Equal(string.Empty, Ansi.Reset());
        }

        [Fact]
        public void Emoji_Should_Return_Empty_When_Mode_Is_None()
        {
            Assert.Equal(string.Empty, Ansi.Emoji("😀"));
        }

        [Fact]
        public void Link_Should_Return_Plain_Text_When_Mode_Is_None()
        {
            Assert.Equal("link", Ansi.Link("link"));
        }
    }
}