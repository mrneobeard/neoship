namespace NeoBeard.Exec;

public readonly struct CommandToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandToken"/> struct.
    /// </summary>
    /// <param name="value">The string value of the token.</param>
    /// <param name="kind">The kind of the token.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var token = new CommandToken("echo", CommandTokenKind.Arg);
    /// Assert.Equal("echo", token.Value);
    /// Assert.Equal(CommandTokenKind.Arg, token.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public CommandToken(string value, CommandTokenKind kind)
    {
        this.Value = value;
        this.Kind = kind;
    }

    public string Value { get; }

    public CommandTokenKind Kind { get; }

    /// <summary>
    /// Returns the string representation of the token.
    /// </summary>
    /// <returns>The token's value as a string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var token = new CommandToken("echo", CommandTokenKind.Arg);
    /// Assert.Equal("echo", token.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString() => this.Value;
}