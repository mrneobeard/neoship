namespace NeoBeard;

/// <summary>
/// Represents the ResourceNotFoundException class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with a default message.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ResourceNotFoundException();
    /// Assert.Equal("The requested resource was not found.", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ResourceNotFoundException()
        : base("The requested resource was not found.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ResourceNotFoundException("Custom error message");
    /// Assert.Equal("Custom error message", ex.Message);
    /// </code>
    /// </example>
    /// </remarks>
    public ResourceNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inner = new InvalidOperationException("inner error");
    /// var ex = new ResourceNotFoundException("outer error", inner);
    /// Assert.Equal("outer error", ex.Message);
    /// Assert.Same(inner, ex.InnerException);
    /// </code>
    /// </example>
    /// </remarks>
    public ResourceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class with resource name and type.
    /// </summary>
    /// <param name="resourceName">The name of the resource that was not found.</param>
    /// <param name="resourceType">The type of the resource that was not found.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new ResourceNotFoundException("config.json", "file");
    /// Assert.Equal("config.json", ex.ResourceName);
    /// Assert.Equal("file", ex.ResourceType);
    /// </code>
    /// </example>
    /// </remarks>
    public ResourceNotFoundException(string resourceName, string resourceType)
        : base($"The resource '{resourceName}' of type '{resourceType}' was not found.")
    {
        this.ResourceName = resourceName;
        this.ResourceType = resourceType;
    }

    public string? ResourceName { get; }

    public string? ResourceType { get; }
}