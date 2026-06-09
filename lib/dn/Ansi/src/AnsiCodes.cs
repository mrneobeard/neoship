using NeoBeard.Colors;

namespace NeoBeard;

/// <summary>
/// Provides ANSI escape codes as string constants and RGB conversion methods for efficient styling.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Apply multiple styles at once using Apply
/// var text = Ansi.Apply("styled text", AnsiCodes.Red, AnsiCodes.Bold);
/// Console.WriteLine(text);
/// </code>
/// </example>
/// </remarks>
public static class AnsiCodes
{
    // Reset codes

    /// <summary>
    /// Resets all attributes to default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("reset text", AnsiCodes.Reset);
    /// Assert.Equal("\u001b[0mreset text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Reset = "0";

    /// <summary>
    /// Resets foreground color to default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("default fg", AnsiCodes.DefaultForeground);
    /// </code>
    /// </example>
    /// </remarks>
    public const string DefaultForeground = "39";

    /// <summary>
    /// Resets background color to default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("default bg", AnsiCodes.DefaultBackground);
    /// </code>
    /// </example>
    /// </remarks>
    public const string DefaultBackground = "49";

    // Standard foreground colors (3-bit/4-bit)

    /// <summary>
    /// Black foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("black text", AnsiCodes.Black);
    /// Assert.Equal("\u001b[30mblack text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Black = "30";

    /// <summary>
    /// Red foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("red text", AnsiCodes.Red);
    /// Assert.Equal("\u001b[31mred text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Red = "31";

    /// <summary>
    /// Green foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("green text", AnsiCodes.Green);
    /// Assert.Equal("\u001b[32mgreen text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Green = "32";

    /// <summary>
    /// Yellow foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("yellow text", AnsiCodes.Yellow);
    /// Assert.Equal("\u001b[33myellow text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Yellow = "33";

    /// <summary>
    /// Blue foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("blue text", AnsiCodes.Blue);
    /// Assert.Equal("\u001b[34mblue text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Blue = "34";

    /// <summary>
    /// Magenta foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("magenta text", AnsiCodes.Magenta);
    /// Assert.Equal("\u001b[35mmagenta text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Magenta = "35";

    /// <summary>
    /// Cyan foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("cyan text", AnsiCodes.Cyan);
    /// Assert.Equal("\u001b[36mcyan text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Cyan = "36";

    /// <summary>
    /// White foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("white text", AnsiCodes.White);
    /// Assert.Equal("\u001b[37mwhite text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string White = "37";

    // Bright foreground colors

    /// <summary>
    /// Bright black foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright black text", AnsiCodes.BrightBlack);
    /// Assert.Equal("\u001b[90mbright black text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightBlack = "90";

    /// <summary>
    /// Bright red foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright red text", AnsiCodes.BrightRed);
    /// Assert.Equal("\u001b[91mbright red text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightRed = "91";

    /// <summary>
    /// Bright green foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright green text", AnsiCodes.BrightGreen);
    /// Assert.Equal("\u001b[92mbright green text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightGreen = "92";

    /// <summary>
    /// Bright yellow foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright yellow text", AnsiCodes.BrightYellow);
    /// Assert.Equal("\u001b[93mbright yellow text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightYellow = "93";

    /// <summary>
    /// Bright blue foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright blue text", AnsiCodes.BrightBlue);
    /// Assert.Equal("\u001b[94mbright blue text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightBlue = "94";

    /// <summary>
    /// Bright magenta foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright magenta text", AnsiCodes.BrightMagenta);
    /// Assert.Equal("\u001b[95mbright magenta text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightMagenta = "95";

    /// <summary>
    /// Bright cyan foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright cyan text", AnsiCodes.BrightCyan);
    /// Assert.Equal("\u001b[96mbright cyan text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightCyan = "96";

    /// <summary>
    /// Bright white foreground color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright white text", AnsiCodes.BrightWhite);
    /// Assert.Equal("\u001b[97mbright white text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BrightWhite = "97";

    // Standard background colors

    /// <summary>
    /// Black background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("black bg", AnsiCodes.BgBlack);
    /// Assert.Equal("\u001b[40mblack bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBlack = "40";

    /// <summary>
    /// Red background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("red bg", AnsiCodes.BgRed);
    /// Assert.Equal("\u001b[41mred bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgRed = "41";

    /// <summary>
    /// Green background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("green bg", AnsiCodes.BgGreen);
    /// Assert.Equal("\u001b[42mgreen bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgGreen = "42";

    /// <summary>
    /// Yellow background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("yellow bg", AnsiCodes.BgYellow);
    /// Assert.Equal("\u001b[43myellow bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgYellow = "43";

    /// <summary>
    /// Blue background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("blue bg", AnsiCodes.BgBlue);
    /// Assert.Equal("\u001b[44mblue bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBlue = "44";

    /// <summary>
    /// Magenta background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("magenta bg", AnsiCodes.BgMagenta);
    /// Assert.Equal("\u001b[45mmagenta bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgMagenta = "45";

    /// <summary>
    /// Cyan background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("cyan bg", AnsiCodes.BgCyan);
    /// Assert.Equal("\u001b[46mcyan bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgCyan = "46";

    /// <summary>
    /// White background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("white bg", AnsiCodes.BgWhite);
    /// Assert.Equal("\u001b[47mwhite bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgWhite = "47";

    // Bright background colors

    /// <summary>
    /// Bright black background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright black bg", AnsiCodes.BgBrightBlack);
    /// Assert.Equal("\u001b[100mbright black bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightBlack = "100";

    /// <summary>
    /// Bright red background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright red bg", AnsiCodes.BgBrightRed);
    /// Assert.Equal("\u001b[101mbright red bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightRed = "101";

    /// <summary>
    /// Bright green background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright green bg", AnsiCodes.BgBrightGreen);
    /// Assert.Equal("\u001b[102mbright green bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightGreen = "102";

    /// <summary>
    /// Bright yellow background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright yellow bg", AnsiCodes.BgBrightYellow);
    /// Assert.Equal("\u001b[103mbright yellow bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightYellow = "103";

    /// <summary>
    /// Bright blue background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright blue bg", AnsiCodes.BgBrightBlue);
    /// Assert.Equal("\u001b[104mbright blue bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightBlue = "104";

    /// <summary>
    /// Bright magenta background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright magenta bg", AnsiCodes.BgBrightMagenta);
    /// Assert.Equal("\u001b[105mbright magenta bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightMagenta = "105";

    /// <summary>
    /// Bright cyan background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright cyan bg", AnsiCodes.BgBrightCyan);
    /// Assert.Equal("\u001b[106mbright cyan bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightCyan = "106";

    /// <summary>
    /// Bright white background color code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bright white bg", AnsiCodes.BgBrightWhite);
    /// Assert.Equal("\u001b[107mbright white bg\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string BgBrightWhite = "107";

    // Text styles

    /// <summary>
    /// Bold text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("bold text", AnsiCodes.Bold);
    /// Assert.Equal("\u001b[1mbold text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Bold = "1";

    /// <summary>
    /// Dim text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("dim text", AnsiCodes.Dim);
    /// Assert.Equal("\u001b[2mdim text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Dim = "2";

    /// <summary>
    /// Italic text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("italic text", AnsiCodes.Italic);
    /// Assert.Equal("\u001b[3mitalic text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Italic = "3";

    /// <summary>
    /// Underline text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("underline text", AnsiCodes.Underline);
    /// Assert.Equal("\u001b[4munderline text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Underline = "4";

    /// <summary>
    /// Blink text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("blink text", AnsiCodes.Blink);
    /// Assert.Equal("\u001b[5mblink text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Blink = "5";

    /// <summary>
    /// Inverse text style code (swaps foreground and background).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("inverse text", AnsiCodes.Inverse);
    /// Assert.Equal("\u001b[7minverse text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Inverse = "7";

    /// <summary>
    /// Hidden text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("hidden text", AnsiCodes.Hidden);
    /// Assert.Equal("\u001b[8mhidden text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Hidden = "8";

    /// <summary>
    /// Strikethrough text style code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("strikethrough text", AnsiCodes.Strikethrough);
    /// Assert.Equal("\u001b[9mstrikethrough text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public const string Strikethrough = "9";

    // Style reset codes

    /// <summary>
    /// Resets bold or dim style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetBoldDim);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetBoldDim = "22";

    /// <summary>
    /// Resets italic style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetItalic);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetItalic = "23";

    /// <summary>
    /// Resets underline style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetUnderline);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetUnderline = "24";

    /// <summary>
    /// Resets blink style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetBlink);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetBlink = "25";

    /// <summary>
    /// Resets inverse style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetInverse);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetInverse = "27";

    /// <summary>
    /// Resets hidden style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetHidden);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetHidden = "28";

    /// <summary>
    /// Resets strikethrough style.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("normal text", AnsiCodes.ResetStrikethrough);
    /// </code>
    /// </example>
    /// </remarks>
    public const string ResetStrikethrough = "29";

    // RGB color methods

    /// <summary>
    /// Returns the ANSI code for 24-bit true color foreground.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The ANSI code string for 24-bit foreground color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.Rgb(255, 128, 64);
    /// var text = Ansi.Apply("colored text", code);
    /// Assert.Equal("\u001b[38;2;255;128;64mcolored text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb(byte r, byte g, byte b) => $"38;2;{r};{g};{b}";

    /// <summary>
    /// Returns the ANSI code for 24-bit true color foreground from an Rgb struct.
    /// </summary>
    /// <param name="color">The RGB color.</param>
    /// <returns>The ANSI code string for 24-bit foreground color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var code = AnsiCodes.Rgb(color);
    /// var text = Ansi.Apply("colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb(Rgb color) => $"38;2;{color.R};{color.G};{color.B}";

    /// <summary>
    /// Returns the ANSI code for 8-bit (256-color) foreground.
    /// </summary>
    /// <param name="colorIndex">The 8-bit color index (0-255).</param>
    /// <returns>The ANSI code string for 8-bit foreground color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.Rgb8(196);
    /// var text = Ansi.Apply("colored text", code);
    /// Assert.Equal("\u001b[38;5;196mcolored text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb8(uint colorIndex) => $"38;5;{colorIndex}";

    /// <summary>
    /// Returns the ANSI code for 8-bit foreground converted from RGB.
    /// </summary>
    /// <param name="color">The RGB color to convert to 8-bit palette.</param>
    /// <returns>The ANSI code string for 8-bit foreground color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var code = AnsiCodes.Rgb8(color);
    /// var text = Ansi.Apply("colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb8(Rgb color) => $"38;5;{color.To256Color()}";

    /// <summary>
    /// Returns the ANSI code for 8-bit foreground converted from RGB components.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The ANSI code string for 8-bit foreground color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.Rgb8(255, 128, 64);
    /// var text = Ansi.Apply("colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb8(byte r, byte g, byte b) => $"38;5;{new Rgb(r, g, b).To256Color()}";

    // Background RGB methods

    /// <summary>
    /// Returns the ANSI code for 24-bit true color background.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The ANSI code string for 24-bit background color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.BgRgb(255, 128, 64);
    /// var text = Ansi.Apply("bg colored text", code);
    /// Assert.Equal("\u001b[48;2;255;128;64mbg colored text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb(byte r, byte g, byte b) => $"48;2;{r};{g};{b}";

    /// <summary>
    /// Returns the ANSI code for 24-bit true color background from an Rgb struct.
    /// </summary>
    /// <param name="color">The RGB color.</param>
    /// <returns>The ANSI code string for 24-bit background color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var code = AnsiCodes.BgRgb(color);
    /// var text = Ansi.Apply("bg colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb(Rgb color) => $"48;2;{color.R};{color.G};{color.B}";

    /// <summary>
    /// Returns the ANSI code for 8-bit (256-color) background.
    /// </summary>
    /// <param name="colorIndex">The 8-bit color index (0-255).</param>
    /// <returns>The ANSI code string for 8-bit background color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.BgRgb8(196);
    /// var text = Ansi.Apply("bg colored text", code);
    /// Assert.Equal("\u001b[48;5;196mbg colored text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb8(uint colorIndex) => $"48;5;{colorIndex}";

    /// <summary>
    /// Returns the ANSI code for 8-bit background converted from RGB.
    /// </summary>
    /// <param name="color">The RGB color to convert to 8-bit palette.</param>
    /// <returns>The ANSI code string for 8-bit background color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var code = AnsiCodes.BgRgb8(color);
    /// var text = Ansi.Apply("bg colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb8(Rgb color) => $"48;5;{color.To256Color()}";

    /// <summary>
    /// Returns the ANSI code for 8-bit background converted from RGB components.
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <returns>The ANSI code string for 8-bit background color.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = AnsiCodes.BgRgb8(255, 128, 64);
    /// var text = Ansi.Apply("bg colored text", code);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb8(byte r, byte g, byte b) => $"48;5;{new Rgb(r, g, b).To256Color()}";
}