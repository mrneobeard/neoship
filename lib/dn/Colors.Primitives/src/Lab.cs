namespace NeoBeard.Colors;

/// <summary>
/// Represents a color in CIELAB color space.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var lab = new Lab(53d, 80d, 67d);
/// var rgb = lab.ToRgb();
/// </code>
/// </example>
/// </remarks>
public readonly struct Lab : IEquatable<Lab>
{
    private const double DoubleEqualityTolerance = 1e-12d;

    private readonly double lightness;

    private readonly double a;

    private readonly double b;

    /// <summary>
    /// Initializes a new instance of the <see cref="Lab"/> struct.
    /// </summary>
    /// <param name="lightness">The lightness component in [0, 100].</param>
    /// <param name="a">The a* component (green-red axis).</param>
    /// <param name="b">The b* component (blue-yellow axis).</param>
    /// <returns>A newly initialized <see cref="Lab"/> value.</returns>
    public Lab(double lightness, double a, double b)
    {
        this.lightness = Math.Clamp(lightness, 0d, 100d);
        this.a = a;
        this.b = b;
    }

    /// <summary>
    /// Gets the CIELAB lightness component in [0, 100].
    /// </summary>
    /// <returns>The lightness value.</returns>
    public double Lightness => this.lightness;

    /// <summary>
    /// Gets the CIELAB a* component (green-red axis).
    /// </summary>
    /// <returns>The a* value.</returns>
    public double A => this.a;

    /// <summary>
    /// Gets the CIELAB b* component (blue-yellow axis).
    /// </summary>
    /// <returns>The b* value.</returns>
    public double B => this.b;

    /// <summary>
    /// Deconstructs the color into LAB components.
    /// </summary>
    /// <param name="lightness">The lightness component.</param>
    /// <param name="a">The a* component.</param>
    /// <param name="b">The b* component.</param>
    /// <returns>No value.</returns>
    public void Deconstruct(out double lightness, out double a, out double b)
    {
        lightness = this.lightness;
        a = this.a;
        b = this.b;
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Lab"/> instance.
    /// </summary>
    /// <param name="other">The value to compare.</param>
    /// <returns><see langword="true"/> when each LAB component is within tolerance; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var a = new Lab(53d, 80d, 67d);
    /// var b = new Lab(53d, 80d, 67d);
    /// Assert.True(a.Equals(b));
    /// </code>
    /// </example>
    public bool Equals(Lab other)
    {
        return AreClose(this.lightness, other.lightness)
            && AreClose(this.a, other.a)
            && AreClose(this.b, other.b);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Lab"/> and matches this value; otherwise <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is Lab other && this.Equals(other);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code for this <see cref="Lab"/> value.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.lightness, this.a, this.b);
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) <= DoubleEqualityTolerance;
    }
}