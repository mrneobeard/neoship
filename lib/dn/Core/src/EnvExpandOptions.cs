namespace NeoBeard;

/// <summary>
/// Options for environment variable expansion.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new EnvExpandOptions
/// {
///     WindowsExpansion = false,
///     CommandSubstitution = false,
/// };
/// </code>
/// </example>
/// </remarks>
public class EnvExpandOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to expand environment
    /// variables using Windows-style syntax (e.g., %VAR%).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { WindowsExpansion = true };
    /// </code>
    /// </example>
    /// </remarks>
    public bool WindowsExpansion { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable command substitution,
    /// for example, using $(command) syntax. This feature allows the output
    /// of a command to be used as part of the environment variable expansion.
    ///
    /// Note that enabling this option may introduce security risks if
    /// untrusted input is processed, as it could lead to command injection vulnerabilities which
    /// is why it is disabled by default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { CommandSubstitution = false };
    /// </code>
    /// </example>
    /// </remarks>
    public bool CommandSubstitution { get; set; } = false;

    /// <summary>
    /// Gets or sets the shell executable used for command substitution.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { UseShell = "bash" };
    /// </code>
    /// </example>
    /// </remarks>
    public string UseShell { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to allow Unix-style
    /// variable assignment (e.g., ${VAR:=value}).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { UnixAssignment = true };
    /// </code>
    /// </example>
    /// </remarks>
    public bool UnixAssignment { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use a custom error message
    /// for Unix-style variable expansion errors.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { UnixCustomErrorMessage = true };
    /// </code>
    /// </example>
    /// </remarks>
    public bool UnixCustomErrorMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable Unix-style
    /// argument expansion (e.g., $1, $2, etc.). Disabled by default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions { UnixArgsExpansion = true };
    /// </code>
    /// </example>
    /// </remarks>
    public bool UnixArgsExpansion { get; set; }

    /// <summary>
    /// Gets or sets a function to retrieve environment variable values.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions
    /// {
    ///     GetVariable = key =&gt; key == "APP" ? "demo" : null,
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public Func<string, string?>? GetVariable { get; set; }

    /// <summary>
    /// Gets or sets a function to set environment variable values.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions
    /// {
    ///     SetVariable = (key, value) =&gt; Console.WriteLine($"{key}={value}"),
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public Action<string, string>? SetVariable { get; set; }

    /// <summary>
    /// Gets or sets a function to retrieve all environment variables as a dictionary. For
    /// command substitution scenarios, this can be used to provide an isolated set of variables
    /// rather than using the process environment variables.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new EnvExpandOptions
    /// {
    ///     GetAllVariables = () =&gt; new Dictionary&lt;string, string&gt; { ["APP"] = "demo" },
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public Func<IDictionary<string, string>>? GetAllVariables { get; set; }
}