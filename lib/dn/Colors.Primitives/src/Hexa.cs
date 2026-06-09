using System.Globalization;

namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in hexadecimal format with an alpha channel (0xRRGGBBAA format).
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hexa = new Hexa("#FF8040CC");
/// Console.WriteLine($"R: {hexa.R}, G: {hexa.G}, B: {hexa.B}, A: {hexa.A}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Hexa : IEquatable<Hexa>, IRgb, IRgba
{
    private readonly uint value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct with opaque black.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa();
    /// Assert.Equal(0, hexa.R);
    /// Assert.Equal(0, hexa.G);
    /// Assert.Equal(0, hexa.B);
    /// Assert.Equal(255, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa()
        : this(0x000000FF)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct. The value must
    /// be in the format <c>0xRRGGBBAA</c> for example <c>0xFF0000FF</c> is opaque red.
    /// </summary>
    /// <param name="value">The hexa value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(0xFF8040CC);
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa(uint value)
    {
        this.value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="a">The alpha channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa(byte r, byte g, byte b, byte a)
    {
        this.value = (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="alpha">The alpha channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, Alpha.Opaque);
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(255, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa(byte r, byte g, byte b, Alpha alpha)
        : this(r, g, b, (byte)alpha)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct by parsing a hex string.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040CC" or "FF8040CC").</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa("#FF8040CC");
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa(string value)
        : this(ParseAsUint(value.AsSpan()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hexa"/> struct by parsing a hex string.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040CC" or "FF8040CC").</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa("#FF8040CC".AsSpan());
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Hexa(ReadOnlySpan<char> value)
        : this(ParseAsUint(value))
    {
    }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal(255, hexa.R);
    /// </code>
    /// </example>
    /// </remarks>
    public byte R => unchecked((byte)((this.value >> 24) & 0xFF));

    /// <summary>
    /// Gets the green channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal(128, hexa.G);
    /// </code>
    /// </example>
    /// </remarks>
    public byte G => unchecked((byte)((this.value >> 16) & 0xFF));

    /// <summary>
    /// Gets the blue channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal(64, hexa.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B => unchecked((byte)((this.value >> 8) & 0xFF));

    /// <summary>
    /// Gets the alpha channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public byte A => unchecked((byte)(this.value & 0xFF));

    Alpha IRgba.A => this.A;

    /// <summary>
    /// Parses a hex string into a <see cref="Hexa"/> value.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040CC", "FF8040CC", "#F84", "F84", "#FF8040", or "FF8040").</param>
    /// <returns>The parsed <see cref="Hexa"/> value.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid hex color.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = Hexa.Parse("#FF8040CC");
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static Hexa Parse(string value)
        => Parse(value.AsSpan());

    /// <summary>
    /// Parses a hex string into a <see cref="Hexa"/> value.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040CC", "FF8040CC", "#F84", "F84", "#FF8040", or "FF8040").</param>
    /// <returns>The parsed <see cref="Hexa"/> value.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid hex color.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = Hexa.Parse("#FF8040CC".AsSpan());
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static Hexa Parse(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6 and not 8)
            throw new FormatException("Invalid hex code.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[8])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
                [6] = 'F',
                [7] = 'F',
            };
            value = tmp;
        }

        if (value.Length == 6)
        {
            var tmp = new Span<char>(new char[8])
            {
                [0] = value[0],
                [1] = value[1],
                [2] = value[2],
                [3] = value[3],
                [4] = value[4],
                [5] = value[5],
                [6] = 'F',
                [7] = 'F',
            };

            value = tmp;
        }

#if NETLEGACY
        var r = uint.Parse(value.Slice(0, 2).ToString(), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2).ToString(), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2).ToString(), NumberStyles.HexNumber);
        var a = uint.Parse(value.Slice(6, 2).ToString(), NumberStyles.HexNumber);
        return new Hexa((byte)r, (byte)g, (byte)b, (byte)a);
#else
        var r = uint.Parse(value.Slice(0, 2), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2), NumberStyles.HexNumber);
        var a = uint.Parse(value.Slice(6, 2), NumberStyles.HexNumber);
        return new Hexa((byte)r, (byte)g, (byte)b, (byte)a);
#endif
    }

    /// <summary>
    /// Tries to parse a hex string into a <see cref="Hexa"/> value.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="code">The parsed <see cref="Hexa"/> value if successful.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Hexa.TryParse("#FF8040CC", out var hexa));
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool TryParse(string value, out Hexa code)
        => TryParse(value.AsSpan(), out code);

    /// <summary>
    /// Tries to parse a hex string into a <see cref="Hexa"/> value.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="code">The parsed <see cref="Hexa"/> value if successful.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Hexa.TryParse("#FF8040CC".AsSpan(), out var hexa));
    /// Assert.Equal(255, hexa.R);
    /// Assert.Equal(128, hexa.G);
    /// Assert.Equal(64, hexa.B);
    /// Assert.Equal(204, hexa.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Hexa code)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6 and not 8)
            throw new FormatException("Invalid hex code.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[8])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
                [6] = 'F',
                [7] = 'F',
            };
            value = tmp;
        }

        if (value.Length == 6)
        {
            var tmp = new Span<char>(new char[8])
            {
                [0] = value[0],
                [1] = value[1],
                [2] = value[2],
                [3] = value[3],
                [4] = value[4],
                [5] = value[5],
                [6] = 'F',
                [7] = 'F',
            };

            value = tmp;
        }
#if NETLEGACY

        var str = new string(value.ToArray());
        if (!uint.TryParse(str.Substring(0, 2), NumberStyles.HexNumber, null, out uint r))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(str.Substring(2, 2), NumberStyles.HexNumber, null, out uint g))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(str.Substring(4, 2), NumberStyles.HexNumber, null, out uint b))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(str.Substring(6, 2), NumberStyles.HexNumber, null, out uint a))
        {
            code = default;
            return false;
        }

        code = new Hexa((byte)r, (byte)g, (byte)b, (byte)a);
        return true;
#else
        if (!uint.TryParse(value.Slice(0, 2), NumberStyles.HexNumber, null, out uint r))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(value.Slice(2, 2), NumberStyles.HexNumber, null, out uint g))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(value.Slice(4, 2), NumberStyles.HexNumber, null, out uint b))
        {
            code = default;
            return false;
        }

        if (!uint.TryParse(value.Slice(6, 2), NumberStyles.HexNumber, null, out uint a))
        {
            code = default;
            return false;
        }

        code = new Hexa((byte)r, (byte)g, (byte)b, (byte)a);
        return true;
#endif
    }

    void IRgba.Deconstruct(out byte r, out byte g, out byte b, out Alpha a)
    {
        r = this.R;
        g = this.G;
        b = this.B;
        a = this.A;
    }

    /// <summary>
    /// Deconstructs the color into its RGB components.
    /// </summary>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// var (r, g, b) = hexa;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// </code>
    /// </example>
    /// </remarks>
    public void Deconstruct(out byte r, out byte g, out byte b)
    {
        r = this.R;
        g = this.G;
        b = this.B;
    }

    /// <summary>
    /// Deconstructs the color into its RGBA components.
    /// </summary>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <param name="a">The alpha channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// var (r, g, b, a) = hexa;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// Assert.Equal(204, a);
    /// </code>
    /// </example>
    /// </remarks>
    public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
    {
        r = this.R;
        g = this.G;
        b = this.B;
        a = this.A;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Hexa"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa1 = new Hexa(255, 128, 64, 204);
    /// var hexa2 = new Hexa(255, 128, 64, 204);
    /// Assert.True(hexa1.Equals(hexa2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Hexa other)
        => this.value == other.value;

    /// <summary>
    /// Determines whether this instance equals an <see cref="IRgb"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgb hexa = new Hexa(255, 128, 64, 204);
    /// IRgb other = new Hexa(255, 128, 64, 100);
    /// Assert.True(hexa.Equals(other));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(IRgb? other)
    {
        if (other is null)
            return false;

        return this.R == other.R && this.G == other.G && this.B == other.B;
    }

    bool IEquatable<IRgba>.Equals(IRgba? other)
    {
        if (other is null)
            return false;

        return this.R == other.R && this.G == other.G && this.B == other.B && this.A == other.A;
    }

    /// <summary>
    /// Returns the hex string representation of this color.
    /// </summary>
    /// <returns>An 8-character hex string (e.g., "FF8040CC").</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hexa = new Hexa(255, 128, 64, 204);
    /// Assert.Equal("FF8040CC", hexa.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
    {
        return $"{this.value:X8}";
    }

    private static uint ParseAsUint(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6 and not 8)
            throw new FormatException("Invalid hex code.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[8])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
                [6] = 'F',
                [7] = 'F',
            };
            value = tmp;
        }

        if (value.Length == 6)
        {
            var tmp = new Span<char>(new char[8])
            {
                [0] = value[0],
                [1] = value[1],
                [2] = value[2],
                [3] = value[3],
                [4] = value[4],
                [5] = value[5],
                [6] = 'F',
                [7] = 'F',
            };

            value = tmp;
        }

#if NETLEGACY
        var r = uint.Parse(value.Slice(0, 2).ToString(), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2).ToString(), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2).ToString(), NumberStyles.HexNumber);
        var a = uint.Parse(value.Slice(6, 2).ToString(), NumberStyles.HexNumber);
        return r << 24 | g << 16 | b << 8 | a;
#else
        var r = uint.Parse(value.Slice(0, 2), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2), NumberStyles.HexNumber);
        var a = uint.Parse(value.Slice(6, 2), NumberStyles.HexNumber);
        return r << 24 | g << 16 | b << 8 | a;
#endif
    }
}