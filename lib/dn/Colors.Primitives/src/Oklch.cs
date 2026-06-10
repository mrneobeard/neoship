namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in OKLCH color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var oklch = new Oklch(0.6d, 0.2d, 30d);
/// var rgb = oklch.ToRgb();
/// </code>
/// </example>
/// </remarks>
public readonly struct Oklch : IEquatable<Oklch>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double lightness;

    private readonly double chroma;

    private readonly double hue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Oklch"/> struct.
    /// </summary>
    /// <param name="lightness">The lightness component in [0, 1].</param>
    /// <param name="chroma">The chroma component, clamped to 0 or greater.</param>
    /// <param name="hue">The hue angle in degrees.</param>
    /// <returns>A newly initialized <see cref="Oklch"/> value.</returns>
    public Oklch(double lightness, double chroma, double hue)
    {
        this.lightness = Math.Clamp(lightness, 0d, 1d);
        this.chroma = Math.Max(chroma, 0d);
        this.hue = NormalizeHue(hue);
    }

    /// <summary>
    /// Gets the OKLCH lightness component in [0, 1].
    /// </summary>
    /// <returns>The lightness value.</returns>
    public double Lightness => this.lightness;

    /// <summary>
    /// Gets the OKLCH chroma component.
    /// </summary>
    /// <returns>The chroma value.</returns>
    public double Chroma => this.chroma;

    /// <summary>
    /// Gets the OKLCH hue component in degrees.
    /// </summary>
    /// <returns>The hue value.</returns>
    public double Hue => this.hue;

    /// <summary>
    /// Converts this OKLCH color to <see cref="Rgb"/>.
    /// </summary>
    /// <returns>An <see cref="Rgb"/> equivalent to this <see cref="Oklch"/> value.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Oklch(0.5d, 0.2d, 90d).ToRgb();
    /// </code>
    /// </example>
    public Rgb ToRgb()
    {
        return ColorSpaceConversions.ToRgb(this);
    }

    /// <summary>
    /// Deconstructs the color into OKLCH components.
    /// </summary>
    /// <param name="lightness">The lightness component.</param>
    /// <param name="chroma">The chroma component.</param>
    /// <param name="hue">The hue component.</param>
    /// <returns>No value.</returns>
    public void Deconstruct(out double lightness, out double chroma, out double hue)
    {
        lightness = this.lightness;
        chroma = this.chroma;
        hue = this.hue;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Oklch"/> instance.
    /// </summary>
    /// <param name="other">The value to compare.</param>
    /// <returns><see langword="true"/> when each OKLCH component is within tolerance; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var a = new Oklch(0.5d, 0.2d, 90d);
    /// var b = new Oklch(0.5d, 0.2d, 90d);
    /// Assert.True(a.Equals(b));
    /// </code>
    /// </example>
    public bool Equals(Oklch other)
    {
        return AreClose(this.lightness, other.lightness)
            && AreClose(this.chroma, other.chroma)
            && AreClose(this.hue, other.hue);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an <see cref="Oklch"/> and equals this value; otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Oklch other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code for this <see cref="Oklch"/> value.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.lightness, this.chroma, this.hue);
    }

    /// <summary>
    /// Converts this <see cref="Oklch"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source OKLCH value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// Rgb rgb = new Oklch(0.5d, 0.2d, 90d);
    /// </code>
    /// </example>
    public static implicit operator Rgb(Oklch value)
    {
        return value.ToRgb();
    }

    /// <summary>
    /// Normalizes a hue angle to the [0, 360) range.
    /// </summary>
    /// <param name="value">The hue angle in degrees.</param>
    /// <returns>The normalized hue angle.</returns>
    private static double NormalizeHue(double value)
    {
        value %= 360d;
        if (value < 0d)
            value += 360d;
        return value;
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) <= DoubleEqualityTolerance;
    }
}