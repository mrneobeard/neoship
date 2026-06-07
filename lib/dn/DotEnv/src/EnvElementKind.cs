namespace NeoBeard.DotEnv;

/// <summary>
/// Represents the kind of element in an environment document.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// foreach (var element in doc)
/// {
///     switch (element.Kind)
///     {
///         case EnvElementKind.Variable:
///             var v = (EnvVariable)element;
///             Console.WriteLine($"{v.Key}={v.Value}");
///             break;
///         case EnvElementKind.Comment:
///             var c = (EnvComment)element;
///             Console.WriteLine($"# {c.Text}");
///             break;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public enum EnvElementKind
{
    /// <summary>
    /// A variable element (KEY=value).
    /// </summary>
    Variable = 0,

    /// <summary>
    /// A comment element (# comment).
    /// </summary>
    Comment = 1,

    /// <summary>
    /// A newline element.
    /// </summary>
    Newline = 2,
}