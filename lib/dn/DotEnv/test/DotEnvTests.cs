using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeoBeard.DotEnv.Tests;

public static class DotEnvTests
{
    [Fact]
    public static void Parse_Simple_Key_Value()
    {
        var doc = DotEnvFile.Parse("KEY=value");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Double_Quoted_Value()
    {
        var doc = DotEnvFile.Parse("KEY=\"value\"");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Single_Quoted_Value()
    {
        var doc = DotEnvFile.Parse("KEY='value'");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Backtick_Quoted_Value()
    {
        var doc = DotEnvFile.Parse("KEY=`value`");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Empty_Value()
    {
        var doc = DotEnvFile.Parse("KEY=");
        Assert.Equal(string.Empty, doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Value_With_Spaces()
    {
        var doc = DotEnvFile.Parse("KEY=value with spaces");
        Assert.Equal("value with spaces", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Newline()
    {
        var doc = DotEnvFile.Parse("KEY=\"value\\nwith\\nnewlines\"");
        Assert.Equal("value\nwith\nnewlines", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Tab()
    {
        var doc = DotEnvFile.Parse("KEY=\"value\\twith\\ttabs\"");
        Assert.Equal("value\twith\ttabs", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Carriage_Return()
    {
        var doc = DotEnvFile.Parse("KEY=\"value\\rwith\\rcr\"");
        Assert.Equal("value\rwith\rcr", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Backslash()
    {
        var doc = DotEnvFile.Parse("KEY=\"value\\\\with\\\\backslash\"");
        Assert.Equal("value\\with\\backslash", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Quote()
    {
        var doc = DotEnvFile.Parse("KEY=\"value with \\\"escaped quotes\\\"\"");
        Assert.Equal("value with \"escaped quotes\"", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Unicode_4Digit()
    {
        var doc = DotEnvFile.Parse("KEY=\"snowman \\u2603\"");
        Assert.Equal("snowman \u2603", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Escaped_Unicode_8Digit()
    {
        var doc = DotEnvFile.Parse("KEY=\"cowboy \\U0001F920\"");
        Assert.Equal("cowboy \U0001F920", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Single_Quote_No_Escapes()
    {
        var doc = DotEnvFile.Parse("KEY='value with \\\\n'");
        Assert.Equal("value with \\\\n", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Multiline_Value()
    {
        var input = "KEY=\"line1\nline2\nline3\"";
        var doc = DotEnvFile.Parse(input);
        Assert.Equal("line1\nline2\nline3", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Comment()
    {
        var doc = DotEnvFile.Parse("# This is a comment\nKEY=value");
        Assert.Single(doc.Comments);
        Assert.Equal("This is a comment", doc.Comments[0]);
    }

    [Fact]
    public static void Parse_Inline_Comment()
    {
        var doc = DotEnvFile.Parse("KEY=value # inline comment");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Unicode_Value()
    {
        var doc = DotEnvFile.Parse("KEY=こんにちは");
        Assert.Equal("こんにちは", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Emoji_Value()
    {
        var doc = DotEnvFile.Parse("KEY=😈");
        Assert.Equal("😈", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Quoted_Emoji()
    {
        var doc = DotEnvFile.Parse("KEY=\"😈\"");
        Assert.Equal("😈", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Multiple_Variables()
    {
        var doc = DotEnvFile.Parse("KEY1=value1\nKEY2=value2\nKEY3=value3");
        Assert.Equal(3, doc.Keys.Count);
        Assert.Equal("value1", doc.Get("KEY1"));
        Assert.Equal("value2", doc.Get("KEY2"));
        Assert.Equal("value3", doc.Get("KEY3"));
    }

    [Fact]
    public static void Parse_Key_With_Underscore()
    {
        var doc = DotEnvFile.Parse("KEY_WITH_UNDERSCORE=value");
        Assert.Equal("value", doc.Get("KEY_WITH_UNDERSCORE"));
    }

    [Fact]
    public static void Parse_Key_With_Digits()
    {
        var doc = DotEnvFile.Parse("KEY123=value");
        Assert.Equal("value", doc.Get("KEY123"));
    }

    [Fact]
    public static void Parse_Whitespace_Before_Key()
    {
        var doc = DotEnvFile.Parse("   KEY=value");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Whitespace_After_Key()
    {
        var doc = DotEnvFile.Parse("KEY   =value");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Whitespace_Before_Value()
    {
        var doc = DotEnvFile.Parse("KEY=   value");
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Parse_Key_With_Space_Throws()
    {
        Assert.Throws<DotEnvParseException>(() => DotEnvFile.Parse("KEY WITH=value"));
    }

    [Fact]
    public static void Parse_Value_Without_Key_Throws()
    {
        Assert.Throws<DotEnvParseException>(() => DotEnvFile.Parse("=value"));
    }

    [Fact]
    public static void Parse_Complex_Document()
    {
        var input = "KEY1=\"value1\"\n\nKEY2='value2'\nKey3=value3\nKey4=a value with spaces\n# This is a comment\nKey5=\"a value with \\\"escaped quotes\\\"\"\nKey6='a value with \\'single quotes\\''\nKey7=\"line1\nline2\nline3\n\"\nKey8=\"value with \\nnewlines\"\nKey9=\"value with \\t tabs\"\nKey11=\"😈\"";
        var doc = DotEnvFile.Parse(input);

        Assert.Equal("value1", doc.Get("KEY1"));
        Assert.Equal("value2", doc.Get("KEY2"));
        Assert.Equal("value3", doc.Get("Key3"));
        Assert.Equal("a value with spaces", doc.Get("Key4"));
        Assert.Equal("a value with \"escaped quotes\"", doc.Get("Key5"));
        Assert.Equal("a value with 'single quotes'", doc.Get("Key6"));
        Assert.Equal("line1\nline2\nline3\n", doc.Get("Key7"));
        Assert.Equal("value with \nnewlines", doc.Get("Key8"));
        Assert.Equal("value with \t tabs", doc.Get("Key9"));
        Assert.Equal("😈", doc.Get("Key11"));
        Assert.Single(doc.Comments);
    }

    [Fact]
    public static void ParseStream_Works()
    {
        var content = "KEY=value\nKEY2=value2";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var doc = DotEnvFile.ParseStream(stream);
        Assert.Equal("value", doc.Get("KEY"));
        Assert.Equal("value2", doc.Get("KEY2"));
    }

    [Fact]
    public static async Task ParseFileAsync_Works()
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempPath, "KEY=value", TestContext.Current.CancellationToken);
            var doc = await DotEnvFile.ParseFileAsync(tempPath, TestContext.Current.CancellationToken);
            Assert.Equal("value", doc.Get("KEY"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public static void RoundTrip_Preserves_Quotes()
    {
        var input = "KEY=\"value\"\nKEY2='value2'\nKEY3=value3";
        var doc = DotEnvFile.Parse(input);
        var output = doc.ToString().Trim();

        Assert.Contains("KEY=\"value\"", output);
        Assert.Contains("KEY2='value2'", output);
        Assert.Contains("KEY3=value3", output);
    }
}