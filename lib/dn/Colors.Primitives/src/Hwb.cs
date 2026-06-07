namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in the HWB (hue, whiteness, blackness) color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hwb = new Hwb(240d, 0d, 0.5d);
/// var rgb = hwb.ToRgb();
/// </code>
/// </example>
/// </remarks>
public readonly struct Hwb : IEquatable<Hwb>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double hue;

    private readonly double whiteness;

    private readonly double blackness;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hwb"/> struct.
    /// </summary>
    /// <param name="hue">The hue angle in degrees.</param>
    /// <param name="whiteness">The whiteness amount, clamped to [0, 1].</param>
    /// <param name="blackness">The blackness amount, clamped to [0, 1].</param>
    /// <returns>A newly initialized <see cref="Hwb"/> value.</returns>
    public Hwb(double hue, double whiteness, double blackness)
    {
        this.hue = NormalizeHue(hue);
        this.whiteness = Math.Clamp(whiteness, 0d, 1d);
        this.blackness = Math.Clamp(blackness, 0d, 1d);
    }

    /// <summary>
    /// Gets the hue component in degrees.
    /// </summary>
    /// <returns>The hue value normalized to [0, 360).</returns>
    public double Hue => this.hue;

    /// <summary>
    /// Gets the whiteness component in [0, 1].
    /// </summary>
    /// <returns>The whiteness value.</returns>
    public double Whiteness => this.whiteness;

    /// <summary>
    /// Gets the blackness component in [0, 1].
    /// </summary>
    /// <returns>The blackness value.</returns>
    public double Blackness => this.blackness;

    /// <summary>
    /// Converts this HWB color to <see cref="Rgb"/>.
    /// </summary>
    /// <returns>An <see cref="Rgb"/> equivalent to this <see cref="Hwb"/> value.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var rgb = new Hwb(0d, 0d, 0d).ToRgb();
    /// </code>
    /// </example>
    public Rgb ToRgb()
    {
        return ColorSpaceConversions.ToRgb(this);
    }

    /// <summary>
    /// Deconstructs the color into HWB components.
    /// </summary>
    /// <param name="hue">The hue angle in degrees.</param>
    /// <param name="whiteness">The whiteness amount.</param>
    /// <param name="blackness">The blackness amount.</param>
    /// <returns>No value.</returns>
    public void Deconstruct(out double hue, out double whiteness, out double blackness)
    {
        hue = this.hue;
        whiteness = this.whiteness;
        blackness = this.blackness;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Hwb"/> instance.
    /// </summary>
    /// <param name="other">The value to compare.</param>
    /// <returns><see langword="true"/> when components are within a precision tolerance; otherwise <see langword="false"/>.</returns>
    public bool Equals(Hwb other)
    {
        return AreClose(this.hue, other.hue)
            && AreClose(this.whiteness, other.whiteness)
            && AreClose(this.blackness, other.blackness);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is an <see cref="Hwb"/> and equal to this instance; otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Hwb other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code based on the HWB components.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.hue, this.whiteness, this.blackness);
    }

    /// <summary>
    /// Converts this <see cref="Hwb"/> value to <see cref="Rgb"/>.
    /// </summary>
    /// <param name="value">The source HWB value.</param>
    /// <returns>An <see cref="Rgb"/> equivalent to <paramref name="value"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// Rgb rgb = new Hwb(180d, 0.1d, 0.2d);
    /// </code>
    /// </example>
    public static implicit operator Rgb(Hwb value)
    {
        return value.ToRgb();
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) <= DoubleEqualityTolerance;
    }

    private static double NormalizeHue(double value)
    {
        value %= 360d;
        if (value < 0d)
            value += 360d;
        return value;
    }
}
