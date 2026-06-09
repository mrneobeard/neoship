namespace NeoBeard;

/// <summary>
/// Represents errors that occur while expanding or manipulating environment values.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// throw new EnvironmentException("Variable HOME is missing.");
/// </code>
/// </example>
/// </remarks>
public class EnvironmentException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentException"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new EnvironmentException();
    /// </code>
    /// </example>
    /// </remarks>
    public EnvironmentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new EnvironmentException("Invalid interpolation.");
    /// </code>
    /// </example>
    /// </remarks>
    public EnvironmentException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference if no inner exception is specified.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// try
    /// {
    ///     throw new InvalidOperationException("bad token");
    /// }
    /// catch (Exception inner)
    /// {
    ///     throw new EnvironmentException("Expansion failed.", inner);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public EnvironmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}