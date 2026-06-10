using System;

namespace NeoBeard.Colors.Primitives.Tests;

public class ColorFormatConversionTests
{
    private const double Tolerance = 1d;

    [Fact]
    public void Rgb_ToHex_RoundTripsThroughBytes()
    {
        var rgb = new Rgb(15, 102, 243);

        var hex = new Hex(rgb.R, rgb.G, rgb.B);
        var roundTrip = new Rgb(hex.R, hex.G, hex.B);

        Assert.Equal(rgb, roundTrip);
        Assert.Equal("0F66F3", hex.ToString());
    }

    [Fact]
    public void Hex_And_Hexa_SupportedFormats_Parse_Correctly()
    {
        var shortHex = new Hex("#0F6");
        var withAlphaHexa = new Hexa("#0F66F3CC");
        var longHexa = new Hexa("F6A8CC");
        var alphaHexa = new Hexa("F6A8CC77");

        Assert.Equal((byte)0, shortHex.R);
        Assert.Equal((byte)255, shortHex.G);
        Assert.Equal((byte)102, shortHex.B);

        Assert.Equal((byte)15, withAlphaHexa.R);
        Assert.Equal((byte)102, withAlphaHexa.G);
        Assert.Equal((byte)243, withAlphaHexa.B);
        Assert.Equal((byte)204, withAlphaHexa.A);

        Assert.Equal((byte)246, longHexa.R);
        Assert.Equal((byte)168, longHexa.G);
        Assert.Equal((byte)204, longHexa.B);
        Assert.Equal((byte)255, longHexa.A);

        Assert.Equal((byte)246, alphaHexa.R);
        Assert.Equal((byte)168, alphaHexa.G);
        Assert.Equal((byte)204, alphaHexa.B);
        Assert.Equal((byte)119, alphaHexa.A);
    }

    [Fact]
    public void Hexa_TryParse_ParsesAllSupportedLengths_AndSetsAlpha()
    {
        Assert.True(Hexa.TryParse("#0F66F3", out var shortToLong));
        Assert.Equal((byte)15, shortToLong.R);
        Assert.Equal((byte)102, shortToLong.G);
        Assert.Equal((byte)243, shortToLong.B);
        Assert.Equal((byte)255, shortToLong.A);

        Assert.True(Hexa.TryParse("F6A8CC", out var sixDigit));
        Assert.Equal((byte)246, sixDigit.R);
        Assert.Equal((byte)168, sixDigit.G);
        Assert.Equal((byte)204, sixDigit.B);
        Assert.Equal((byte)255, sixDigit.A);

        Assert.True(Hexa.TryParse("11223344", out var withAlpha));
        Assert.Equal((byte)17, withAlpha.R);
        Assert.Equal((byte)34, withAlpha.G);
        Assert.Equal((byte)51, withAlpha.B);
        Assert.Equal((byte)68, withAlpha.A);
    }

    [Theory]
    [InlineData("#GG")]
    [InlineData("#1234")]
    [InlineData("#12")]
    public void Hex_Parse_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => new Hex(input));
        Assert.Throws<FormatException>(() => new Hexa(input));
    }

    [Fact]
    public void Rgb_And_Rgba_Conversions_RetainRgb_AndRespectAlpha()
    {
        var rgb = new Rgb(200, 10, 5);
        var rgba = new Rgba(rgb.R, rgb.G, rgb.B);
        var fromBytes = new Rgba((byte)200, (byte)10, (byte)5, (byte)255);
        Rgb roundTrip = rgba;

        Assert.Equal(rgb, roundTrip);
        Assert.Equal(rgba, fromBytes);
        Assert.Equal(255, (byte)rgba.A);
    }

    [Fact]
    public void Rgba_And_Hexa_Conversions_LetDataFlow_BothDirections()
    {
        var rgba = new Rgba((byte)10, (byte)20, (byte)30, (byte)40);
        var hexa = new Hexa(rgba.R, rgba.G, rgba.B, (byte)rgba.A);
        var backToRgba = new Rgba(hexa.R, hexa.G, hexa.B, (byte)hexa.A);

        Assert.Equal(rgba, backToRgba);
        Assert.Equal(rgba.R, hexa.R);
        Assert.Equal(rgba.G, hexa.G);
        Assert.Equal(rgba.B, hexa.B);
        Assert.Equal((byte)rgba.A, hexa.A);
    }

    [Fact]
    public void Argb_And_Rgb_SharePackedAndComponentData()
    {
        var argb = new Argb(255, 12, 34, 56);
        var (a, r, g, b) = argb;
        var rebuilt = new Argb(a, r, g, b);
        var rgb = new Rgb(argb.R, argb.G, argb.B);

        Assert.Equal(argb, rebuilt);
        Assert.Equal((byte)255, a);
        Assert.Equal((byte)12, r);
        Assert.Equal((byte)34, g);
        Assert.Equal((byte)56, b);
        Assert.Equal(new Rgb(12, 34, 56), rgb);
    }

    [Fact]
    public void Color_ImplicitConversions_WorkWithRgbAndRgba()
    {
        Rgb rgb = new Rgb(7, 8, 9);
        Color color = rgb;
        Rgb rgbAgain = color;

        Assert.Equal(rgb, rgbAgain);

        var withAlpha = new Color(7, 8, 9, 100);
        var rgba = new Rgba(withAlpha.R, withAlpha.G, withAlpha.B, (byte)withAlpha.A);
        var roundTrip = new Color(rgba);

        Assert.Equal(withAlpha, roundTrip);
    }

    [Fact]
    public void Hsl_Hwb_Lab_Lch_Oklch_RoundTrips_AllStayNearOriginalRgb()
    {
        var rgb = new Rgb(14, 125, 221);

        var hsl = rgb.ToHsl();
        var hwb = hsl.ToHwb();
        var rgbFromHwb = hwb.ToRgb();

        var lab = rgb.ToLab();
        var lch = lab.ToLch();
        var rgbFromLch = lch.ToLab().ToRgb();

        var oklch = rgb.ToOklch();
        var rgbFromOklch = oklch.ToRgb();

        AssertClose(rgb, rgbFromHwb);
        AssertClose(rgb, rgbFromLch);
        AssertClose(rgb, rgbFromOklch);
    }

    [Fact]
    public void ColorSpace_InterConversions_BetweenWideGamutSpaces_AndRgb_StayNearOriginal()
    {
        var rgb = new Rgb(45, 120, 220);

        var displayP3 = rgb.ToDisplayP3();
        var rec2020FromDisplay = displayP3.ToRec2020();
        var rgbFromRec2020 = rec2020FromDisplay.ToRgb();

        var adobeFromDisplay = displayP3.ToAdobeRgb();
        var rgbFromAdobe = adobeFromDisplay.ToRgb();

        var rec2020 = rgb.ToRec2020();
        var displayFromRec2020 = rec2020.ToDisplayP3();
        var rgbFromDisplay = displayFromRec2020.ToRgb();

        AssertClose(rgb, rgbFromRec2020);
        AssertClose(rgb, rgbFromAdobe);
        AssertClose(rgb, rgbFromDisplay);
    }

    [Fact]
    public void Hex_RoundTrips_ToHexa_WithOpaqueAlpha()
    {
        var hex = new Hex(1, 2, 3);
        var hexa = new Hexa(hex.R, hex.G, hex.B, (byte)255);
        var parsed = new Hexa("010203FF");

        Assert.Equal((byte)255, hexa.A);
        Assert.Equal(hexa, parsed);
    }

    [Fact]
    public void Rgba_Deconstruct_IncludesAlphaChannel()
    {
        var rgba = new Rgba((byte)64, (byte)128, (byte)192, (byte)16);

        var (r, g, b, a) = rgba;

        Assert.Equal((byte)64, r);
        Assert.Equal((byte)128, g);
        Assert.Equal((byte)192, b);
        Assert.Equal((byte)16, (byte)a);
    }

    [Fact]
    public void ColorFormat_Conversions_AllPublicMethods_StayWithinTolerance()
    {
        var rgb = new Rgb(14, 125, 221);

        AssertClose(rgb, rgb.ToHsl().ToRgb());
        AssertClose(rgb, rgb.ToHwb().ToRgb());
        AssertClose(rgb, rgb.ToLab().ToRgb());
        AssertClose(rgb, rgb.ToLab().ToLch().ToLab().ToRgb());
        AssertClose(rgb, rgb.ToOklch().ToRgb());
        AssertClose(rgb, rgb.ToDisplayP3().ToRgb());
        AssertClose(rgb, rgb.ToRec2020().ToRgb());
        AssertClose(rgb, rgb.ToAdobeRgb().ToRgb());

        var hsl = rgb.ToHsl();
        AssertClose(rgb, hsl.ToRgb());
        AssertClose(rgb, hsl.ToHwb().ToRgb());
        AssertClose(rgb, hsl.ToOklch().ToRgb());
        AssertClose(rgb, hsl.ToOklch().ToHsl().ToRgb());

        var hwb = rgb.ToHwb();
        AssertClose(rgb, hwb.ToHsl().ToRgb());

        var display = rgb.ToDisplayP3();
        AssertClose(rgb, display.ToRec2020().ToRgb());
        AssertClose(rgb, display.ToAdobeRgb().ToRgb());

        var rec = rgb.ToRec2020();
        AssertClose(rgb, rec.ToDisplayP3().ToRgb());
        AssertClose(rgb, rec.ToAdobeRgb().ToRgb());

        var adobe = rgb.ToAdobeRgb();
        AssertClose(rgb, adobe.ToDisplayP3().ToRgb());
        AssertClose(rgb, adobe.ToRec2020().ToRgb());
    }


    [Fact]
    public void ColorFormatConversionMatrix_AllTransforms_AreSelfConsistent()
    {
        var rgb = new Rgb(14, 125, 221);

        var hsl = rgb.ToHsl();
        var hwb = rgb.ToHwb();
        var lab = rgb.ToLab();
        var lch = lab.ToLch();
        var oklch = rgb.ToOklch();
        var display = rgb.ToDisplayP3();
        var rec2020 = rgb.ToRec2020();
        var adobe = rgb.ToAdobeRgb();

        AssertClose(rgb, hsl.ToRgb());
        AssertClose(rgb, hwb.ToRgb());
        AssertClose(rgb, hsl.ToHwb().ToRgb());
        AssertClose(rgb, hwb.ToHsl().ToRgb());

        AssertClose(rgb, lab.ToRgb());
        AssertClose(rgb, lch.ToLab().ToRgb());

        AssertClose(rgb, oklch.ToRgb());
        AssertClose(rgb, hsl.ToOklch().ToRgb());
        AssertClose(rgb, oklch.ToHsl().ToRgb());

        AssertClose(rgb, display.ToRgb());
        AssertClose(rgb, display.ToRec2020().ToDisplayP3().ToRgb());
        AssertClose(rgb, display.ToAdobeRgb().ToDisplayP3().ToRgb());

        AssertClose(rgb, rec2020.ToRgb());
        AssertClose(rgb, rec2020.ToDisplayP3().ToRec2020().ToRgb());
        AssertClose(rgb, rec2020.ToAdobeRgb().ToRec2020().ToRgb());

        AssertClose(rgb, adobe.ToRgb());
        AssertClose(rgb, adobe.ToDisplayP3().ToAdobeRgb().ToRgb());
        AssertClose(rgb, adobe.ToRec2020().ToAdobeRgb().ToRgb());

        AssertClose(rgb, rgb.ToDisplayP3().ToRec2020().ToAdobeRgb().ToDisplayP3().ToRgb());
        AssertClose(rgb, rgb.ToRec2020().ToAdobeRgb().ToDisplayP3().ToRec2020().ToRgb());
        AssertClose(rgb, rgb.ToAdobeRgb().ToDisplayP3().ToRec2020().ToAdobeRgb().ToRgb());
    }

    private static void AssertClose(Rgb expected, Rgb actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0d, Tolerance);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0d, Tolerance);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0d, Tolerance);
    }
}