namespace NeoBeard.Ssh;

/// <summary>
/// Represents the ordered environment values that should be sent for an SSH session.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var plan = new SshSessionEnvPlan(
///     new Dictionary&lt;string, string&gt; { ["APP_ENV"] = "prod" },
///     strictEnv: false);
/// Console.WriteLine(plan.Variables["APP_ENV"]);
/// </code>
/// </example>
/// </remarks>
public sealed class SshSessionEnvPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SshSessionEnvPlan"/> class.
    /// </summary>
    /// <param name="variables">The variables to send in env requests.</param>
    /// <param name="strictEnv">A value indicating whether env rejection should fail the session.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var plan = new SshSessionEnvPlan(
    ///     new Dictionary&lt;string, string&gt; { ["LANG"] = "C.UTF-8" },
    ///     strictEnv: true);
    /// </code>
    /// </example>
    /// </remarks>
    public SshSessionEnvPlan(IReadOnlyDictionary<string, string> variables, bool strictEnv)
    {
        this.Variables = variables;
        this.StrictEnv = strictEnv;
    }

    /// <summary>
    /// Gets the env variables that should be requested for the session.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vars = plan.Variables;
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, string> Variables { get; }

    /// <summary>
    /// Gets a value indicating whether env rejection should fail the session.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// bool strict = plan.StrictEnv;
    /// </code>
    /// </example>
    /// </remarks>
    public bool StrictEnv { get; }
}