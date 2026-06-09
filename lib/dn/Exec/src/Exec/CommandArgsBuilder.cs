namespace NeoBeard.Exec;

/// <summary>
/// An abstract base class for building command arguments.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// public class DotnetBuildArgs : CommandArgsBuilder
/// {
///     public override string[] SubCommands => ["build"];
///
///     public string? Configuration { get; set; }
///     public string? OutputPath { get; set; }
///
///     public override CommandArgs Build()
///     {
///         var args = new CommandArgs(SubCommands);
///         if (Configuration != null)
///             args.AddRange(["-c", Configuration]);
///         if (OutputPath != null)
///             args.AddRange(["-o", OutputPath]);
///         return args;
///     }
/// }
///
/// CommandArgs args = new DotnetBuildArgs { Configuration = "Release" };
/// </code>
/// </example>
/// </remarks>
public abstract class CommandArgsBuilder : ICommandArgsBuilder
{
    /// <summary>
    /// Gets the sub-commands for this builder.
    /// </summary>
    /// <value>An array of sub-command strings.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// public class GitArgs : CommandArgsBuilder
    /// {
    ///     public override string[] SubCommands => ["clone"];
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract string[] SubCommands { get; }

    /// <summary>
    /// Converts a <see cref="CommandArgsBuilder"/> to <see cref="CommandArgs"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The built <see cref="CommandArgs"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var builder = new DotnetBuildArgs { Configuration = "Release" };
    /// CommandArgs args = builder;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(CommandArgsBuilder builder)
    {
        return builder.Build();
    }

    /// <summary>
    /// Builds the command arguments.
    /// </summary>
    /// <returns>A <see cref="CommandArgs"/> instance containing the built arguments.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var builder = new DotnetBuildArgs { Configuration = "Release" };
    /// var args = builder.Build();
    /// Assert.Contains("-c", args);
    /// Assert.Contains("Release", args);
    /// </code>
    /// </example>
    /// </remarks>
    public abstract CommandArgs Build();
}