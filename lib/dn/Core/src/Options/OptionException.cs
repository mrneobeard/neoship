namespace NeoBeard.Options;

/// <summary>
/// Represents exceptions raised when a required option value is not available.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var option = Option&lt;string&gt;.None;
/// option.ValueOrThrow("value required");
/// </code>
/// </example>
/// </remarks>
public class OptionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionException"/> class.
    /// </summary>
    /// <example>
    /// <code lang="csharp">
    /// throw new OptionException();
    /// </code>
    /// </example>
    public OptionException()
    {
    }

    /// <summary>
    /// Initializes a new instance with an error message.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <example>
    /// <code lang="csharp">
    /// throw new OptionException("No value present");
    /// </code>
    /// </example>
    public OptionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets optional diagnostic payload associated with this option failure.
    /// </summary>
    /// <value>
    /// A payload object describing the failed option value, or <see langword="null"/>.
    /// </value>
    public object? Value { get; }

    /// <summary>
    /// Initializes a new instance with a message and diagnostic payload.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="value">Diagnostic payload.</param>
    /// <example>
    /// <code lang="csharp">
    /// throw new OptionException("No value", null);
    /// </code>
    /// </example>
    public OptionException(string message, object? value) : base(message)
    {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance with message and inner exception.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    /// <example>
    /// <code lang="csharp">
    /// throw new OptionException("Outer", new InvalidOperationException());
    /// </code>
    /// </example>
    public OptionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and diagnostic payload.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    /// <param name="value">Diagnostic payload.</param>
    /// <example>
    /// <code lang="csharp">
    /// throw new OptionException("Outer", new Exception(), option);
    /// </code>
    /// </example>
    public OptionException(string message, Exception innerException, object? value) : base(message, innerException)
    {
        this.Value = value;
    }
}
