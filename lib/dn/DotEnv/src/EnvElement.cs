namespace NeoBeard.DotEnv;

/// <summary>
/// Abstract base class for elements in an environment document.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// EnvElement element = new EnvVariable("KEY", "value", QuoteStyle.Double);
/// Console.WriteLine(element.Kind); // EnvElementKind.Variable
/// </code>
/// </example>
/// </remarks>
public abstract class EnvElement
{
    /// <summary>
    /// Gets the kind of this element.
    /// </summary>
    public abstract EnvElementKind Kind { get; }
}