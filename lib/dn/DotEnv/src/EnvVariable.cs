namespace NeoBeard.DotEnv;

/// <summary>
/// Represents a variable element (KEY=value) in an environment document.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var variable = new EnvVariable("DATABASE_URL", "postgres://localhost/db", QuoteStyle.Double);
/// Console.WriteLine($"{variable.Key}={variable.Value}");
/// </code>
/// </example>
/// </remarks>
public sealed class EnvVariable : EnvElement
{
    private string key;
    private string value;
    private QuoteStyle quote;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvVariable"/> class.
    /// </summary>
    /// <param name="key">The variable key (name).</param>
    /// <param name="value">The variable value.</param>
    /// <param name="quote">The quote style used for the value.</param>
    public EnvVariable(string key, string value, QuoteStyle quote = QuoteStyle.None)
    {
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.value = value ?? string.Empty;
        this.quote = quote;
    }

    /// <summary>
    /// Gets the element kind (always <see cref="EnvElementKind.Variable"/>).
    /// </summary>
    public override EnvElementKind Kind => EnvElementKind.Variable;

    /// <summary>
    /// Gets or sets the variable key (name).
    /// </summary>
    public string Key
    {
        get => this.key;
        set => this.key = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the variable value.
    /// </summary>
    public string Value
    {
        get => this.value;
        set => this.value = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the quote style for the value.
    /// </summary>
    public QuoteStyle Quote
    {
        get => this.quote;
        set => this.quote = value;
    }

    /// <summary>
    /// Gets a value indicating whether the value is quoted.
    /// </summary>
    public bool IsQuoted => this.quote != QuoteStyle.None;
}