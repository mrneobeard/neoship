namespace NeoBeard.Exec;

/// <summary>
/// Represents the CommandTokenKind enum.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public enum CommandTokenKind
{
    Invalid = 0,
    Arg,
    SingleQuotedArg,
    DoubleQuotedArg,
    SubProcessStart,
    SubProcessEnd,
    And,
    Or,
    Pipe,
    StatementEnd,
    ClosureStart,
    ClosureEnd,
}