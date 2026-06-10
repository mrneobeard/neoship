using System;

namespace NeoBeard.Colors;

/// <summary>
/// Conversion helpers for additional color spaces.
/// </summary>
public static class ColorSpaceConversions
{
    private const double D65WhiteX = 0.95047d;

    private const double D65WhiteY = 1d;

    private const double D65WhiteZ = 1.08883d;

    private const double LabEpsilon = 216d / 24389d;

    private const double LabKappa = 24389d / 27d;

    private const double Rec2020Gamma = 0.45d;

    private const double Rec2020InvGamma = 1d / Rec2020Gamma;

    private const double SrgbToXyzR0 = 0.4124564d;

    private const double SrgbToXyzR1 = 0.3575761d;

    private const double SrgbToXyzR2 = 0.1804375d;

    private const double SrgbToXyzG0 = 0.2126729d;

    private const double SrgbToXyzG1 = 0.7151522d;

    private const double SrgbToXyzG2 = 0.072175d;

    private const double SrgbToXyzB0 = 0.0193339d;

    private const double SrgbToXyzB1 = 0.119192d;

    private const double SrgbToXyzB2 = 0.9503041d;

    private const double XyzToSrgbR0 = 3.2404542d;

    private const double XyzToSrgbR1 = -1.5371385d;

    private const double XyzToSrgbR2 = -0.4985314d;

    private const double XyzToSrgbG0 = -0.969266d;

    private const double XyzToSrgbG1 = 1.8760108d;

    private const double XyzToSrgbG2 = 0.041556d;

    private const double XyzToSrgbB0 = 0.0556434d;

    private const double XyzToSrgbB1 = -0.2040259d;

    private const double XyzToSrgbB2 = 1.0572252d;

    private const double XyzToDisplayP3R0 = 2.4934969d;

    private const double XyzToDisplayP3R1 = -0.9313836d;

    private const double XyzToDisplayP3R2 = -0.4027108d;

    private const double XyzToDisplayP3G0 = -0.8294889d;

    private const double XyzToDisplayP3G1 = 1.762664d;

    private const double XyzToDisplayP3G2 = 0.0236247d;

    private const double XyzToDisplayP3B0 = 0.0358458d;

    private const double XyzToDisplayP3B1 = -0.0761724d;

    private const double XyzToDisplayP3B2 = 0.9568845d;

    private const double DisplayP3ToXyzR0 = 0.4865709d;

    private const double DisplayP3ToXyzR1 = 0.2656676d;

    private const double DisplayP3ToXyzR2 = 0.1982173d;

    private const double DisplayP3ToXyzG0 = 0.2289746d;

    private const double DisplayP3ToXyzG1 = 0.6917385d;

    private const double DisplayP3ToXyzG2 = 0.0792869d;

    private const double DisplayP3ToXyzB0 = 0d;

    private const double DisplayP3ToXyzB1 = 0.0451134d;

    private const double DisplayP3ToXyzB2 = 1.043944d;

    private const double XyzToAdobeR0 = 2.041369d;

    private const double XyzToAdobeR1 = -0.5649464d;

    private const double XyzToAdobeR2 = -0.3446944d;

    private const double XyzToAdobeG0 = -0.969266d;

    private const double XyzToAdobeG1 = 1.8760108d;

    private const double XyzToAdobeG2 = 0.041556d;

    private const double XyzToAdobeB0 = 0.0134474d;

    private const double XyzToAdobeB1 = -0.1183897d;

    private const double XyzToAdobeB2 = 1.0154096d;

    private const double AdobeToXyzR0 = 0.5767309d;

    private const double AdobeToXyzR1 = 0.185554d;

    private const double AdobeToXyzR2 = 0.1881852d;

    private const double AdobeToXyzG0 = 0.2973769d;

    private const double AdobeToXyzG1 = 0.6273491d;

    private const double AdobeToXyzG2 = 0.0752741d;

    private const double AdobeToXyzB0 = 0.0270343d;

    private const double AdobeToXyzB1 = 0.0706872d;

    private const double AdobeToXyzB2 = 0.991108d;

    private const double Rec2020ToXyzR0 = 0.636958d;

    private const double Rec2020ToXyzR1 = 0.144617d;

    private const double Rec2020ToXyzR2 = 0.168881d;

    private const double Rec2020ToXyzG0 = 0.2627d;

    private const double Rec2020ToXyzG1 = 0.677998d;

    private const double Rec2020ToXyzG2 = 0.0593017d;

    private const double Rec2020ToXyzB0 = 0d;

    private const double Rec2020ToXyzB1 = 0.0280727d;

    private const double Rec2020ToXyzB2 = 1.060985d;

    private const double XyzToRec2020R0 = 1.716651d;

    private const double XyzToRec2020R1 = -0.355671d;

    private const double XyzToRec2020R2 = -0.253366d;

    private const double XyzToRec2020G0 = -0.666684d;

    private const double XyzToRec2020G1 = 1.616481d;

    private const double XyzToRec2020G2 = 0.0157685d;

    private const double XyzToRec2020B0 = 0.01764d;

    private const double XyzToRec2020B1 = -0.0427707d;

    private const double XyzToRec2020B2 = 0.942103d;

    private const double OklabToLinearR0 = 4.0767416621d;

    private const double OklabToLinearR1 = -3.3077115913d;

    private const double OklabToLinearR2 = 0.2309699292d;

    private const double OklabToLinearG0 = -1.2684380046d;

    private const double OklabToLinearG1 = 2.6097574011d;

    private const double OklabToLinearG2 = -0.3413193965d;

    private const double OklabToLinearB0 = -0.0041960863d;

    private const double OklabToLinearB1 = -0.7034186170d;

    private const double OklabToLinearB2 = 1.7076147010d;

    private const double LinearToOklabL0 = 0.2104542553d;

    private const double LinearToOklabL1 = 0.7936177850d;

    private const double LinearToOklabL2 = -0.0040720468d;

    private const double LinearToOklabA0 = 1.9779984951d;

    private const double LinearToOklabA1 = -2.4285922050d;

    private const double LinearToOklabA2 = 0.4505937099d;

    private const double LinearToOklabB0 = 0.0259040371d;

    private const double LinearToOklabB1 = 0.7827717662d;

    private const double LinearToOklabB2 = -0.8086757660d;
    private const double OklabToLinearL1 = 0.3963377774d;

    private const double OklabToLinearL2 = 0.2158037573d;

    private const double OklabToLinearA1 = -0.1055613458d;

    private const double OklabToLinearA2 = -0.0638541728d;

    private const double OklabToLinearLms0 = 1d;

    private const double OklabToLinearLms1 = -0.0894841775d;

    private const double OklabToLinearLms2 = -1.2914855480d;

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Hsl"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="Hsl"/> value with hue, saturation and lightness equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var hsl = new Rgb(255, 0, 0).ToHsl();
    /// </code>
    /// </example>
    public static Hsl ToHsl(this Rgb value)
    {
        var r = value.R / 255d;
        var g = value.G / 255d;
        var b = value.B / 255d;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        var lightness = (max + min) / 2d;

        if (Math.Abs(delta) < double.Epsilon)
        {
            return new Hsl(0d, 0d, lightness);
        }

        var saturation = lightness <= 0.5d
            ? delta / (max + min)
            : delta / (2d - max - min);

        double hue;
        if (Math.Abs(max - r) < double.Epsilon)
        {
            hue = (g - b) / delta;
            if (hue < 0d)
                hue += 6d;
        }
        else if (Math.Abs(max - g) < double.Epsilon)
        {
            hue = ((b - r) / delta) + 2d;
        }
        else
        {
            hue = ((r - g) / delta) + 4d;
        }

        return new Hsl(hue * 60d, saturation, lightness);
    }

    /// <summary>
    /// Converts an <see cref="Hsl"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Hsl"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Hsl(120d, 1d, 0.5d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this Hsl value)
    {
        var h = Math.Clamp(value.Hue / 360d, 0d, 1d);
        var s = value.Saturation;
        var l = value.Lightness;

        if (s <= 0d)
            return new Rgb(ToByte(l), ToByte(l), ToByte(l));

        var q = l < 0.5d
            ? l * (1d + s)
            : (l + s) - (l * s);

        var p = (2d * l) - q;

        var r = HueToRgb(p, q, h + (1d / 3d));
        var g = HueToRgb(p, q, h);
        var b = HueToRgb(p, q, h - (1d / 3d));

        return new Rgb(ToByte(r), ToByte(g), ToByte(b));
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Hwb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="Hwb"/> value derived from <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var hwb = new Rgb(255, 200, 100).ToHwb();
    /// </code>
    /// </example>
    public static Hwb ToHwb(this Rgb value)
    {
        var hsl = value.ToHsl();
        var rf = value.R / 255d;
        var gf = value.G / 255d;
        var bf = value.B / 255d;

        var whiteness = Math.Min(rf, Math.Min(gf, bf));
        var blackness = 1d - Math.Max(rf, Math.Max(gf, bf));

        return new Hwb(hsl.Hue, whiteness, blackness);
    }

    /// <summary>
    /// Converts an <see cref="Hwb"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Hwb"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Hwb(180d, 0.1d, 0.1d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this Hwb value)
    {
        var w = Math.Clamp(value.Whiteness, 0d, 1d);
        var black = Math.Clamp(value.Blackness, 0d, 1d);
        var h = Math.Clamp(value.Hue / 360d, 0d, 1d);

        var sum = w + black;
        if (sum >= 1d)
        {
            var normalized = sum <= 0d ? 0d : w / sum;
            return new Rgb(ToByte(normalized), ToByte(normalized), ToByte(normalized));
        }

        var rgb = HueToRgb(value: h);
        var chroma = 1d - w - black;
        var r = (rgb.Item1 * chroma) + w;
        var g = (rgb.Item2 * chroma) + w;
        var b = (rgb.Item3 * chroma) + w;

        return new Rgb(ToByte(r), ToByte(g), ToByte(b));
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Lab"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="Lab"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var lab = new Rgb(80, 90, 100).ToLab();
    /// </code>
    /// </example>
    public static Lab ToLab(this Rgb value)
    {
        var linear = SrgbToLinear(value);
        var x = (SrgbToXyzR0 * linear.red) + (SrgbToXyzR1 * linear.green) + (SrgbToXyzR2 * linear.blue);
        var y = (SrgbToXyzG0 * linear.red) + (SrgbToXyzG1 * linear.green) + (SrgbToXyzG2 * linear.blue);
        var z = (SrgbToXyzB0 * linear.red) + (SrgbToXyzB1 * linear.green) + (SrgbToXyzB2 * linear.blue);

        var fx = LabF(x / D65WhiteX);
        var fy = LabF(y / D65WhiteY);
        var fz = LabF(z / D65WhiteZ);

        var lightness = (116d * fy) - 16d;
        var a = 500d * (fx - fy);
        var b = 200d * (fy - fz);

        return new Lab(lightness, a, b);
    }

    /// <summary>
    /// Converts a <see cref="Lab"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Lab"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Lab(50d, 20d, 30d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this Lab value)
    {
        var lightness = Math.Clamp(value.Lightness, 0d, 100d);
        var fy = (lightness + 16d) / 116d;
        var fx = fy + (value.A / 500d);
        var fz = fy - (value.B / 200d);

        var fx3 = fx * fx * fx;
        var fy3 = fy * fy * fy;
        var fz3 = fz * fz * fz;

        var xr = fx3 > LabEpsilon ? fx3 : (116d * fx - 16d) / LabKappa;
        var yr = fy3 > LabEpsilon ? fy3 : (116d * fy - 16d) / LabKappa;
        var zr = fz3 > LabEpsilon ? fz3 : (116d * fz - 16d) / LabKappa;

        var x = xr * D65WhiteX;
        var y = yr * D65WhiteY;
        var z = zr * D65WhiteZ;

        var rLinear = (XyzToSrgbR0 * x) + (XyzToSrgbR1 * y) + (XyzToSrgbR2 * z);
        var gLinear = (XyzToSrgbG0 * x) + (XyzToSrgbG1 * y) + (XyzToSrgbG2 * z);
        var bLinear = (XyzToSrgbB0 * x) + (XyzToSrgbB1 * y) + (XyzToSrgbB2 * z);

        return new Rgb(ToSrgbByte(rLinear), ToSrgbByte(gLinear), ToSrgbByte(bLinear));
    }

    /// <summary>
    /// Converts a <see cref="Lab"/> value to <see cref="Lch"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Lab"/> value.</param>
    /// <returns>An <see cref="Lch"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var lch = new Lab(50d, 20d, 30d).ToLch();
    /// </code>
    /// </example>
    public static Lch ToLch(this Lab value)
    {
        var chroma = Math.Sqrt((value.A * value.A) + (value.B * value.B));
        var hue = Math.Atan2(value.B, value.A) * 180d / Math.PI;
        if (hue < 0d)
            hue += 360d;

        return new Lch(value.Lightness, chroma, hue);
    }

    /// <summary>
    /// Converts a <see cref="Lch"/> value to <see cref="Lab"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Lch"/> value.</param>
    /// <returns>An <see cref="Lab"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var lab = new Lch(50d, 20d, 30d).ToLab();
    /// </code>
    /// </example>
    public static Lab ToLab(this Lch value)
    {
        var hue = Math.Clamp(value.Hue, 0d, 360d) * Math.PI / 180d;
        var a = value.Chroma * Math.Cos(hue);
        var b = value.Chroma * Math.Sin(hue);
        return new Lab(value.Lightness, a, b);
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Oklch"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="Oklch"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var oklch = new Rgb(255, 0, 0).ToOklch();
    /// </code>
    /// </example>
    public static Oklch ToOklch(this Rgb value)
    {
        var linear = SrgbToLinear(value);
        var lp = (OklabMatrixLinearsToL(linear.red, linear.green, linear.blue),
            OklabMatrixLinearsToA(linear.red, linear.green, linear.blue),
            OklabMatrixLinearsToB(linear.red, linear.green, linear.blue));

        var l = lp.Item1;
        var a = lp.Item2;
        var b = lp.Item3;
        var chroma = Math.Sqrt((a * a) + (b * b));

        var hue = Math.Atan2(b, a) * 180d / Math.PI;
        if (hue < 0d)
            hue += 360d;

        return new Oklch(Math.Clamp(l, 0d, 1d), chroma, hue);
    }

    /// <summary>
    /// Converts an <see cref="Oklch"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Oklch"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Oklch(0.6d, 0.2d, 90d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this Oklch value)
    {
        var hue = Math.Clamp(value.Hue, 0d, 360d) * Math.PI / 180d;
        var a = value.Chroma * Math.Cos(hue);
        var b = value.Chroma * Math.Sin(hue);

        var l_ = Math.Pow(value.Lightness + (OklabToLinearL1 * a) + (OklabToLinearL2 * b), 3d);
        var m_ = Math.Pow(value.Lightness + (OklabToLinearA1 * a) + (OklabToLinearA2 * b), 3d);
        var s_ = Math.Pow(value.Lightness + (OklabToLinearLms1 * a) + (OklabToLinearLms2 * b), 3d);

        var l = (OklabToLinearR0 * l_) + (OklabToLinearR1 * m_) + (OklabToLinearR2 * s_);
        var g = (OklabToLinearG0 * l_) + (OklabToLinearG1 * m_) + (OklabToLinearG2 * s_);
        var bChannel = (OklabToLinearB0 * l_) + (OklabToLinearB1 * m_) + (OklabToLinearB2 * s_);

        return new Rgb(ToSrgbByte(l), ToSrgbByte(g), ToSrgbByte(bChannel));
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="DisplayP3"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="DisplayP3"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var wide = new Rgb(255, 0, 0).ToDisplayP3();
    /// </code>
    /// </example>
    public static DisplayP3 ToDisplayP3(this Rgb value)
    {
        var linear = SrgbToLinear(value);
        var x = (SrgbToXyzR0 * linear.red) + (SrgbToXyzR1 * linear.green) + (SrgbToXyzR2 * linear.blue);
        var y = (SrgbToXyzG0 * linear.red) + (SrgbToXyzG1 * linear.green) + (SrgbToXyzG2 * linear.blue);
        var z = (SrgbToXyzB0 * linear.red) + (SrgbToXyzB1 * linear.green) + (SrgbToXyzB2 * linear.blue);

        var r = (XyzToDisplayP3R0 * x) + (XyzToDisplayP3R1 * y) + (XyzToDisplayP3R2 * z);
        var g = (XyzToDisplayP3G0 * x) + (XyzToDisplayP3G1 * y) + (XyzToDisplayP3G2 * z);
        var b = (XyzToDisplayP3B0 * x) + (XyzToDisplayP3B1 * y) + (XyzToDisplayP3B2 * z);

        return new DisplayP3(EncodeDisplayP3(r), EncodeDisplayP3(g), EncodeDisplayP3(b));
    }

    /// <summary>
    /// Converts a <see cref="DisplayP3"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="DisplayP3"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new DisplayP3(1d, 0d, 0d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this DisplayP3 value)
    {
        var linear = new
        {
            red = DisplayP3ToLinear(value.R),
            green = DisplayP3ToLinear(value.G),
            blue = DisplayP3ToLinear(value.B),
        };

        var x = (DisplayP3ToXyzR0 * linear.red) + (DisplayP3ToXyzR1 * linear.green) + (DisplayP3ToXyzR2 * linear.blue);
        var y = (DisplayP3ToXyzG0 * linear.red) + (DisplayP3ToXyzG1 * linear.green) + (DisplayP3ToXyzG2 * linear.blue);
        var z = (DisplayP3ToXyzB0 * linear.red) + (DisplayP3ToXyzB1 * linear.green) + (DisplayP3ToXyzB2 * linear.blue);

        var r = (XyzToSrgbR0 * x) + (XyzToSrgbR1 * y) + (XyzToSrgbR2 * z);
        var g = (XyzToSrgbG0 * x) + (XyzToSrgbG1 * y) + (XyzToSrgbG2 * z);
        var b = (XyzToSrgbB0 * x) + (XyzToSrgbB1 * y) + (XyzToSrgbB2 * z);

        return new Rgb(ToSrgbByte(r), ToSrgbByte(g), ToSrgbByte(b));
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Rec2020"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>A <see cref="Rec2020"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var next = new Rgb(1, 2, 3).ToRec2020();
    /// </code>
    /// </example>
    public static Rec2020 ToRec2020(this Rgb value)
    {
        var linear = SrgbToLinear(value);
        var x = (SrgbToXyzR0 * linear.red) + (SrgbToXyzR1 * linear.green) + (SrgbToXyzR2 * linear.blue);
        var y = (SrgbToXyzG0 * linear.red) + (SrgbToXyzG1 * linear.green) + (SrgbToXyzG2 * linear.blue);
        var z = (SrgbToXyzB0 * linear.red) + (SrgbToXyzB1 * linear.green) + (SrgbToXyzB2 * linear.blue);

        var r = (XyzToRec2020R0 * x) + (XyzToRec2020R1 * y) + (XyzToRec2020R2 * z);
        var g = (XyzToRec2020G0 * x) + (XyzToRec2020G1 * y) + (XyzToRec2020G2 * z);
        var b = (XyzToRec2020B0 * x) + (XyzToRec2020B1 * y) + (XyzToRec2020B2 * z);

        return new Rec2020(EncodeRec2020(r), EncodeRec2020(g), EncodeRec2020(b));
    }

    /// <summary>
    /// Converts a <see cref="Rec2020"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rec2020"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Rec2020(0.5d, 0.5d, 0.5d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this Rec2020 value)
    {
        var linear = new
        {
            red = Rec2020ToLinear(value.R),
            green = Rec2020ToLinear(value.G),
            blue = Rec2020ToLinear(value.B),
        };

        var x = (Rec2020ToXyzR0 * linear.red) + (Rec2020ToXyzR1 * linear.green) + (Rec2020ToXyzR2 * linear.blue);
        var y = (Rec2020ToXyzG0 * linear.red) + (Rec2020ToXyzG1 * linear.green) + (Rec2020ToXyzG2 * linear.blue);
        var z = (Rec2020ToXyzB0 * linear.red) + (Rec2020ToXyzB1 * linear.green) + (Rec2020ToXyzB2 * linear.blue);

        var r = (XyzToSrgbR0 * x) + (XyzToSrgbR1 * y) + (XyzToSrgbR2 * z);
        var g = (XyzToSrgbG0 * x) + (XyzToSrgbG1 * y) + (XyzToSrgbG2 * z);
        var b = (XyzToSrgbB0 * x) + (XyzToSrgbB1 * y) + (XyzToSrgbB2 * z);

        return new Rgb(ToSrgbByte(r), ToSrgbByte(g), ToSrgbByte(b));
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="AdobeRgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="AdobeRgb"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var adobe = new Rgb(200, 150, 100).ToAdobeRgb();
    /// </code>
    /// </example>
    public static AdobeRgb ToAdobeRgb(this Rgb value)
    {
        var linear = SrgbToLinear(value);
        var x = (SrgbToXyzR0 * linear.red) + (SrgbToXyzR1 * linear.green) + (SrgbToXyzR2 * linear.blue);
        var y = (SrgbToXyzG0 * linear.red) + (SrgbToXyzG1 * linear.green) + (SrgbToXyzG2 * linear.blue);
        var z = (SrgbToXyzB0 * linear.red) + (SrgbToXyzB1 * linear.green) + (SrgbToXyzB2 * linear.blue);

        var r = (XyzToAdobeR0 * x) + (XyzToAdobeR1 * y) + (XyzToAdobeR2 * z);
        var g = (XyzToAdobeG0 * x) + (XyzToAdobeG1 * y) + (XyzToAdobeG2 * z);
        var b = (XyzToAdobeB0 * x) + (XyzToAdobeB1 * y) + (XyzToAdobeB2 * z);

        return new AdobeRgb(EncodeAdobe(r), EncodeAdobe(g), EncodeAdobe(b));
    }

    /// <summary>
    /// Converts an <see cref="AdobeRgb"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="AdobeRgb"/> value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new AdobeRgb(0.5d, 0.4d, 0.3d).ToRgb();
    /// </code>
    /// </example>
    public static Rgb ToRgb(this AdobeRgb value)
    {
        var linear = new
        {
            red = AdobeToLinear(value.R),
            green = AdobeToLinear(value.G),
            blue = AdobeToLinear(value.B),
        };

        var x = (AdobeToXyzR0 * linear.red) + (AdobeToXyzR1 * linear.green) + (AdobeToXyzR2 * linear.blue);
        var y = (AdobeToXyzG0 * linear.red) + (AdobeToXyzG1 * linear.green) + (AdobeToXyzG2 * linear.blue);
        var z = (AdobeToXyzB0 * linear.red) + (AdobeToXyzB1 * linear.green) + (AdobeToXyzB2 * linear.blue);

        var r = (XyzToSrgbR0 * x) + (XyzToSrgbR1 * y) + (XyzToSrgbR2 * z);
        var g = (XyzToSrgbG0 * x) + (XyzToSrgbG1 * y) + (XyzToSrgbG2 * z);
        var b = (XyzToSrgbB0 * x) + (XyzToSrgbB1 * y) + (XyzToSrgbB2 * z);

        return new Rgb(ToSrgbByte(r), ToSrgbByte(g), ToSrgbByte(b));
    }

    /// <summary>
    /// Converts a <see cref="Rec2020"/> value to <see cref="DisplayP3"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rec2020"/> value.</param>
    /// <returns>A <see cref="DisplayP3"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var wide = new Rec2020(0.5d, 0.4d, 0.3d).ToDisplayP3();
    /// </code>
    /// </example>
    public static DisplayP3 ToDisplayP3(this Rec2020 value)
    {
        return value.ToRgb().ToDisplayP3();
    }

    /// <summary>
    /// Converts a <see cref="DisplayP3"/> value to <see cref="Rec2020"/>.
    /// </summary>
    /// <param name="value">The source <see cref="DisplayP3"/> value.</param>
    /// <returns>A <see cref="Rec2020"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rec = new DisplayP3(0.5d, 0.4d, 0.3d).ToRec2020();
    /// </code>
    /// </example>
    public static Rec2020 ToRec2020(this DisplayP3 value)
    {
        return value.ToRgb().ToRec2020();
    }

    /// <summary>
    /// Converts a <see cref="DisplayP3"/> value to <see cref="AdobeRgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="DisplayP3"/> value.</param>
    /// <returns>An <see cref="AdobeRgb"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var adobe = new DisplayP3(0.5d, 0.4d, 0.3d).ToAdobeRgb();
    /// </code>
    /// </example>
    public static AdobeRgb ToAdobeRgb(this DisplayP3 value)
    {
        return value.ToRgb().ToAdobeRgb();
    }

    /// <summary>
    /// Converts an <see cref="AdobeRgb"/> value to <see cref="DisplayP3"/>.
    /// </summary>
    /// <param name="value">The source <see cref="AdobeRgb"/> value.</param>
    /// <returns>A <see cref="DisplayP3"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var wide = new AdobeRgb(0.5d, 0.4d, 0.3d).ToDisplayP3();
    /// </code>
    /// </example>
    public static DisplayP3 ToDisplayP3(this AdobeRgb value)
    {
        return value.ToRgb().ToDisplayP3();
    }

    /// <summary>
    /// Converts an <see cref="AdobeRgb"/> value to <see cref="Rec2020"/>.
    /// </summary>
    /// <param name="value">The source <see cref="AdobeRgb"/> value.</param>
    /// <returns>A <see cref="Rec2020"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rec = new AdobeRgb(0.5d, 0.4d, 0.3d).ToRec2020();
    /// </code>
    /// </example>
    public static Rec2020 ToRec2020(this AdobeRgb value)
    {
        return value.ToRgb().ToRec2020();
    }

    /// <summary>
    /// Converts a <see cref="Rec2020"/> value to <see cref="AdobeRgb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Rec2020"/> value.</param>
    /// <returns>An <see cref="AdobeRgb"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var adobe = new Rec2020(0.5d, 0.4d, 0.3d).ToAdobeRgb();
    /// </code>
    /// </example>
    public static AdobeRgb ToAdobeRgb(this Rec2020 value)
    {
        return value.ToRgb().ToAdobeRgb();
    }

    /// <summary>
    /// Converts a <see cref="Hwb"/> value to <see cref="Hsl"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Hwb"/> value.</param>
    /// <returns>An <see cref="Hsl"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var hsl = new Hwb(120d, 0.3d, 0.4d).ToHsl();
    /// </code>
    /// </example>
    public static Hsl ToHsl(this Hwb value)
    {
        return value.ToRgb().ToHsl();
    }

    /// <summary>
    /// Converts a <see cref="Hsl"/> value to <see cref="Hwb"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Hsl"/> value.</param>
    /// <returns>An <see cref="Hwb"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var hwb = new Hsl(120d, 1d, 0.5d).ToHwb();
    /// </code>
    /// </example>
    public static Hwb ToHwb(this Hsl value)
    {
        return value.ToRgb().ToHwb();
    }

    /// <summary>
    /// Converts an <see cref="Rgb"/> value to <see cref="Lch"/> through CIELAB.
    /// </summary>
    /// <param name="value">The source <see cref="Rgb"/> value.</param>
    /// <returns>An <see cref="Lch"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var lch = new Rgb(80, 90, 100).ToLch();
    /// </code>
    /// </example>
    public static Lch ToLch(this Rgb value)
    {
        return value.ToLab().ToLch();
    }

    /// <summary>
    /// Converts an <see cref="Hsl"/> value to <see cref="Oklch"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Hsl"/> value.</param>
    /// <returns>An <see cref="Oklch"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var oklch = new Hsl(120d, 1d, 0.5d).ToOklch();
    /// </code>
    /// </example>
    public static Oklch ToOklch(this Hsl value)
    {
        return value.ToRgb().ToOklch();
    }

    /// <summary>
    /// Converts an <see cref="Oklch"/> value to <see cref="Hsl"/>.
    /// </summary>
    /// <param name="value">The source <see cref="Oklch"/> value.</param>
    /// <returns>An <see cref="Hsl"/> value equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var hsl = new Oklch(0.5d, 0.2d, 90d).ToHsl();
    /// </code>
    /// </example>
    public static Hsl ToHsl(this Oklch value)
    {
        return value.ToRgb().ToHsl();
    }

    private static (double r, double g, double b) HueToRgb(double value)
    {
        var rgb = (value: value, red: 0d, green: 0d, blue: 0d);
        var red = HueToRgb(0d, 1d, rgb.value + (1d / 3d));
        var green = HueToRgb(0d, 1d, rgb.value);
        var blue = HueToRgb(0d, 1d, rgb.value - (1d / 3d));

        return (red, green, blue);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0d)
            t += 1d;
        if (t > 1d)
            t -= 1d;

        if (t < 1d / 6d)
            return p + ((q - p) * 6d * t);

        if (t < 1d / 2d)
            return q;

        if (t < 2d / 3d)
            return p + ((q - p) * (2d / 3d - t) * 6d);

        return p;
    }

    private static (double red, double green, double blue) SrgbToLinear(Rgb value)
    {
        return (
            SrgbToLinear(value.R / 255d),
            SrgbToLinear(value.G / 255d),
            SrgbToLinear(value.B / 255d));
    }

    private static double SrgbToLinear(double value)
    {
        value = Math.Clamp(value, 0d, 1d);
        return value <= 0.04045d
            ? value / 12.92d
            : Math.Pow((value + 0.055d) / 1.055d, 2.4d);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(Math.Clamp(value, 0d, 1d) * 255d, MidpointRounding.AwayFromZero);
    }

    private static byte ToSrgbByte(double linear)
    {
        return ToByte(SrgbToEncoded(linear));
    }

    private static double SrgbToEncoded(double linear)
    {
        linear = Math.Max(0d, linear);
        var value = linear <= 0.0031308d
            ? linear * 12.92d
            : 1.055d * Math.Pow(linear, 1d / 2.4d) - 0.055d;

        return Math.Clamp(value, 0d, 1d);
    }

    private static double LabF(double t)
    {
        return t > LabEpsilon
            ? Math.Pow(t, 1d / 3d)
            : (LabKappa * t + 16d) / 116d;
    }

    private static double OklabMatrixLinearsToL(double red, double green, double blue)
    {
        var l = (0.4122214708d * red) + (0.5363325363d * green) + (0.0514459929d * blue);
        var m = (0.2119034982d * red) + (0.6806995451d * green) + (0.1073969566d * blue);
        var s = (0.0883024619d * red) + (0.2817188376d * green) + (0.6299787005d * blue);

        return (LinearToOklabL0 * Math.Cbrt(l)) + (LinearToOklabL1 * Math.Cbrt(m)) + (LinearToOklabL2 * Math.Cbrt(s));
    }

    private static double OklabMatrixLinearsToA(double red, double green, double blue)
    {
        var l = (0.4122214708d * red) + (0.5363325363d * green) + (0.0514459929d * blue);
        var m = (0.2119034982d * red) + (0.6806995451d * green) + (0.1073969566d * blue);
        var s = (0.0883024619d * red) + (0.2817188376d * green) + (0.6299787005d * blue);

        return (LinearToOklabA0 * Math.Cbrt(l)) + (LinearToOklabA1 * Math.Cbrt(m)) + (LinearToOklabA2 * Math.Cbrt(s));
    }

    private static double OklabMatrixLinearsToB(double red, double green, double blue)
    {
        var l = (0.4122214708d * red) + (0.5363325363d * green) + (0.0514459929d * blue);
        var m = (0.2119034982d * red) + (0.6806995451d * green) + (0.1073969566d * blue);
        var s = (0.0883024619d * red) + (0.2817188376d * green) + (0.6299787005d * blue);

        return (LinearToOklabB0 * Math.Cbrt(l)) + (LinearToOklabB1 * Math.Cbrt(m)) + (LinearToOklabB2 * Math.Cbrt(s));
    }

    private static double DisplayP3ToLinear(double encoded)
    {
        var value = Math.Clamp(encoded, 0d, 1d);
        return Math.Pow(value, 2.2d);
    }

    private static double EncodeDisplayP3(double linear)
    {
        return Math.Clamp(Math.Pow(Math.Clamp(linear, 0d, 1d), 1d / 2.2d), 0d, 1d);
    }

    private static double Rec2020ToLinear(double encoded)
    {
        var value = Math.Clamp(encoded, 0d, 1d);
        return value < 0.08125d
            ? value / 4.5d
            : Math.Pow((value + 0.099d) / 1.099d, Rec2020InvGamma);
    }

    private static double EncodeRec2020(double linear)
    {
        var value = Math.Clamp(linear, 0d, 1d);
        return value <= 0.018053968510807d
            ? value * 4.5d
            : 1.099d * Math.Pow(value, Rec2020Gamma) - 0.099d;
    }

    private static double AdobeToLinear(double encoded)
    {
        return Math.Pow(Math.Clamp(encoded, 0d, 1d), 2.2d);
    }

    private static double EncodeAdobe(double linear)
    {
        return Math.Clamp(Math.Pow(Math.Clamp(linear, 0d, 1d), 1d / 2.2d), 0d, 1d);
    }
}