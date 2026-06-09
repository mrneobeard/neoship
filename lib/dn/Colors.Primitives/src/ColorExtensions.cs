namespace NeoBeard.Colors;

/// <summary>
/// Provides extension methods for color types.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var color = new Rgb(255, 128, 64);
/// var color256 = color.To256Color();
/// Console.WriteLine($"256-color index: {color256}");
/// </code>
/// </example>
/// </remarks>
public static class ColorExtensions
{
    /// <summary>
    /// Converts an RGB color to the nearest 256-color palette index.
    /// </summary>
    /// <param name="color">The RGB color to convert.</param>
    /// <returns>The 256-color palette index (0-255).</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var color = new Rgb(255, 0, 0);
    /// var index = color.To256Color();
    /// Assert.Equal(196, index);
    /// </code>
    /// </example>
    /// </remarks>
    public static int To256Color(this Rgb color)
    {
        var r = color.R;
        var g = color.G;
        var b = color.B;

        // Check if grayscale (all components roughly equal)
        if (r == g && g == b)
        {
            // Grayscale range: 232-255 (24 shades)
            // Map 0-255 to 0-23, then add 232
            return 232 + (r * 24 / 256);
        }

        // 216 colors (16-231): 6 levels for each RGB component
        // Formula: 16 + 36*r + 6*g + b (where r,g,b are 0-5)
        var r6 = r * 6 / 256;
        var g6 = g * 6 / 256;
        var b6 = b * 6 / 256;

        return 16 + (36 * r6) + (6 * g6) + b6;
    }
}