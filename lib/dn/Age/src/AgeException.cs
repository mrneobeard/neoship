namespace NeoBeard.Age;

/// <summary>
/// Represents an error raised while processing an age file.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// try
/// {
///     AgeFile.Decrypt(Stream.Null, Stream.Null, []);
/// }
/// catch (AgeException ex)
/// {
///     Console.WriteLine(ex.Message);
/// }
/// </code>
/// </example>
/// </remarks>
public class AgeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgeException"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exception = new AgeException();
    /// Console.WriteLine(exception.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public AgeException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgeException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exception = new AgeException("The age file is invalid.");
    /// Console.WriteLine(exception.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public AgeException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AgeException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inner = new InvalidOperationException();
    /// var exception = new AgeException("The age file is invalid.", inner);
    /// Console.WriteLine(exception.InnerException is InvalidOperationException);
    /// </code>
    /// </example>
    /// </remarks>
    public AgeException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}