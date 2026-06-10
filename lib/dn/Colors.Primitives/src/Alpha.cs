namespace NeoBeard.Colors;

/// <summary>
/// Represents an alpha channel value for color spaces/profiles.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var opaque = Alpha.Opaque;
/// var transparent = Alpha.Transparent;
/// var half = new Alpha(0.5);
/// </code>
/// </example>
/// </remarks>
public readonly struct Alpha : IEquatable<Alpha>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Alpha"/> struct.
    /// </summary>
    /// <param name="a">The alpha value between 0.0 (transparent) and 1.0 (opaque).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when value is outside the range [0.0, 1.0].</exception>
    /// <returns>An <see cref="Alpha"/> value equivalent to <paramref name="a"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var halfOpaque = new Alpha(0.5);
    /// Assert.Equal(0.5, halfOpaque.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Alpha(double a)
    {
        if (a < 0d || a > 1d)
            throw new ArgumentOutOfRangeException(nameof(a));

        this.A = a;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Alpha"/> struct with opaque (1.0) value.
    /// </summary>
    /// <returns>An <see cref="Alpha"/> value set to 1.0.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha();
    /// Assert.Equal(1.0, alpha.A);
    /// </code>
    /// </example>
    /// </remarks>
    public Alpha()
    {
        this.A = 1d;
    }

    /// <summary>
    /// Gets a fully opaque alpha value (1.0).
    /// </summary>
    /// <returns>An <see cref="Alpha"/> value equal to 1.0.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var opaque = Alpha.Opaque;
    /// Assert.Equal(1.0, opaque.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static Alpha Opaque => new(1d);

    /// <summary>
    /// Gets a fully transparent alpha value (0.0).
    /// </summary>
    /// <returns>An <see cref="Alpha"/> value equal to 0.0.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var transparent = Alpha.Transparent;
    /// Assert.Equal(0.0, transparent.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static Alpha Transparent => new(0d);

    /// <summary>
    /// Gets the alpha value between 0.0 (transparent) and 1.0 (opaque).
    /// </summary>
    /// <returns>The alpha value as <see cref="double"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha(0.5);
    /// Assert.Equal(0.5, alpha.A);
    /// </code>
    /// </example>
    /// </remarks>
    public double A { get; } = 1d;

    /// <summary>
    /// Converts a <see cref="double"/> to an <see cref="Alpha"/>.
    /// </summary>
    /// <param name="a">The alpha value between 0.0 and 1.0.</param>
    /// <returns>An <see cref="Alpha"/> value equivalent to <paramref name="a"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Alpha alpha = 0.5;
    /// Assert.Equal(0.5, alpha.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Alpha(double a)
    {
        return new Alpha(a);
    }

    /// <summary>
    /// Converts an <see cref="Alpha"/> to a <see cref="double"/>.
    /// </summary>
    /// <param name="a">The alpha value to convert.</param>
    /// <returns>The <see cref="double"/> representation of <paramref name="a"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha(0.5);
    /// double value = alpha;
    /// Assert.Equal(0.5, value);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator double(Alpha a)
    {
        return a.A;
    }

    /// <summary>
    /// Converts a <see cref="byte"/> to an <see cref="Alpha"/> (0-255 maps to 0.0-1.0).
    /// </summary>
    /// <param name="a">The byte alpha value (0-255).</param>
    /// <returns>An <see cref="Alpha"/> value equivalent to <paramref name="a"/> divided by 255.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Alpha alpha = (byte)128;
    /// Assert.Equal(128.0 / 255.0, alpha.A);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Alpha(byte a)
    {
        return new Alpha(a / 255d);
    }

    /// <summary>
    /// Converts an <see cref="Alpha"/> to a <see cref="byte"/> (0.0-1.0 maps to 0-255).
    /// </summary>
    /// <param name="a">The alpha value to convert.</param>
    /// <returns>A <see cref="byte"/> representing the same alpha value as <paramref name="a"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha(0.5);
    /// byte value = alpha;
    /// Assert.Equal(127, value);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator byte(Alpha a)
    {
        return (byte)(a.A * 255);
    }

    /// <summary>
    /// Determines whether this instance equals another <see cref="Alpha"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha1 = new Alpha(0.5);
    /// var alpha2 = new Alpha(0.5);
    /// Assert.True(alpha1.Equals(alpha2));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Alpha other)
    {
        return this.A.Equals(other.A);
    }

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the object is an <see cref="Alpha"/> and equals this instance; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha(0.5);
    /// object obj = new Alpha(0.5);
    /// Assert.True(alpha.Equals(obj));
    /// Assert.False(alpha.Equals(null));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj)
    {
        return obj is Alpha other && this.Equals(other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>An <see cref="int"/> hash code for this <see cref="Alpha"/> value.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var alpha = new Alpha(0.5);
    /// var hash = alpha.GetHashCode();
    /// Assert.NotEqual(0, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.A);
    }
}