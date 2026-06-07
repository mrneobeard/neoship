namespace NeoBeard.Colors;

/// <summary>
/// Represents an RGBA color value with red, green, blue, and alpha channels in 0xRRGGBBAA format.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Rgba(255, 128, 64, 200);
/// Console.WriteLine($"R: {color.R}, G: {color.G}, B: {color.B}, A: {color.A}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Rgba : IRgba, IEquatable<Rgba>
{
    private readonly uint value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct with opaque black.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba();
    /// Assert.Equal(0, color.R);
    /// Assert.Equal(0, color.G);
    /// Assert.Equal(0, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba()
        : this(0x000000FF)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct.
    /// </summary>
    /// <param name="value">The packed RGBA value in 0xRRGGBBAA format.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(0xFF8040C8);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(uint value)
    {
        this.value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct from an <see cref="IRgb"/> with opaque alpha.
    /// </summary>
    /// <param name="rgb">The RGB color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgb rgb = new Rgb(255, 128, 64);
    /// var color = new Rgba(rgb);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(IRgb rgb)
    {
        this.value = (uint)((rgb.R << 24) | (rgb.G << 16) | (rgb.B << 8) | 0xFF);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct from an <see cref="IRgba"/>.
    /// </summary>
    /// <param name="rgb">The RGBA color to copy.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgba rgba = new Rgba(255, 128, 64, 200);
    /// var color = new Rgba(rgba);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(IRgba rgb)
    {
        this.value = (uint)((rgb.R << 24) | (rgb.G << 16) | (rgb.B << 8) | (byte)rgb.A);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct with opaque alpha.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(byte r, byte g, byte b)
    {
        this.value = (uint)((r << 24) | (g << 16) | (b << 8) | 0xFF);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="a">The alpha channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(byte r, byte g, byte b, byte a)
    {
        this.value = (uint)((r << 24) | (g << 16) | (b << 8) | a);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgba"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="a">The alpha channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, Alpha.Opaque);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgba(byte r, byte g, byte b, Alpha a)
        : this(r, g, b, (byte)a)
    {
    }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(255, color.R);
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
    /// var color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(128, color.G);
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
    /// var color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B => unchecked((byte)((this.value >> 8) & 0xFF));

    /// <summary>
    /// Gets the alpha channel value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Alpha A => unchecked((byte)(this.value & 0xFF));

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
    /// var color = new Rgba(255, 128, 64, 200);
    /// var (r, g, b, a) = color;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// Assert.Equal(200, a);
    /// </code>
    /// </example>
    /// </remarks>
    public void Deconstruct(out byte r, out byte g, out byte b, out Alpha a)
    {
        r = this.R;
        g = this.G;
        b = this.B;
        a = this.A;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Rgba"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Rgba(255, 128, 64, 200);
    /// var color2 = new Rgba(255, 128, 64, 200);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Rgba other)
    {
        return this.R == other.R && this.G == other.G && this.B == other.B && this.A == other.A;
    }

    /// <summary>
    /// Determines whether this instance equals an <see cref="IRgba"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgba color1 = new Rgba(255, 128, 64, 200);
    /// IRgba color2 = new Rgba(255, 128, 64, 200);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(IRgba? other)
    {
        return this.R == other?.R && this.G == other.G && this.B == other.B && this.A == other.A;
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the object is an <see cref="IRgba"/> and equals this instance; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, 200);
    /// object obj = new Rgba(255, 128, 64, 200);
    /// Assert.True(color.Equals(obj));
    /// Assert.False(color.Equals(null));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        return obj is IRgba other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgba(255, 128, 64, 200);
    /// var hash = color.GetHashCode();
    /// Assert.NotEqual(0, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.R, this.G, this.B, this.A);
    }
}