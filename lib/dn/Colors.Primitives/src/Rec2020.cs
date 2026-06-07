namespace NeoBeard.Colors;

/// <summary>
/// Represents an RGB color in the BT.2020 color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Rec2020(1d, 0.5d, 0d).ToRgb();
/// Assert.Equal(new Rgb(255, 128, 0), color);
/// </code>
/// </example>
/// </remarks>
public readonly struct Rec2020 : IEquatable<Rec2020>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double red;

    private readonly double green;

    private readonly double blue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rec2020"/>.
    /// </summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    public Rec2020(double red, double green, double blue)
    {
        this.red = Math.Clamp(red, 0d, 1d);
        this.green = Math.Clamp(green, 0d, 1d);
        this.blue = Math.Clamp(blue, 0d, 1d);
    }

    /// <summary>
    /// Gets the red component.
    /// </summary>
    /// <returns>The red component value in [0, 1].</returns>
    public double R => this.red;

    /// <summary>
    /// Gets the green component.
    /// </summary>
    /// <returns>The green component value in [0, 1].</returns>
    public double G => this.green;

    /// <summary>
    /// Gets the blue component.
    /// </summary>
    /// <returns>The blue component value in [0, 1].</returns>
    public double B => this.blue;

    /// <summary>
    /// Converts this BT.2020 color to RGB.
    /// </summary>
    /// <returns>An <see cref="Rgb"/> value representing this color.</returns>
    public Rgb ToRgb()
    {
        return ColorSpaceConversions.ToRgb(this);
    }

    /// <summary>
    /// Deconstructs the color into BT.2020 components.
    /// </summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    public void Deconstruct(out double red, out double green, out double blue)
    {
        red = this.red;
        green = this.green;
        blue = this.blue;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Rec2020"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><c>true</c> if the instances are approximately equal; otherwise, <c>false</c>.</returns>
    public bool Equals(Rec2020 other)
    {
        return Approximately(this.red, other.red)
            && Approximately(this.green, other.green)
            && Approximately(this.blue, other.blue);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the object is a <see cref="Rec2020"/> and equals this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Rec2020 other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.red, this.green, this.blue);
    }

    /// <summary>
    /// Converts this instance to an equivalent <see cref="Rgb"/> value.
    /// </summary>
    /// <param name="value">The color to convert.</param>
    /// <returns>An RGB color converted from <paramref name="value"/>.</returns>
    public static implicit operator Rgb(Rec2020 value)
    {
        return value.ToRgb();
    }

    private static bool Approximately(double left, double right)
    {
        return Math.Abs(left - right) <= DoubleEqualityTolerance;
    }
}
