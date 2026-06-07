namespace NeoBeard.Results;

/// <summary>
/// Represents exceptions raised when attempting to access values from an error-state <see cref="Result"/>.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Result result = Result.Fail(Error.From("failed"));
/// Error error = result; // will throw <see cref="ResultException"/>
/// </code>
/// </example>
/// </remarks>
public class ResultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultException"/> class.
    /// </summary>
    public ResultException()
    {
    }

    /// <summary>
    /// Initializes a new instance with an error message.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <example>
    /// <code lang="csharp">
    /// throw new ResultException("Result is empty");
    /// </code>
    /// </example>
    public ResultException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the result payload associated with this exception.
    /// </summary>
    /// <value>
    /// The <see cref="object"/> associated with the failed result, or <see langword="null"/>.
    /// </value>
    public object? Value { get; }

    /// <summary>
    /// Initializes a new instance with a message and payload.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="value">Associated payload.</param>
    public ResultException(string message, object? value) : base(message)
    {
        this.Value = value;
    }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public ResultException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance with a message, inner exception, and payload.
    /// </summary>
    /// <param name="message">The user-facing error message.</param>
    /// <param name="innerException">The underlying cause.</param>
    /// <param name="value">Associated payload.</param>
    public ResultException(string message, Exception innerException, object? value) : base(message, innerException)
    {
        this.Value = value;
    }
}
