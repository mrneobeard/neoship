namespace NeoBeard.DotEnv;

/// <summary>
/// Represents a comment element in an environment document.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var comment = new EnvComment("This is a comment", isInline: false);
/// Console.WriteLine($"# {comment.Text}");
/// </code>
/// </example>
/// </remarks>
public sealed class EnvComment : EnvElement
{
    private string text;
    private bool isInline;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvComment"/> class.
    /// </summary>
    /// <param name="text">The comment text (without the # prefix).</param>
    /// <param name="isInline">Whether this is an inline comment after a variable.</param>
    public EnvComment(string text, bool isInline = false)
    {
        this.text = text ?? string.Empty;
        this.isInline = isInline;
    }

    /// <summary>
    /// Gets the element kind (always <see cref="EnvElementKind.Comment"/>).
    /// </summary>
    public override EnvElementKind Kind => EnvElementKind.Comment;

    /// <summary>
    /// Gets or sets the comment text (without the # prefix).
    /// </summary>
    public string Text
    {
        get => this.text;
        set => this.text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this is an inline comment.
    /// Inline comments appear on the same line as a variable.
    /// </summary>
    public bool IsInline
    {
        get => this.isInline;
        set => this.isInline = value;
    }
}