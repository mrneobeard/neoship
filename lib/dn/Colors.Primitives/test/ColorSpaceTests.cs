using System;

namespace NeoBeard.Colors.Primitives.Tests;

public class ColorSpaceTests
{
    private const double Tolerance = 1d;

    [Fact]
    public void Rgb_ConvertToHexAndBack_PreservesComponents()
    {
        var rgb = new Rgb(12, 34, 56);

        var hex = new Hex(rgb.R, rgb.G, rgb.B);
        var actual = new Rgb(hex.R, hex.G, hex.B);

        Assert.Equal(rgb, actual);
    }

    [Fact]
    public void Rgb_Hsl_RoundTrip_PreservesValue()
    {
        var rgb = new Rgb(64, 128, 192);
        var hsl = rgb.ToHsl();
        var roundTrip = hsl.ToRgb();

        Assert.Equal(rgb, roundTrip);
    }

    [Fact]
    public void Rgb_Hwb_RoundTrip_CloseToOriginal()
    {
        var rgb = new Rgb(40, 120, 220);
        var hwb = rgb.ToHwb();
        var roundTrip = hwb.ToRgb();

        AssertClose(rgb, roundTrip);
    }

    [Fact]
    public void Rgb_Lab_Lch_RoundTrip_CloseToOriginal()
    {
        var rgb = new Rgb(80, 40, 220);
        var lab = rgb.ToLab();
        var lch = lab.ToLch();
        var rgbFromLch = lch.ToLab().ToRgb();

        AssertClose(rgb, rgbFromLch);
    }

    [Fact]
    public void Rgb_Oklch_RoundTrip_CloseToOriginal()
    {
        var rgb = new Rgb(220, 40, 80);
        var oklch = rgb.ToOklch();
        var roundTrip = oklch.ToRgb();

        AssertClose(rgb, roundTrip);
    }

    [Fact]
    public void Rgb_DisplayP3_ConvertToRgb_CloseToOriginal()
    {
        var rgb = new Rgb(100, 120, 140);
        var displayP3 = rgb.ToDisplayP3();
        var roundTrip = displayP3.ToRgb();

        AssertClose(rgb, roundTrip);
    }

    [Fact]
    public void Rgb_Rec2020_ConvertToRgb_CloseToOriginal()
    {
        var rgb = new Rgb(10, 150, 200);
        var rec2020 = rgb.ToRec2020();
        var roundTrip = rec2020.ToRgb();

        AssertClose(rgb, roundTrip);
    }

    [Fact]
    public void Rgb_AdobeRgb_ConvertToRgb_CloseToOriginal()
    {
        var rgb = new Rgb(200, 180, 40);
        var adobeRgb = rgb.ToAdobeRgb();
        var roundTrip = adobeRgb.ToRgb();

        AssertClose(rgb, roundTrip);
    }

    [Fact]
    public void Hsl_ConvertToRgb_WorksForPureRed()
    {
        var rgb = new Hsl(0d, 1d, 0.5d).ToRgb();

        Assert.Equal(new Rgb(255, 0, 0), rgb);
    }

    [Theory]
    [InlineData("#0A0B0C", 10, 11, 12)]
    [InlineData("0A0B0C", 10, 11, 12)]
    [InlineData("#ABC", 170, 187, 204)]
    [InlineData("abc", 170, 187, 204)]
    public void Hex_Parse_AcceptsSupportedFormats(string input, int expectedR, int expectedG, int expectedB)
    {
        var hex = new Hex(input);

        Assert.Equal((byte)expectedR, hex.R);
        Assert.Equal((byte)expectedG, hex.G);
        Assert.Equal((byte)expectedB, hex.B);
        Assert.Equal($"{expectedR:X2}{expectedG:X2}{expectedB:X2}", hex.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("#GGHHHH")]
    public void Hex_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => new Hex(input));
    }

    [Theory]
    [InlineData("#F84", 255, 136, 68)]
    [InlineData("#abc", 170, 187, 204)]
    public void Hex_TryParse_ValidInput_ReturnsTrue(string input, int expectedR, int expectedG, int expectedB)
    {
        var result = Hex.TryParse(input, out var hex);

        Assert.True(result);
        Assert.Equal((byte)expectedR, hex.R);
        Assert.Equal((byte)expectedG, hex.G);
        Assert.Equal((byte)expectedB, hex.B);
    }

    [Fact]
    public void Hex_TryParse_InvalidHexCharacters_ReturnsFalse()
    {
        var result = Hex.TryParse("ZZZ", out var hex);

        Assert.False(result);
        Assert.Equal(default, hex);
    }

    [Fact]
    public void Alpha_DefaultsAndConversions_WorkAsExpected()
    {
        var alphaDefault = new Alpha();
        var alphaHalf = new Alpha(0.5d);
        var alphaFromDouble = (Alpha)0.5d;
        var alphaFromByte = (Alpha)(byte)128;

        Assert.Equal(1d, alphaDefault.A);
        Assert.Equal(0.5d, alphaHalf.A);
        Assert.Equal(0.5d, alphaFromDouble.A);
        Assert.Equal((byte)128, (byte)alphaFromByte);
    }

    [Theory]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    public void Alpha_OutOfRange_ThrowsArgumentOutOfRange(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Alpha(value));
    }

    [Fact]
    public void Rgba_ByteAndAlphaConstructors_BehaveConsistently()
    {
        var withOpaqueByte = new Rgba(10, 20, 30);
        var withExplicitAlpha = new Rgba(10, 20, 30, Alpha.Transparent);
        var fromBytes = new Rgba((byte)10, (byte)20, (byte)30, (byte)255);

        Assert.Equal(255, (byte)withOpaqueByte.A);
        Assert.Equal(0, (byte)withExplicitAlpha.A);
        Assert.Equal(withOpaqueByte, fromBytes);
    }

    [Fact]
    public void Argb_OutOfRange_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Argb(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Argb(0x1_0000_0000));
    }

    [Fact]
    public void To256Color_UsesKnownPaletteIndexes()
    {
        Assert.Equal(196, new Rgb(255, 0, 0).To256Color());
        Assert.Equal(244, new Rgb(128, 128, 128).To256Color());
    }

    [Fact]
    public void Color_EqualityAndDeconstruct_IncludeAlpha()
    {
        var color = new Color(1, 2, 3, 4);
        var another = new Color(1, 2, 3, 4);
        var (r, g, b, a) = color;

        Assert.True(color == another);
        Assert.False(color != another);
        Assert.True(color.Equals(another));
        Assert.Equal(1, r);
        Assert.Equal(2, g);
        Assert.Equal(3, b);
        Assert.Equal(4, a);
    }

    private static void AssertClose(Rgb expected, Rgb actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0d, Tolerance);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0d, Tolerance);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0d, Tolerance);
    }
}
