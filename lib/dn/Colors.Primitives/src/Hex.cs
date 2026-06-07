using System.Globalization;

namespace NeoBeard.Colors;

/// <summary>
/// Represents a hexadecimal RGB color value without an alpha channel.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hex = new Hex("#FF8040");
/// Console.WriteLine($"R: {hex.R}, G: {hex.G}, B: {hex.B}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Hex
{
    private readonly uint value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hex"/> struct with black (0x000000).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex();
    /// Assert.Equal(0u, hex.Value);
    /// Assert.Equal(0, hex.R);
    /// Assert.Equal(0, hex.G);
    /// Assert.Equal(0, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Hex()
        : this(0x000000)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hex"/> struct.
    /// </summary>
    /// <param name="value">The packed RGB hex value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex(0xFF8040);
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Hex(uint value)
    {
        this.value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hex"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex(255, 128, 64);
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Hex(byte r, byte g, byte b)
    {
        this.value = (uint)((r << 16) | (g << 8) | b);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hex"/> struct by parsing a hex string.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040" or "FF8040").</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex("#FF8040");
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Hex(string value)
        : this(ParseAsUint(value.AsSpan()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Hex"/> struct by parsing a hex string.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040" or "FF8040").</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex("#FF8040".AsSpan());
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Hex(ReadOnlySpan<char> value)
        : this(ParseAsUint(value))
    {
    }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex("#FF8040");
    /// Assert.Equal(255, hex.R);
    /// </code>
    /// </example>
    /// </remarks>
    public byte R => unchecked((byte)((this.value >> 16) & 0xFF));

    /// <summary>
    /// Gets the green channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex("#FF8040");
    /// Assert.Equal(128, hex.G);
    /// </code>
    /// </example>
    /// </remarks>
    public byte G => unchecked((byte)((this.value >> 8) & 0xFF));

    /// <summary>
    /// Gets the blue channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex("#FF8040");
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B => unchecked((byte)(this.value & 0xFF));

    /// <summary>
    /// Parses a hex string into a <see cref="Hex"/> value.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040", "FF8040", "#F84", or "F84").</param>
    /// <returns>The parsed <see cref="Hex"/> value.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid hex color.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = Hex.Parse("#FF8040");
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static Hex Parse(string value)
        => Parse(value.AsSpan());

    /// <summary>
    /// Parses a hex string into a <see cref="Hex"/> value.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040", "FF8040", "#F84", or "F84").</param>
    /// <returns>The parsed <see cref="Hex"/> value.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid hex color.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = Hex.Parse("#FF8040".AsSpan());
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static Hex Parse(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6)
            throw new FormatException($"Invalid hex code {value.AsString()}.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[6])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
            };
            value = tmp;
        }

#if NETLEGACY
        var r = uint.Parse(value.Slice(0, 2).ToString(), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2).ToString(), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2).ToString(), NumberStyles.HexNumber);
        return new Hex((byte)r, (byte)g, (byte)b);
#else
        var r = uint.Parse(value.Slice(0, 2), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2), NumberStyles.HexNumber);
        return new Hex((byte)r, (byte)g, (byte)b);
#endif
    }

    /// <summary>
    /// Parses a hex string into a uint value.
    /// </summary>
    /// <param name="value">The hex string (e.g., "#FF8040", "FF8040", "#F84", or "F84").</param>
    /// <returns>The parsed uint value.</returns>
    /// <exception cref="FormatException">Thrown when the string is not a valid hex color.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = Hex.ParseAsUint("#FF8040");
    /// Assert.Equal(0xFF8040u, result);
    /// </code>
    /// </example>
    /// </remarks>
    public static uint ParseAsUint(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6)
            throw new FormatException($"Invalid hex code {value.AsString()}.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[6])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
            };
            value = tmp;
        }

#if NETLEGACY
        var r = uint.Parse(value.Slice(0, 2).ToString(), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2).ToString(), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2).ToString(), NumberStyles.HexNumber);
        return r << 16 | g << 8 | b;
#else
        var r = uint.Parse(value.Slice(0, 2), NumberStyles.HexNumber);
        var g = uint.Parse(value.Slice(2, 2), NumberStyles.HexNumber);
        var b = uint.Parse(value.Slice(4, 2), NumberStyles.HexNumber);
        return r << 16 | g << 8 | b;
#endif
    }

    /// <summary>
    /// Tries to parse a hex string into a <see cref="Hex"/> value.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="code">The parsed <see cref="Hex"/> value if successful.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Hex.TryParse("#FF8040", out var hex));
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool TryParse(string value, out Hex code)
        => TryParse(value.AsSpan(), out code);

    /// <summary>
    /// Tries to parse a hex string into a <see cref="Hex"/> value.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="code">The parsed <see cref="Hex"/> value if successful.</param>
    /// <returns><c>true</c> if parsing was successful; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Hex.TryParse("#FF8040".AsSpan(), out var hex));
    /// Assert.Equal(255, hex.R);
    /// Assert.Equal(128, hex.G);
    /// Assert.Equal(64, hex.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool TryParse(ReadOnlySpan<char> value, out Hex code)
    {
        if (value.Length == 0)
            throw new FormatException("Invalid hex code. No data.");

        value = value.Trim();
        if (value.StartsWith(new[] { '#' }))
            value = value.Slice(1);

        if (value.Length is not 3 and not 6)
            throw new FormatException($"Invalid hex code {value.AsString()}.");

        if (value.Length == 3)
        {
            var r1 = value[0];
            var g1 = value[1];
            var b1 = value[2];

            var tmp = new Span<char>(new char[6])
            {
                [0] = r1,
                [1] = r1,
                [2] = g1,
                [3] = g1,
                [4] = b1,
                [5] = b1,
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

        code = new Hex((byte)r, (byte)g, (byte)b);
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

        code = new Hex((byte)r, (byte)g, (byte)b);
        return true;
#endif
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
    /// var hex = new Hex("#FF8040");
    /// var (r, g, b) = hex;
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
    /// Returns the hex string representation of this color.
    /// </summary>
    /// <returns>A 6-character hex string (e.g., "FF8040").</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hex = new Hex(255, 128, 64);
    /// Assert.Equal("FF8040", hex.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
    {
        return $"{this.value:X6}";
    }
}