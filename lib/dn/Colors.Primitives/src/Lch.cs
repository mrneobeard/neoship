namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in CIELCH color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var lch = new Lch(60d, 40d, 30d);
/// var lab = lch.ToLab();
/// </code>
/// </example>
/// </remarks>
public readonly struct Lch : IEquatable<Lch>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double lightness;

    private readonly double chroma;

    private readonly double hue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Lch"/> struct.
    /// </summary>
    /// <param name="lightness">The lightness component in [0, 100].</param>
    /// <param name="chroma">The chroma component, clamped to 0 or greater.</param>
    /// <param name="hue">The hue angle in degrees.</param>
    /// <returns>A newly initialized <see cref="Lch"/> value.</returns>
    public Lch(double lightness, double chroma, double hue)
    {
        this.lightness = Math.Clamp(lightness, 0d, 100d);
        this.chroma = Math.Max(chroma, 0d);
        this.hue = NormalizeHue(hue);
    }

    /// <summary>
    /// Gets the CIELCH lightness component in [0, 100].
    /// </summary>
    /// <returns>The lightness value.</returns>
    public double Lightness => this.lightness;

    /// <summary>
    /// Gets the CIELCH chroma component.
    /// </summary>
    /// <returns>The chroma value.</returns>
    public double Chroma => this.chroma;

    /// <summary>
    /// Gets the CIELCH hue component in degrees.
    /// </summary>
    /// <returns>The hue value.</returns>
    public double Hue => this.hue;

    /// <summary>
    /// Deconstructs the color into LCH components.
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
    /// Determines whether this instance equals another <see cref="Lch"/> instance.
    /// </summary>
    /// <param name="other">The value to compare.</param>
    /// <returns><see langword="true"/> when each LCH component is within tolerance; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var lch1 = new Lch(60d, 30d, 180d);
    /// var lch2 = new Lch(60d, 30d, 180d);
    /// Assert.True(lch1.Equals(lch2));
    /// </code>
    /// </example>
    public bool Equals(Lch other)
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
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Lch"/> and matches this value; otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Lch other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code for this <see cref="Lch"/> value.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.lightness, this.chroma, this.hue);
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