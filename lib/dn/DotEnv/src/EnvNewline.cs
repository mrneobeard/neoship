namespace NeoBeard.DotEnv;

/// <summary>
/// Represents a newline element in an environment document.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var doc = new EnvDoc();
/// doc.Set("KEY1", "value1");
/// doc.AddNewline();
/// doc.Set("KEY2", "value2");
/// </code>
/// </example>
/// </remarks>
public sealed class EnvNewline : EnvElement
{
    /// <summary>
    /// Gets the element kind (always <see cref="EnvElementKind.Newline"/>).
    /// </summary>
    public override EnvElementKind Kind => EnvElementKind.Newline;
}