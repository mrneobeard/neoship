namespace NeoBeard.Colors;

/// <summary>
/// Defines an interface for RGBA color values with an alpha channel.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IRgba color = new Rgba(255, 128, 64, 200);
/// Console.WriteLine($"R: {color.R}, G: {color.G}, B: {color.B}, A: {color.A}");
/// </code>
/// </example>
/// </remarks>
public interface IRgba : IEquatable<IRgba>
{
    /// <summary>
    /// Gets the red channel value (0-255).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgba color = new Rgba(255, 128, 64, 200);
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
    /// IRgba color = new Rgba(255, 128, 64, 200);
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
    /// IRgba color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(64, color.B);
    /// </code>
    /// </example>
    /// </remarks>
    byte B { get; }

    /// <summary>
    /// Gets the alpha channel value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgba color = new Rgba(255, 128, 64, 200);
    /// Assert.Equal(200, color.A);
    /// </code>
    /// </example>
    /// </remarks>
    Alpha A { get; }

    /// <summary>
    /// Deconstructs the color into its RGBA components.
    /// </summary>
    /// <param name="r">The red channel value.</param>
    /// <param name="g">The green channel value.</param>
    /// <param name="b">The blue channel value.</param>
    /// <param name="a">The alpha channel value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IRgba color = new Rgba(255, 128, 64, 200);
    /// var (r, g, b, a) = color;
    /// Assert.Equal(255, r);
    /// Assert.Equal(128, g);
    /// Assert.Equal(64, b);
    /// Assert.Equal(200, a);
    /// </code>
    /// </example>
    /// </remarks>
    void Deconstruct(out byte r, out byte g, out byte b, out Alpha a);
}