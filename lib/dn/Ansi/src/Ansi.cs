using System.Security.Cryptography.X509Certificates;

using NeoBeard.Colors;

namespace NeoBeard;

/// <summary>
/// Provides static methods for applying ANSI escape codes to text for terminal coloring and styling.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Console.WriteLine(Ansi.Red("Error: ") + "Something went wrong");
/// Console.WriteLine(Ansi.Bold("Important notice"));
/// Console.WriteLine(Ansi.Green("Success!"));
/// </code>
/// </example>
/// </remarks>
public static class Ansi
{
    /// <summary>
    /// Returns the emoji string only if the current ANSI mode is not None.
    /// </summary>
    /// <param name="emojii">The emoji unicode string.</param>
    /// <returns>The emoji string if mode is not None; otherwise, an empty string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var emoji = Ansi.Emoji("😀");
    /// // Returns "😀" when AnsiSettings.Current.Mode != AnsiMode.None
    /// // Returns "" when AnsiSettings.Current.Mode == AnsiMode.None
    /// </code>
    /// </example>
    /// </remarks>
    public static string Emoji(string emojii)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return string.Empty;

        return emojii;
    }

    /// <summary>
    /// Applies the specified ANSI codes to the given text. If the current ANSI mode is None, returns the original text without modification.
    /// </summary>
    /// <param name="text">The text to which ANSI codes will be applied.</param>
    /// <param name="codes">One or more ANSI code strings (e.g., "31" for red, "1" for bold).</param>
    /// <returns>The text wrapped with ANSI escape codes if mode is not None; otherwise, the original text.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("red bold text", "31", "1");
    /// Assert.Equal("\u001b[31;1mred bold text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Apply(string text, params string[] codes)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        var startCodes = string.Join(";", codes);
        return $"\u001b[{startCodes}m{text}\u001b[0m";
    }

    /// <summary>
    /// Applies the specified ANSI code functions to the given text. If the current ANSI mode is None, returns the original text without modification.
    /// </summary>
    /// <param name="text">The text to which ANSI code functions will be applied.</param>
    /// <param name="codeFuncs">One or more functions that take a string and return a string with ANSI codes applied.</param>
    /// <returns>
    /// The text with the specified ANSI code functions applied, or the original text if mode is None.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Apply("red bold text", Ansi.Red, Ansi.Bold);
    /// Assert.Equal("\u001b[1m\u001b[31mred bold text\u001b[39m\u001b[22m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Apply(string text, params Func<string, string>[] codeFuncs)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        foreach (var func in codeFuncs)
        {
            text = func(text);
        }

        return text;
    }

    /// <summary>
    /// Degrades a 24-bit RGB color to the appropriate ANSI color codes based on the current ANSI mode,
    /// allowing for graceful fallback to lower color modes when necessary.
    /// </summary>
    /// <param name="color24bit">The 24-bit RGB color to use if the terminal supports 24-bit color.</param>
    /// <param name="color8bit">The 8-bit RGB color to use if the terminal supports 8-bit color.</param>
    /// <param name="color">The ANSI color code to use if the terminal supports only basic colors.</param>
    /// <returns>A function that applies the appropriate ANSI color codes based on the current ANSI mode.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var colorFunc = Ansi.DegradeColor(new Rgb(255, 0, 0), new Rgb(5, 0, 0), 31);
    /// var coloredText = colorFunc("This text will be colored based on ANSI mode");
    /// Console.WriteLine(coloredText);
    /// </code>
    /// </example>
    /// </remarks>
    public static Func<string, string> DegradeColor(Rgb color24bit, Rgb color8bit, int color)
    {
        switch (AnsiSettings.Current.Mode)
        {
            case AnsiMode.TwentyFourBit:
                return text => $"\u001b[38;2;{color24bit.R};{color24bit.G};{color24bit.B}m{text}\u001b[39m";
            case AnsiMode.EightBit:
                return text => $"\u001b[38;5;{color8bit.To256Color()}m{text}\u001b[39m";
            case AnsiMode.None:
                return text => text;
            default:
                return text => $"\u001b[{color}m{text}\u001b[39m";
        }
    }

    /// <summary>
    /// Degrades a 24-bit RGB background color to the appropriate ANSI background color codes based on the current ANSI mode,
    /// allowing for graceful fallback to lower color modes when necessary.
    /// </summary>
    /// <param name="color24bit">The 24-bit RGB color to use for the background if the terminal supports 24-bit color.</param>
    /// <param name="color8bit">The 8-bit RGB color to use for the background if the terminal supports 8-bit color.</param>
    /// <param name="color">The ANSI color code to use for the background if the terminal supports only basic colors.</param>
    /// <returns>A function that applies the appropriate ANSI background color codes based on the current ANSI mode.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var bgColorFunc = Ansi.DegradeBgColor(new Rgb(255, 0, 0), new Rgb(5, 0, 0), 41);
    /// var coloredText = bgColorFunc("This text will have a colored background based on ANSI mode");
    /// Console.WriteLine(coloredText);
    /// </code>
    /// </example>
    /// </remarks>
    public static Func<string, string> DegradeBgColor(Rgb color24bit, Rgb color8bit, int color)
    {
        switch (AnsiSettings.Current.Mode)
        {
            case AnsiMode.TwentyFourBit:
                return text => $"\u001b[48;2;{color24bit.R};{color24bit.G};{color24bit.B}m{text}\u001b[49m";
            case AnsiMode.EightBit:
                return text => $"\u001b[48;5;{color8bit.To256Color()}m{text}\u001b[49m";
            case AnsiMode.None:
                return text => text;
            default:
                return text => $"\u001b[{color}m{text}\u001b[49m";
        }
    }

    /// <summary>
    /// Applies white foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with white ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.White("white text");
    /// Assert.Equal("\u001b[37mwhite text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string White(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[37m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies black foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with black ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Black("black text");
    /// Assert.Equal("\u001b[30mblack text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Black(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[30m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies red foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with red ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Red("red text");
    /// Assert.Equal("\u001b[31mred text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Red(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[31m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies green foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with green ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Green("green text");
    /// Assert.Equal("\u001b[32mgreen text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Green(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[32m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies yellow foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with yellow ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Yellow("yellow text");
    /// Assert.Equal("\u001b[33myellow text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Yellow(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[33m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies blue foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with blue ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Blue("blue text");
    /// Assert.Equal("\u001b[34mblue text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Blue(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[34m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies magenta foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with magenta ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Magenta("magenta text");
    /// Assert.Equal("\u001b[35mmagenta text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Magenta(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[35m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies cyan foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with cyan ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Cyan("cyan text");
    /// Assert.Equal("\u001b[36mcyan text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Cyan(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[36m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright black foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright black ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightBlack("bright black text");
    /// Assert.Equal("\u001b[90mbright black text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightBlack(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[90m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright red foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright red ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightRed("bright red text");
    /// Assert.Equal("\u001b[91mbright red text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightRed(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[91m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright green foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright green ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightGreen("bright green text");
    /// Assert.Equal("\u001b[92mbright green text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightGreen(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[92m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright yellow foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright yellow ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightYellow("bright yellow text");
    /// Assert.Equal("\u001b[93mbright yellow text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightYellow(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[93m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright blue foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright blue ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightBlue("bright blue text");
    /// Assert.Equal("\u001b[94mbright blue text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightBlue(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[94m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright magenta foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright magenta ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightMagenta("bright magenta text");
    /// Assert.Equal("\u001b[95mbright magenta text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightMagenta(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[95m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright cyan foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright cyan ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightCyan("bright cyan text");
    /// Assert.Equal("\u001b[96mbright cyan text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightCyan(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[96m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies bright white foreground color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright white ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BrightWhite("bright white text");
    /// Assert.Equal("\u001b[97mbright white text\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BrightWhite(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[97m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies white background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with white background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgWhite("white bg");
    /// Assert.Equal("\u001b[47mwhite bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgWhite(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[47m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies black background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with black background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBlack("black bg");
    /// Assert.Equal("\u001b[40mblack bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBlack(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[40m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies red background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with red background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgRed("red bg");
    /// Assert.Equal("\u001b[41mred bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRed(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[41m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies green background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with green background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgGreen("green bg");
    /// Assert.Equal("\u001b[42mgreen bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgGreen(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[42m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies yellow background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with yellow background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgYellow("yellow bg");
    /// Assert.Equal("\u001b[43myellow bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgYellow(string text)
        => AnsiSettings.Current.Mode == AnsiMode.None ?
            text :
            $"\u001b[43m{text}\u001b[49m";

    /// <summary>
    /// Applies blue background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with blue background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBlue("blue bg");
    /// Assert.Equal("\u001b[44mblue bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBlue(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[44m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies magenta background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with magenta background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgMagenta("magenta bg");
    /// Assert.Equal("\u001b[45mmagenta bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgMagenta(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[45m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies cyan background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with cyan background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgCyan("cyan bg");
    /// Assert.Equal("\u001b[46mcyan bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgCyan(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[46m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright black background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright black background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightBlack("bright black bg");
    /// Assert.Equal("\u001b[100mbright black bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightBlack(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[100m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright red background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright red background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightRed("bright red bg");
    /// Assert.Equal("\u001b[101mbright red bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightRed(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[101m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright green background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright green background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightGreen("bright green bg");
    /// Assert.Equal("\u001b[102mbright green bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightGreen(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[102m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright yellow background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright yellow background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightYellow("bright yellow bg");
    /// Assert.Equal("\u001b[103mbright yellow bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightYellow(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[103m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright blue background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright blue background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightBlue("bright blue bg");
    /// Assert.Equal("\u001b[104mbright blue bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightBlue(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[104m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright magenta background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright magenta background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightMagenta("bright magenta bg");
    /// Assert.Equal("\u001b[105mbright magenta bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightMagenta(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[105m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright cyan background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright cyan background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightCyan("bright cyan bg");
    /// Assert.Equal("\u001b[106mbright cyan bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightCyan(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[106m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies bright white background color to the specified text.
    /// </summary>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with bright white background ANSI color codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgBrightWhite("bright white bg");
    /// Assert.Equal("\u001b[107mbright white bg\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgBrightWhite(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[107m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies an 8-bit color to the specified text.
    /// </summary>
    /// <param name="rgb">The 8-bit color index (0-255).</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 8-bit color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Rgb8(123, "text");
    /// Assert.Equal("\u001b[38;5;123mtext\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb8(uint rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[38;5;{rgb}m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies a 24-bit RGB color to the specified text using an Rgb struct.
    /// </summary>
    /// <param name="rgb">The RGB color value.</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 24-bit RGB color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var text = Ansi.Rgb(color, "text");
    /// Assert.Equal("\u001b[38;2;255;128;64mtext\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb(Rgb rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[38;2;{rgb.R};{rgb.G};{rgb.B}m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies a 24-bit RGB color to the specified text using a packed uint value.
    /// </summary>
    /// <param name="rgb">The packed RGB value (0xRRGGBB format).</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 24-bit RGB color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Rgb(0xFF8040, "text");
    /// Assert.Equal("\u001b[38;2;255;128;64mtext\u001b[39m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Rgb(uint rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[38;2;{(rgb >> 16) & 0xFF};{(rgb >> 8) & 0xFF};{rgb & 0xFF}m{text}\u001b[39m";
    }

    /// <summary>
    /// Applies an 8-bit background color to the specified text.
    /// </summary>
    /// <param name="rgb">The 8-bit color index (0-255).</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 8-bit background color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgRgb8(123, "text");
    /// Assert.Equal("\u001b[48;5;123mtext\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb8(uint rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[48;5;{rgb}m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies a 24-bit RGB background color to the specified text using a packed uint value.
    /// </summary>
    /// <param name="rgb">The packed RGB value (0xRRGGBB format).</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 24-bit RGB background color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.BgRgb(0xFF8040, "text");
    /// Assert.Equal("\u001b[48;2;255;128;64mtext\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb(uint rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[48;2;{(rgb >> 16) & 0xFF};{(rgb >> 8) & 0xFF};{rgb & 0xFF}m{text}\u001b[49m";
    }

    /// <summary>
    /// Applies a 24-bit RGB background color to the specified text using an Rgb struct.
    /// </summary>
    /// <param name="rgb">The RGB color value.</param>
    /// <param name="text">The text to color.</param>
    /// <returns>The text with 24-bit RGB background color ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// var text = Ansi.BgRgb(color, "text");
    /// Assert.Equal("\u001b[48;2;255;128;64mtext\u001b[49m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string BgRgb(Rgb rgb, string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[48;2;{rgb.R};{rgb.G};{rgb.B}m{text}\u001b[49m";
    }

    /// <summary>
    /// Formats the text as a hyperlink using ANSI escape codes.
    /// </summary>
    /// <param name="text">The URL to link to.</param>
    /// <returns>The text formatted as a hyperlink, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var link = Ansi.Link("https://example.com");
    /// Assert.Equal("\u001b]8;;https://example.com\u001b\\https://example.com\u001b]8;;\u001b\\", link);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Link(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b]8;;{text}\u001b\\{text}\u001b]8;;\u001b\\";
    }

    /// <summary>
    /// Applies bold styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with bold ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Bold("bold text");
    /// Assert.Equal("\u001b[1mbold text\u001b[22m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Bold(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[1m{text}\u001b[22m";
    }

    /// <summary>
    /// Applies dim styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with dim ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Dim("dim text");
    /// Assert.Equal("\u001b[2mdim text\u001b[22m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Dim(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[2m{text}\u001b[22m";
    }

    /// <summary>
    /// Applies italic styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with italic ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Italic("italic text");
    /// Assert.Equal("\u001b[3mitalic text\u001b[23m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Italic(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[3m{text}\u001b[23m";
    }

    /// <summary>
    /// Applies underline styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with underline ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Underline("underline text");
    /// Assert.Equal("\u001b[4munderline text\u001b[24m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Underline(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[4m{text}\u001b[24m";
    }

    /// <summary>
    /// Applies strikethrough styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with strikethrough ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Strikethrough("strikethrough text");
    /// Assert.Equal("\u001b[9mstrikethrough text\u001b[29m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Strikethrough(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[9m{text}\u001b[29m";
    }

    /// <summary>
    /// Applies blink styling to the specified text.
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with blink ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Blink("blink text");
    /// Assert.Equal("\u001b[5mblink text\u001b[25m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Blink(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[5m{text}\u001b[25m";
    }

    /// <summary>
    /// Applies inverse styling to the specified text (swaps foreground and background colors).
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with inverse ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Inverse("inverse text");
    /// Assert.Equal("\u001b[7minverse text\u001b[27m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Inverse(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[7m{text}\u001b[27m";
    }

    /// <summary>
    /// Applies hidden styling to the specified text (text is not displayed).
    /// </summary>
    /// <param name="text">The text to style.</param>
    /// <returns>The text with hidden ANSI codes applied, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Hidden("hidden text");
    /// Assert.Equal("\u001b[8mhidden text\u001b[28m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Hidden(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[8m{text}\u001b[28m";
    }

    /// <summary>
    /// Resets all ANSI styling and applies it to the specified text.
    /// </summary>
    /// <param name="text">The text to wrap with reset codes.</param>
    /// <returns>The text wrapped with reset ANSI codes, or plain text if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var text = Ansi.Reset("reset text");
    /// Assert.Equal("\u001b[0mreset text\u001b[0m", text);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Reset(string text)
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return text;

        return $"\u001b[0m{text}\u001b[0m";
    }

    /// <summary>
    /// Returns an ANSI reset code to clear all styling.
    /// </summary>
    /// <returns>The ANSI reset code, or an empty string if mode is None.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var reset = Ansi.Reset();
    /// Assert.Equal("\u001b[0m", reset);
    /// </code>
    /// </example>
    /// </remarks>
    public static string Reset()
    {
        if (AnsiSettings.Current.Mode == AnsiMode.None)
            return string.Empty;

        return "\u001b[0m";
    }
}