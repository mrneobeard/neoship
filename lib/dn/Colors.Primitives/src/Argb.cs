namespace NeoBeard.Colors;

/// <summary>
/// Represents an ARGB color value with alpha channel first (0xAARRGGBB format).
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Argb(255, 128, 64, 32);
/// Console.WriteLine($"A: {color.A}, R: {color.R}, G: {color.G}, B: {color.B}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Argb : IArgb, IEquatable<Argb>
{
    /// <summary>
    /// Gets the number of bits to shift the alpha component into the packed value.
    /// </summary>
    internal const int AlphaShift = 24;

    /// <summary>
    /// Gets the number of bits to shift the red component into the packed value.
    /// </summary>
    internal const int RedShift = 16;

    /// <summary>
    /// Gets the number of bits to shift the green component into the packed value.
    /// </summary>
    internal const int GreenShift = 8;

    /// <summary>
    /// Gets the number of bits to shift the blue component into the packed value.
    /// </summary>
    internal const int BlueShift = 0;

    /// <summary>
    /// Gets the mask used to isolate the alpha bits.
    /// </summary>
    internal const uint AlphaMask = 0xFFu << AlphaShift;

    /// <summary>
    /// Gets the mask used to isolate the red bits.
    /// </summary>
    internal const uint RedMask = 0xFFu << RedShift;

    /// <summary>
    /// Gets the mask used to isolate the green bits.
    /// </summary>
    internal const uint GreenMask = 0xFFu << GreenShift;

    /// <summary>
    /// Gets the mask used to isolate the blue bits.
    /// </summary>
    internal const uint BlueMask = 0xFFu << BlueShift;

    private readonly long value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Argb"/> struct from a packed 0xAARRGGBB value.
    /// </summary>
    /// <param name="value">The packed ARGB value in 0xAARRGGBB format.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is negative or exceeds 0xFFFFFFFF.</exception>
    /// <returns>A new <see cref="Argb"/> value with the supplied packed value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(0xFF804020);
    /// Assert.Equal(255, color.A);
    /// Assert.Equal(128, color.R);
    /// Assert.Equal(64, color.G);
    /// Assert.Equal(32, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Argb(long value)
    {
        if (value < 0 || value > 0xFFFFFFFF)
            throw new ArgumentOutOfRangeException(nameof(value));

        this.value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Argb"/> struct with component values.
    /// </summary>
    /// <param name="a">The alpha channel value (0-255).</param>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <returns>A new <see cref="Argb"/> value from ARGB components.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(255, color.A);
    /// Assert.Equal(128, color.R);
    /// Assert.Equal(64, color.G);
    /// Assert.Equal(32, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Argb(byte a, byte r, byte g, byte b)
    {
        this.value = (a << AlphaShift) | (r << RedShift) | (g << GreenShift) | (b << BlueShift);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Argb"/> struct with transparent black (0, 0, 0, 0).
    /// </summary>
    /// <returns>A new <see cref="Argb"/> value initialized to zero.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb();
    /// Assert.Equal(0, color.A);
    /// Assert.Equal(0, color.R);
    /// Assert.Equal(0, color.G);
    /// Assert.Equal(0, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Argb()
        : this(0, 0, 0, 0)
    {
    }

    /// <summary>
    /// Gets the alpha channel value (0-255).
    /// </summary>
    /// <returns>The alpha byte.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public byte A => unchecked((byte)(this.value >> AlphaShift));

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <returns>The red byte.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(128, color.R);
    /// </code>
    /// </example>
    /// </remarks>
    public byte R => unchecked((byte)(this.value >> RedShift));

    /// <summary>
    /// Gets the green channel value (0-255).
    /// </summary>
    /// <returns>The green byte.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(64, color.G);
    /// </code>
    /// </example>
    /// </remarks>
    public byte G => unchecked((byte)(this.value >> GreenShift));

    /// <summary>
    /// Gets the blue channel value (0-255).
    /// </summary>
    /// <returns>The blue byte.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(32, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B => unchecked((byte)(this.value >> BlueShift));

    /// <summary>
    /// Deconstructs the color into its ARGB components.
    /// </summary>
    /// <param name="a">The alpha channel value.</param>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <returns>No value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// var (a, r, g, b) = color;
    /// Assert.Equal(255, a);
    /// Assert.Equal(128, r);
    /// Assert.Equal(64, g);
    /// Assert.Equal(32, b);
    /// </code>
    /// </example>
    /// </remarks>
    public void Deconstruct(out byte a, out byte r, out byte g, out byte b)
    {
        a = this.A;
        r = this.R;
        g = this.G;
        b = this.B;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Argb"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Argb(255, 128, 64, 32);
    /// var color2 = new Argb(255, 128, 64, 32);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Argb other)
    {
        return this.value == other.value;
    }

    /// <summary>
    /// Determines whether this instance equals an <see cref="IArgb"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><see langword="true"/> if channel values are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color1 = new Argb(255, 128, 64, 32);
    /// IArgb color2 = new Argb(255, 128, 64, 32);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(IArgb? other)
    {
        return this.R == other?.R && this.G == other.G && this.B == other.B && this.A == other.A;
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if the object is an <see cref="IArgb"/> and equals this instance; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// object obj = new Argb(255, 128, 64, 32);
    /// Assert.True(color.Equals(obj));
    /// Assert.False(color.Equals(null));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        return obj is IArgb other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Argb(255, 128, 64, 32);
    /// var hash = color.GetHashCode();
    /// Assert.NotEqual(0, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public override int GetHashCode()
    {
        return this.value.GetHashCode();
    }
}
