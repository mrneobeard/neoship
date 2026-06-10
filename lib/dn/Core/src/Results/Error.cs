namespace NeoBeard.Results;

/// <summary>
/// Represents a non-generic error value.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Error error = Error.From("boom");
/// Console.WriteLine(error.Message);
/// </code>
/// </example>
/// </remarks>
public readonly struct Error : IError
{
    /// <summary>
    /// Initializes a new <see cref="Error"/> from an <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The source exception.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <example>
    /// <code lang="csharp">
    /// var error = new Error(new InvalidOperationException("bad"), "failed", "E001");
    /// </code>
    /// </example>
    public Error(Exception exception, string? message = null, string? code = null)
    {
        this.Message = message ?? exception.Message;
        this.Code = code ?? exception.GetType().Name;
        this.Exception = exception;
    }

    /// <summary>
    /// Initializes a new <see cref="Error"/> from a nested <see cref="IError"/> cause.
    /// </summary>
    /// <param name="cause">The underlying error cause.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <example>
    /// <code lang="csharp">
    /// var cause = Error.From("root");
    /// var error = new Error(cause, "wrapper");
    /// </code>
    /// </example>
    public Error(IError cause, string? message = null, string? code = null)
    {
        this.Message = message ?? cause.Message;
        this.Code = code ?? cause.Code;
        this.Exception = cause.Exception;
        this.Cause = cause;
    }

    /// <summary>
    /// Initializes a new <see cref="Error"/> from a message and optional code.
    /// </summary>
    /// <param name="message">The human-readable message.</param>
    /// <param name="code">Optional error code.</param>
    /// <example>
    /// <code lang="csharp">
    /// var error = new Error("bad input", "E_INVALID");
    /// </code>
    /// </example>
    public Error(string message, string? code = null)
    {
        this.Message = message;
        this.Code = code ?? "error";
        this.Exception = null;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <value>The message associated with this error.</value>
    public string Message { get; }

    /// <summary>
    /// Gets the optional error code.
    /// </summary>
    /// <value>The error code, or <see langword="null"/>.</value>
    public string? Code { get; }

    /// <summary>
    /// Gets an optional nested error cause.
    /// </summary>
    /// <value>The nested <see cref="IError"/> if available.</value>
    public IError? Cause { get; }

    /// <summary>
    /// Gets an optional exception payload.
    /// </summary>
    /// <value>The associated <see cref="Exception"/> if present.</value>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the shared empty error value.
    /// </summary>
    /// <value>An empty <see cref="Error"/> instance.</value>
    public static Error Empty { get; } = new(string.Empty, string.Empty);

    /// <summary>
    /// Creates an <see cref="Error"/> from a message.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error"/> value.</returns>
    /// <example>
    /// <code lang="csharp">
    /// Error error = Error.From("bad");
    /// </code>
    /// </example>
    public static Error From(string message, string? code = null)
        => new(message, code);

    /// <summary>
    /// Creates an <see cref="Error"/> from an exception.
    /// </summary>
    /// <param name="exception">The source exception.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error"/> value.</returns>
    public static Error From(Exception exception, string? message = null, string? code = null)
        => new(exception, message, code);

    /// <summary>
    /// Creates an <see cref="Error"/> from another <see cref="IError"/> cause.
    /// </summary>
    /// <param name="cause">The underlying cause.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error"/> value.</returns>
    public static Error From(IError cause, string? message = null, string? code = null)
    {
        return new Error(cause, message, code);
    }

    /// <summary>
    /// Converts a message value into an <see cref="Error"/>.
    /// </summary>
    /// <param name="message">The source error.</param>
    /// <returns>
    /// A <see cref="string"/> containing <paramref name="message"/> when conversion is requested by compiler.
    /// </returns>
    public static implicit operator Error(string message) => new(message);

    /// <summary>
    /// Converts an <see cref="Exception"/> into an <see cref="Error"/>.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <returns>A newly created <see cref="Error"/>.</returns>
    public static implicit operator Error(Exception exception) => new(exception);

    /// <summary>
    /// Converts an <see cref="Error"/> into its message string.
    /// </summary>
    /// <param name="error">The source error.</param>
    /// <returns><paramref name="error"/> as <see cref="string"/>.</returns>
    public static implicit operator string(Error error) => error.Message;

    /// <summary>
    /// Converts an <see cref="Error"/> into an <see cref="Exception"/>.
    /// </summary>
    /// <param name="error">The source error.</param>
    /// <returns>An <see cref="Exception"/> representing this error.</returns>
    public static implicit operator Exception(Error error) => error.ToException();

    /// <summary>
    /// Converts this error into an <see cref="Exception"/>.
    /// </summary>
    /// <returns>
    /// The associated <see cref="Exception"/> if present; otherwise a new <see cref="ResultException"/>.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// var ex = error.ToException();
    /// throw ex;
    /// </code>
    /// </example>
    public Exception ToException()
        => this.Exception ?? new ResultException(this.Message, this);

    /// <summary>
    /// Returns the display representation of this error.
    /// </summary>
    /// <returns>A <see cref="string"/> for diagnostics.</returns>
    public override string ToString() => $"{nameof(Error)}: {Message}";
}


/// <summary>
/// Represents a typed error value.
/// </summary>
/// <typeparam name="T">The typed error payload.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Error&lt;int&gt; error = Error&lt;int&gt;.From(5, "invalid code");
/// Console.WriteLine(error.Value);
/// </code>
/// </example>
/// </remarks>
public readonly struct Error<T> : IError<T>
{
    /// <summary>
    /// Initializes a new <see cref="Error{T}"/> from a typed value.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="code">Optional error code.</param>
    public Error(T value, string? message = null, string? code = null)
    {
        this.Value = value;
        this.Message = message ?? value?.ToString() ?? string.Empty;
        this.Code = code;
        this.Exception = null;
    }

    /// <summary>
    /// Initializes a new <see cref="Error{T}"/> from value and exception.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="exception">The source exception.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    public Error(T value, Exception exception, string? message = null, string? code = null)
    {
        this.Value = value;
        this.Message = message ?? exception.Message;
        this.Code = code ?? exception.GetType().Name;
        this.Exception = exception;
    }

    /// <summary>
    /// Initializes a new <see cref="Error{T}"/> from value and nested error cause.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="cause">The underlying error.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    public Error(T value, IError cause, string? message = null, string? code = null)
    {
        this.Value = value;
        this.Message = message ?? cause.Message;
        this.Code = code ?? cause.Code;
        this.Exception = cause.Exception;
        this.Cause = cause;
    }

    /// <summary>
    /// Gets the typed error payload.
    /// </summary>
    /// <value>The <typeparamref name="T"/> payload.</value>
    public T Value { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <value>The message associated with this typed error.</value>
    public string Message { get; }

    /// <summary>
    /// Gets an optional error code.
    /// </summary>
    /// <value>The error code, or <see langword="null"/>.</value>
    public string? Code { get; }

    /// <summary>
    /// Gets an optional nested error cause.
    /// </summary>
    /// <value>An optional nested <see cref="IError"/>.</value>
    public IError? Cause { get; }

    /// <summary>
    /// Gets an optional exception payload.
    /// </summary>
    /// <value>The associated <see cref="Exception"/> if available.</value>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the shared empty typed error value.
    /// </summary>
    /// <value>An empty <see cref="Error{T}"/> value.</value>
    public static Error<T> Empty { get; } = new(default!, string.Empty, string.Empty);

    /// <summary>
    /// Converts a typed value to <see cref="Error{T}"/>.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <returns>An <see cref="Error{T}"/> containing <paramref name="value"/>.</returns>
    public static implicit operator Error<T>(T value)
    {
        if (value is Exception ex)
            return new Error<T>(value, ex.Message, ex.GetType().Name);

        return new Error<T>(value);
    }

    /// <summary>
    /// Converts a typed error into its value.
    /// </summary>
    /// <param name="error">The source typed error.</param>
    /// <returns>The typed payload.</returns>
    public static implicit operator T(Error<T> error) => error.Value;

    /// <summary>
    /// Converts a typed error into its message string.
    /// </summary>
    /// <param name="error">The source typed error.</param>
    /// <returns>The message string.</returns>
    public static implicit operator string(Error<T> error) => error.Message;

    /// <summary>
    /// Converts a typed error into an <see cref="Exception"/>.
    /// </summary>
    /// <param name="error">The source typed error.</param>
    /// <returns>An <see cref="Exception"/> representation.</returns>
    public static implicit operator Exception(Error<T> error) => error.ToException();

    /// <summary>
    /// Creates a typed error from a payload value.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <returns>A new <see cref="Error{T}"/> value.</returns>
    public static Error<T> From(T value) => new(value);

    /// <summary>
    /// Creates a typed error from payload and exception details.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="exception">The source exception.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error{T}"/> value.</returns>
    public static Error<T> From(T value, Exception exception, string? message = null, string? code = null) => new(value, exception, message, code);

    /// <summary>
    /// Creates a typed error from payload and a cause.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="cause">The underlying error cause.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error{T}"/> value.</returns>
    public static Error<T> From(T value, IError cause, string? message = null, string? code = null) => new(value, cause, message, code);

    /// <summary>
    /// Creates a typed error from payload and message.
    /// </summary>
    /// <param name="value">The typed error payload.</param>
    /// <param name="message">Error message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new <see cref="Error{T}"/> value.</returns>
    public static Error<T> From(T value, string message, string? code = null) => new(value, message, code);

    /// <summary>
    /// Converts this typed error into an <see cref="Exception"/>.
    /// </summary>
    /// <returns>The associated <see cref="Exception"/> if present; otherwise a new <see cref="ResultException"/>.</returns>
    public Exception ToException()
        => this.Exception ?? new ResultException(this.Message, this);

    /// <summary>
    /// Returns the display representation of this typed error.
    /// </summary>
    /// <returns>A diagnostic <see cref="string"/>.</returns>
    public override string ToString() => $"{nameof(Error<T>)}<{typeof(T).Name}>: {Message}";
}

/// <summary>
/// Represents a common error contract.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IError error = Error.From("boom");
/// Console.WriteLine(error.Message);
/// </code>
/// </example>
/// </remarks>
public interface IError
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <value>The message text.</value>
    string Message { get; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <value>Optional code string, if available.</value>
    string? Code { get; }

    /// <summary>
    /// Gets an optional nested error cause.
    /// </summary>
    /// <value>The nested error, if available.</value>
    IError? Cause { get; }

    /// <summary>
    /// Gets an optional exception payload.
    /// </summary>
    /// <value>The attached exception, if any.</value>
    Exception? Exception { get; }
}

/// <summary>
/// Represents a typed error contract.
/// </summary>
/// <typeparam name="T">The type of the error payload.</typeparam>
/// <example>
/// <code lang="csharp">
/// IError&lt;int&gt; error = Error&lt;int&gt;.From(1, "bad");
/// Console.WriteLine(error.Value);
/// </code>
/// </example>
public interface IError<T> : IError
{
    /// <summary>
    /// Gets the typed error payload.
    /// </summary>
    /// <value>The payload of type <typeparamref name="T"/>.</value>
    T Value { get; }
}