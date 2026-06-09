namespace NeoBeard.Exec;

/// <summary>
/// Defines an interface for objects that own command options.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// public class MyCommand : ICommandOptionsOwner
/// {
///     public CommandOptions Options { get; } = new CommandOptions();
/// }
///
/// var cmd = new MyCommand();
/// cmd.Options.File = "dotnet";
/// cmd.Options.Args = ["--version"];
/// </code>
/// </example>
/// </remarks>
public interface ICommandOptionsOwner
{
    /// <summary>
    /// Gets the command options.
    /// </summary>
    /// <value>The <see cref="CommandOptions"/> for this owner.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// ICommandOptionsOwner owner = new Command();
    /// var options = owner.Options;
    /// options.File = "dotnet";
    /// </code>
    /// </example>
    /// </remarks>
    CommandOptions Options { get; }
}