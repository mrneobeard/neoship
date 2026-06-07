using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

/// <summary>
/// Represents an untyped result with a success state and optional error payload.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = Result.Ok();
/// if (result.HasValue) { /* success */ }
/// </code>
/// </example>
/// </remarks>
[Union]
public sealed class Result : IResult
{
    private readonly Error error;

    private readonly bool ok;

    private readonly Never never;

    /// <summary>
    /// Initializes a new success value result with <see cref="Never.Value"/>.
    /// </summary>
    /// <example>
    /// <code lang="csharp">
    /// var result = new Result();
    /// </code>
    /// </example>
    public Result()
    {
        this.error = Error.Empty;
        this.ok = true;
        this.never = Never.Value;
    }

    /// <summary>
    /// Initializes a new error-state result from an <see cref="Error"/>.
    /// </summary>
    /// <param name="error">The error payload.</param>
    /// <example>
    /// <code lang="csharp">
    /// var result = new Result(Error.From("failed"));
    /// </code>
    /// </example>
    public Result(Error error)
    {
        this.error = error;
        this.ok = false;
        this.never = Never.Value;
    }

    /// <summary>
    /// Gets either the value or the error payload depending on state.
    /// </summary>
    /// <value>
    /// A <see cref="Never"/> when successful; otherwise an <see cref="Error"/>.
    /// </value>
    public object? Value => this.ok ? this.never : this.error;

    /// <summary>
    /// Gets whether this result is successful.
    /// </summary>
    /// <value><see langword="true"/> when no error is present.</value>
    public bool HasValue => this.ok;

    /// <summary>
    /// Gets whether this result is in error state.
    /// </summary>
    /// <value><see langword="true"/> when an error is present.</value>
    public bool HasError => !this.ok;

    /// <summary>
    /// Converts an <see cref="Error"/> into a successful typed <see cref="Result"/>.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A result in error state.</returns>
    public static implicit operator Result(Error error) => new(error);

    /// <summary>
    /// Converts a <see cref="Result"/> to a completed <see cref="Task{TResult}"/>.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>A completed <see cref="Task{Result}"/>.</returns>
    public static implicit operator Task<Result>(Result result) => Task.FromResult(result);

    /// <summary>
    /// Blocks and unwraps a completed result task.
    /// </summary>
    /// <param name="result">Task containing a result.</param>
    /// <returns>The task result value.</returns>
    public static implicit operator Result(Task<Result> result) => result.Result;

    /// <summary>
    /// Converts a <see cref="Result"/> to a completed <see cref="ValueTask{TResult}"/>.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>A completed <see cref="ValueTask{Result}"/>.</returns>
    public static implicit operator ValueTask<Result>(Result result) => ValueTask.FromResult(result);

    /// <summary>
    /// Blocks and unwraps a completed result value task.
    /// </summary>
    /// <param name="result">Value task containing a result.</param>
    /// <returns>The task result value.</returns>
    public static implicit operator Result(ValueTask<Result> result) => result.Result;

    /// <summary>
    /// Converts an <see cref="Exception"/> into an error result.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <returns>A result in error state.</returns>
    public static implicit operator Result(Exception exception) => new(exception);

    /// <summary>
    /// Converts <see cref="Never"/> into a successful result.
    /// </summary>
    /// <param name="never">The marker value.</param>
    /// <returns>A successful result.</returns>
    public static implicit operator Result(Never never) => new();

    /// <summary>
    /// Converts a message into an error result.
    /// </summary>
    /// <param name="message">The message to wrap as error.</param>
    /// <returns>An error-state <see cref="Result"/>.</returns>
    public static implicit operator Result(string message) => new(message);

    /// <summary>
    /// Converts a result of typed none into an untyped <see cref="Result"/>.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>An untyped <see cref="Result"/> equivalent.</returns>
    public static implicit operator Result(Result<Never> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    /// <summary>
    /// Converts a unit result into an untyped <see cref="Result"/>.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>An untyped <see cref="Result"/> equivalent.</returns>
    public static implicit operator Result(Result<ValueTuple> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    /// <summary>
    /// Converts a typed result with typed error into an untyped result.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>An untyped <see cref="Result"/> equivalent.</returns>
    public static implicit operator Result(Result<Never, Error> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    /// <summary>
    /// Converts a unit typed result with error into an untyped result.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>An untyped <see cref="Result"/> equivalent.</returns>
    public static implicit operator Result(Result<ValueTuple, Error> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    /// <summary>
    /// Converts a result into an error payload when in error state.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>The error payload.</returns>
    /// <exception cref="ResultException">Thrown when the result is successful.</exception>
    public static implicit operator Error(Result result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    /// <summary>
    /// Gets a shared successful empty result value.
    /// </summary>
    /// <value>A successful <see cref="Result"/>.</value>
    public static Result OkDefault { get; } = new();

    /// <summary>
    /// Returns a shared successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Ok() => OkDefault;

    /// <summary>
    /// Creates a successful typed result from a value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The typed value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Ok<T>(T value) => new(value);

    /// <summary>
    /// Creates a successful typed result from value and error type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="E">The error payload type.</typeparam>
    /// <param name="value">The value payload.</param>
    /// <returns>A successful <see cref="Result{T, E}"/>.</returns>
    public static Result<T, E> Ok<T, E>(T value) => new(value);

    /// <summary>
    /// Creates an error result from non-generic error.
    /// </summary>
    /// <param name="error">The error payload.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(Error error) => new(error);

    /// <summary>
    /// Creates an error result from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(Exception exception, string? message = null, string? code = null) => new(Error.From(exception, message, code));

    /// <summary>
    /// Creates an error result from a typed cause.
    /// </summary>
    /// <param name="cause">The error cause.</param>
    /// <param name="message">Optional replacement message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(IError cause, string? message = null, string? code = null) => new(Error.From(cause, message, code));

    /// <summary>
    /// Creates an error result with message text.
    /// </summary>
    /// <param name="message">Failure message.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Fail(string message, string? code = null) => new(Error.From(message, code));

    /// <summary>
    /// Creates a typed error result.
    /// </summary>
    /// <typeparam name="T">Value type for the typed result.</typeparam>
    /// <typeparam name="E">Error payload type.</typeparam>
    /// <param name="error">The typed error.</param>
    /// <returns>A failed <see cref="Result{T, E}"/>.</returns>
    public static Result<T, E> Fail<T, E>(Error<E> error) => new(error);

    /// <summary>
    /// Executes <paramref name="action"/> and wraps exceptions as failure.
    /// </summary>
    /// <param name="action">Action to execute.</param>
    /// <returns>
    /// <see cref="Result"/> in success state when action completes; otherwise failure.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// var result = Result.Try(() =&gt; DoWork());
    /// </code>
    /// </example>
    public static Result Try(Action action)
    {
        try
        {
            action();
            return Ok();
        }
        catch (Exception ex)
        {
            return Fail(ex);
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> and wraps the return value.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Function to execute.</param>
    /// <returns>
    /// <see cref="Result{T}"/> in success state when action returns; otherwise failure.
    /// </returns>
    public static Result<T> Try<T>(Func<T> action)
    {
        try
        {
            return Ok(action());
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> and maps exceptions to an error value.
    /// </summary>
    /// <typeparam name="T">Success value type.</typeparam>
    /// <typeparam name="E">Error payload type.</typeparam>
    /// <param name="action">Function to execute.</param>
    /// <param name="errorFactory">Factory mapping exceptions to <typeparamref name="E"/>.</param>
    /// <returns>
    /// <see cref="Result{T, E}"/> on success or mapped error.
    /// </returns>
    public static Result<T, E> Try<T, E>(Func<T> action, Func<Exception, E> errorFactory)
    {
        try
        {
            var value = action();
            return value;
        }
        catch (Exception ex)
        {
            return new Result<T, E>(errorFactory(ex));
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> and maps exceptions to a typed error.
    /// </summary>
    /// <typeparam name="T">Success value type.</typeparam>
    /// <typeparam name="E">Error payload type.</typeparam>
    /// <param name="action">Function to execute.</param>
    /// <param name="errorFactory">Factory mapping exceptions to <see cref="Error{E}"/>.</param>
    /// <returns>
    /// <see cref="Result{T, E}"/> on success or mapped error.
    /// </returns>
    public static Result<T, E> Try<T, E>(Func<T> action, Func<Exception, Error<E>> errorFactory)
    {
        try
        {
            var value = action();
            return value;
        }
        catch (Exception ex)
        {
            return errorFactory(ex);
        }
    }

    /// <summary>
    /// Asynchronously executes <paramref name="action"/> with cancellation and wraps exceptions.
    /// </summary>
    /// <param name="action">Async action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task containing a successful <see cref="Result"/> or failure.
    /// </returns>
    public static async Task<Result> TryAsync(Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await action(cancellationToken).ConfigureAwait(false);
            return new();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }


    /// <summary>
    /// Asynchronously executes <paramref name="action"/> and wraps exceptions.
    /// </summary>
    /// <param name="action">Async action.</param>
    /// <returns>
    /// A task containing a successful <see cref="Result"/> or failure.
    /// </returns>
    public static async Task<Result> TryAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
            return new();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Asynchronously executes a function and wraps return value or exceptions.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Async function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task containing <see cref="Result{T}"/> or failure.
    /// </returns>
    public static async Task<Result<T>> TryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await action(cancellationToken).ConfigureAwait(false);
            return new(value);
        } 
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Asynchronously executes a function and wraps return value or exceptions.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Async function.</param>
    /// <returns>
    /// A task containing <see cref="Result{T}"/> or failure.
    /// </returns>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var value = await action().ConfigureAwait(false);
            return new(value);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Asynchronously executes a function and maps exceptions to typed errors.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <typeparam name="E">Error payload type.</typeparam>
    /// <param name="action">Async function.</param>
    /// <param name="errorFactory">Factory for mapping <see cref="Exception"/> to <typeparamref name="E"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task containing <see cref="Result{T, E}"/> or mapped failure.
    /// </returns>
    public static async Task<Result<T, E>> TryAsync<T, E>(Func<CancellationToken, Task<T>> action, Func<Exception, E> errorFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await action(cancellationToken);
            return new(value);
        } 
        catch (Exception ex)
        {
            return new Result<T, E>(errorFactory(ex));
        }
    }

    /// <summary>
    /// Gets the current error or default when successful.
    /// </summary>
    /// <returns>An <see cref="Error"/> when in error state; otherwise <see langword="default"/>.</returns>
    public IError ErrorOrDefault()
    {
        if (!this.ok)
            return this.error;

        return default!;
    }

    /// <summary>
    /// Gets the current error or fallback value.
    /// </summary>
    /// <param name="defaultValueFactory">Factory used when successful.</param>
    /// <returns>An <see cref="Error"/> when in error state; otherwise fallback.</returns>
    public IError ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.ok)
            return defaultValueFactory();

        return this.error;
    }

    /// <summary>
    /// Gets the current error or fallback value.
    /// </summary>
    /// <param name="defaultValue">Fallback value when successful.</param>
    /// <returns>An <see cref="Error"/> when in error state; otherwise <paramref name="defaultValue"/>.</returns>
    public Error ErrorOrDefault(Error defaultValue)
    {
        if (!this.ok)
            return this.error;

        return defaultValue;
    }
    /// <summary>
    /// Tries to extract the current error payload.
    /// </summary>
    /// <param name="error">The extracted <see cref="Error"/> when present.</param>
    /// <returns><see langword="true"/> when an error is present; otherwise <see langword="false"/>.</returns>
    public bool TryGetError(out Error error)
    {
        if (this.ok)
        {
            error = default!;
            return false;
        }

        error = this.error;
        return true;
    }

    /// <summary>
    /// Invokes <paramref name="map"/> for success state and returns <see langword="default"/> otherwise.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Delegate invoked on success.</param>
    /// <returns>
    /// The mapped value when success; otherwise <see langword="default"/>.
    /// </returns>
    public U Map<U>(Func<U> map)
    {
        if (this.ok)
            return map();

        return default!;
    }

    /// <summary>
    /// Invokes <paramref name="map"/> for success, otherwise <paramref name="factory"/>.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Delegate invoked on success.</param>
    /// <param name="factory">Fallback delegate for error state.</param>
    /// <returns>Mapped value or fallback.</returns>
    public U MapError<U>(Func<Error, U> map)
    {
        if (!this.ok)
            return map(this.error);

        return default!;
    }

    /// <summary>
    /// Invokes <paramref name="map"/> for error or <paramref name="factory"/> on success.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Delegate invoked on error.</param>
    /// <param name="factory">Delegate invoked on success.</param>
    /// <returns>Mapped value from either path.</returns>
    public U MapError<U>(Func<Error, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    /// <summary>
    /// Maps success or error into one return value.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="errorMap">Error mapping function.</param>
    /// <returns>Mapped value from one of the two delegates.</returns>
    public U MapEither<U>(Func<U> map, Func<Error, U> errorMap)
    {
        if (this.ok)
            return map();

        return errorMap(this.error);
    }
    
    /// <summary>
    /// Maps success to <typeparamref name="U"/> and errors to <typeparamref name="E"/>.
    /// </summary>
    /// <typeparam name="U">Mapped success type.</typeparam>
    /// <typeparam name="E">Mapped error type.</typeparam>
    /// <param name="mapValue">Success mapping function.</param>
    /// <param name="mapError">Error mapping function.</param>
    /// <returns>A <see cref="Result{U, E}"/>.</returns>
    public Result<U, E> MapResult<U, E>(Func<U> mapValue, Func<Error, E> mapError)
    {
        if (this.ok)
            return new(mapValue());

        return new(mapError(this.error));
    }
}
