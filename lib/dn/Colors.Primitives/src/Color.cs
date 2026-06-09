namespace NeoBeard.Colors;

/// <summary>
/// Represents a color with red, green, blue, and alpha channels.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Color(255, 128, 64, 200);
/// Console.WriteLine($"R: {color.R}, G: {color.G}, B: {color.B}, A: {color.A}");
/// </code>
/// </example>
/// </remarks>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct from a <see cref="System.Drawing.Color"/>.
    /// </summary>
    /// <param name="color">The system drawing color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var sysColor = System.Drawing.Color.FromArgb(200, 255, 128, 64);
    /// var color = new Color(sysColor);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color(System.Drawing.Color color)
    {
        this.R = color.R;
        this.G = color.G;
        this.B = color.B;
        this.A = color.A;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct with opaque alpha.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color(byte r, byte g, byte b)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = Alpha.Opaque;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct.
    /// </summary>
    /// <param name="r">The red channel value (0-255).</param>
    /// <param name="g">The green channel value (0-255).</param>
    /// <param name="b">The blue channel value (0-255).</param>
    /// <param name="a">The alpha channel value (0-255).</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color(byte r, byte g, byte b, byte a)
    {
        this.R = r;
        this.G = g;
        this.B = b;
        this.A = a;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct with opaque black (0, 0, 0, 255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color();
    /// Assert.Equal(0, color.R);
    /// Assert.Equal(0, color.G);
    /// Assert.Equal(0, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color()
        : this(0, 0, 0, 255)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct from an <see cref="Rgb"/>.
    /// </summary>
    /// <param name="rgb">The RGB color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Rgb(255, 128, 64);
    /// var color = new Color(rgb);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color(Rgb rgb)
        : this(rgb.R, rgb.G, rgb.B)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Color"/> struct from an <see cref="Rgba"/>.
    /// </summary>
    /// <param name="rgba">The RGBA color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var rgba = new Rgba(255, 128, 64, 200);
    /// var color = new Color(rgba);
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Color(Rgba rgba)
        : this(rgba.R, rgba.G, rgba.B, rgba.A)
    {
    }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// Assert.Equal(255, color.R);
    /// </code>
    /// </example>
    /// </remarks>
    public byte R { get; }

    /// <summary>
    /// Gets the green channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// Assert.Equal(128, color.G);
    /// </code>
    /// </example>
    /// </remarks>
    public byte G { get; }

    /// <summary>
    /// Gets the blue channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    public byte B { get; }

    /// <summary>
    /// Gets the alpha channel value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Alpha A { get; }

    /// <summary>
    /// Converts an <see cref="Rgb"/> to a <see cref="Color"/>.
    /// </summary>
    /// <param name="rgb">The RGB color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Rgb rgb = new Rgb(255, 128, 64);
    /// Color color = rgb;
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Color(Rgb rgb)
    {
        return new Color(rgb);
    }

    /// <summary>
    /// Converts an <see cref="Rgba"/> to a <see cref="Color"/>.
    /// </summary>
    /// <param name="rgba">The RGBA color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Rgba rgba = new Rgba(255, 128, 64, 200);
    /// Color color = rgba;
    /// Assert.Equal(255, color.R);
    /// Assert.Equal(128, color.G);
    /// Assert.Equal(64, color.B);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Color(Rgba rgba)
    {
        return new Color(rgba);
    }

    /// <summary>
    /// Converts a <see cref="Color"/> to a <see cref="System.Drawing.Color"/>.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// System.Drawing.Color sysColor = color;
    /// Assert.Equal(255, sysColor.R);
    /// Assert.Equal(128, sysColor.G);
    /// Assert.Equal(64, sysColor.B);
    /// Assert.Equal(200, sysColor.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator System.Drawing.Color(Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    /// <summary>
    /// Determines whether two <see cref="Color"/> instances are equal.
    /// </summary>
    /// <param name="left">The first color to compare.</param>
    /// <param name="right">The second color to compare.</param>
    /// <returns><c>true</c> if the colors are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Color(255, 128, 64, 200);
    /// var color2 = new Color(255, 128, 64, 200);
    /// Assert.True(color1 == color2);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator ==(Color left, Color right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="Color"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first color to compare.</param>
    /// <param name="right">The second color to compare.</param>
    /// <returns><c>true</c> if the colors are not equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Color(255, 128, 64, 200);
    /// var color2 = new Color(100, 50, 25, 100);
    /// Assert.True(color1 != color2);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator !=(Color left, Color right)
    {
        return !left.Equals(right);
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
    /// var color = new Color(255, 128, 64, 200);
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
    /// Deconstruct the color into its RGBA components.
    /// </summary>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <param name="a">The alpha channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// var (r, g, b, a) = color;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// Assert.Equal(200, a);
    /// </code>
    /// </example>
    /// </remarks>
    public void Deconstruct(out byte r, out byte g, out byte b, out int a)
    {
        r = this.R;
        g = this.G;
        b = this.B;
        a = this.A;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Color"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color1 = new Color(255, 128, 64, 200);
    /// var color2 = new Color(255, 128, 64, 200);
    /// Assert.True(color1.Equals(color2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Color other)
    {
        return this.R == other.R && this.G == other.G && this.B == other.B && this.A == other.A;
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the object is a <see cref="Color"/> and equals this instance; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
    /// object obj = new Color(255, 128, 64, 200);
    /// Assert.True(color.Equals(obj));
    /// Assert.False(color.Equals(null));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        return obj is Color other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Color(255, 128, 64, 200);
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