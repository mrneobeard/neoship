namespace NeoBeard.Exec;

/// <summary>
/// Defines an interface for building command arguments.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// public class MyArgsBuilder : ICommandArgsBuilder
/// {
///     public CommandArgs Build() => ["--flag", "value"];
/// }
///
/// ICommandArgsBuilder builder = new MyArgsBuilder();
/// var args = builder.Build();
/// </code>
/// </example>
/// </remarks>
public interface ICommandArgsBuilder
{
    /// <summary>
    /// Builds the command arguments.
    /// </summary>
    /// <returns>A <see cref="CommandArgs"/> instance containing the built arguments.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ICommandArgsBuilder builder = new MyArgsBuilder();
    /// var args = builder.Build();
    /// Assert.Equal(2, args.Count);
    /// </code>
    /// </example>
    /// </remarks>
    CommandArgs Build();
}