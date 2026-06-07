namespace NeoBeard.Colors;

/// <summary>
/// Represents an RGB color value without an alpha channel.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Rgb(255, 128, 64);
/// Console.WriteLine($"R: {color.R}, G: {color.G}, B: {color.B}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Rgb : IEquatable<Rgb>
{
    private readonly uint value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgb"/> struct with black (0, 0, 0).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb();
    /// Assert.Equal(0, color.R);
    /// Assert.Equal(0, color.G);
    /// Assert.Equal(0, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgb()
    {
        this.value = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgb"/> struct.
    /// </summary>
    /// <param name="value">The packed RGB value where R is bits 16-23, G is bits 8-15, and B is bits 0-7.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(0xFF8040);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgb(uint value)
    {
        this.value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Rgb"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public Rgb(byte r, byte g, byte b)
    {
        this.value = (uint)((r << 16) | (g << 8) | b);
    }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 128, 64);
    /// Assert.Equal(255, color.R);
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
    /// var color = new Rgb(255, 128, 64);
    /// Assert.Equal(128, color.G);
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
    /// var color = new Rgb(255, 128, 64);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B => unchecked((byte)(this.value & 0xFF));

    /// <summary>
    /// Converts an <see cref="Rgba"/> to an <see cref="Rgb"/>, discarding the alpha channel.
    /// </summary>
    /// <param name="rgba">The RGBA color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Rgba rgba = new Rgba(255, 128, 64, 200);
    /// Rgb rgb = rgba;
    /// Assert.Equal(255, rgb.R);
    /// Assert.Equal(128, rgb.G);
    /// Assert.Equal(64, rgb.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Rgb(Rgba rgba)
    {
        return new Rgb(rgba.R, rgba.G, rgba.B);
    }

    /// <summary>
    /// Converts a <see cref="Color"/> to an <see cref="Rgb"/>.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Color color = new Color(255, 128, 64, 200);
    /// Rgb rgb = color;
    /// Assert.Equal(255, rgb.R);
    /// Assert.Equal(128, rgb.G);
    /// Assert.Equal(64, rgb.B);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Rgb(Color color)
    {
        return new Rgb(color.R, color.G, color.B);
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
    /// var color = new Rgb(255, 128, 64);
    /// var (r, g, b) = color;
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
    /// Determines whether this instance equals another <see cref="Rgb"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Rgb(255, 128, 64);
    /// var color2 = new Rgb(255, 128, 64);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Rgb other)
    {
        return this.R == other.R && this.G == other.G && this.B == other.B;
    }
}