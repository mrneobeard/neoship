namespace NeoBeard.Colors;

/// <summary>
/// Defines an interface for ARGB color values with alpha channel first.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IArgb color = new Argb(255, 128, 64, 32);
/// Console.WriteLine($"A: {color.A}, R: {color.R}, G: {color.G}, B: {color.B}");
/// </code>
/// </example>
/// </remarks>
public interface IArgb : IEquatable<IArgb>
{
    /// <summary>
    /// Gets the alpha channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(255, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    byte A { get; }

    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(128, color.R);
    /// </code>
    /// </example>
    /// </remarks>
    byte R { get; }

    /// <summary>
    /// Gets the green channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(64, color.G);
    /// </code>
    /// </example>
    /// </remarks>
    byte G { get; }

    /// <summary>
    /// Gets the blue channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color = new Argb(255, 128, 64, 32);
    /// Assert.Equal(32, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    byte B { get; }

    /// <summary>
    /// Deconstructs the color into its ARGB components.
    /// </summary>
    /// <param name="a">The alpha channel value.</param>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IArgb color = new Argb(255, 128, 64, 32);
    /// var (a, r, g, b) = color;
    /// Assert.Equal(255, a);
    /// Assert.Equal(128, r);
    /// Assert.Equal(64, g);
    /// Assert.Equal(32, b);
    /// </code>
    /// </example>
    /// </remarks>
    void Deconstruct(out byte a, out byte r, out byte g, out byte b);
}