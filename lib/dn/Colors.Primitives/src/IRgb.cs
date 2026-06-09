namespace NeoBeard.Colors;

/// <summary>
/// Defines an interface for RGB color values without an alpha channel.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IRgb color = new Rgb(255, 128, 64);
/// Console.WriteLine($"R: {color.R}, G: {color.G}, B: {color.B}");
/// </code>
/// </example>
/// </remarks>
public interface IRgb : IEquatable<IRgb>
{
    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgb color = new Rgb(255, 128, 64);
    /// Assert.Equal(255, color.R);
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
    /// IRgb color = new Rgb(255, 128, 64);
    /// Assert.Equal(128, color.G);
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
    /// IRgb color = new Rgb(255, 128, 64);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    byte B { get; }

    /// <summary>
    /// Deconstructs the color into its RGB components.
    /// </summary>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgb color = new Rgb(255, 128, 64);
    /// var (r, g, b) = color;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// </code>
    /// </example>
    /// </remarks>
    void Deconstruct(out byte r, out byte g, out byte b);
}