namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in HSL color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hsl = new Hsl(30d, 1d, 0.5d);
/// var rgb = hsl.ToRgb();
/// Assert.Equal(new Rgb(255, 128, 0), rgb);
/// </code>
/// </example>
/// </remarks>
public readonly struct Hsl : IEquatable<Hsl>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double hue;

    private readonly double saturation;

    private readonly double lightness;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hsl"/>.
    /// </summary>
    /// <param name="hue">The hue component in degrees.</param>
    /// <param name="saturation">The saturation component in [0, 1].</param>
    /// <param name="lightness">The lightness component in [0, 1].</param>
    public Hsl(double hue, double saturation, double lightness)
    {
        this.hue = NormalizeHue(hue);
        this.saturation = Math.Clamp(saturation, 0d, 1d);
        this.lightness = Math.Clamp(lightness, 0d, 1d);
    }

    /// <summary>
    /// Gets the hue component in degrees.
    /// </summary>
    /// <returns>The hue value in degrees.</returns>
    public double Hue => this.hue;

    /// <summary>
    /// Gets the saturation component in [0, 1].
    /// </summary>
    /// <returns>The saturation value in [0, 1].</returns>
    public double Saturation => this.saturation;

    /// <summary>
    /// Gets the lightness component in [0, 1].
    /// </summary>
    /// <returns>The lightness value in [0, 1].</returns>
    public double Lightness => this.lightness;

    /// <summary>
    /// Converts this HSL color to RGB.
    /// </summary>
    /// <returns>An <see cref="Rgb"/> value representing this color.</returns>
    public Rgb ToRgb()
    {
        return ColorSpaceConversions.ToRgb(this);
    }

    /// <summary>
    /// Deconstructs the color into HSL components.
    /// </summary>
    /// <param name="hue">The hue component.</param>
    /// <param name="saturation">The saturation component.</param>
    /// <param name="lightness">The lightness component.</param>
    public void Deconstruct(out double hue, out double saturation, out double lightness)
    {
        hue = this.hue;
        saturation = this.saturation;
        lightness = this.lightness;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Hsl"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are approximately equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Hsl other)
    {
        return Approximately(this.hue, other.hue)
            && Approximately(this.saturation, other.saturation)
            && Approximately(this.lightness, other.lightness);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the object is a <see cref="Hsl"/> and equals this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Hsl other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.hue, this.saturation, this.lightness);
    }

    /// <summary>
    /// Converts this HSL color to an equivalent <see cref="Rgb"/> value.
    /// </summary>
    /// <param name="value">The color to convert.</param>
    /// <returns>An RGB color converted from <paramref name="value"/>.</returns>
    public static implicit operator Rgb(Hsl value)
    {
        return value.ToRgb();
    }

    private static bool Approximately(double left, double right)
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