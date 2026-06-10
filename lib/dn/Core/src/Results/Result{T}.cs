using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

/// <summary>
/// Represents a typed result with a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The result value type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Result&lt;int&gt; result = Result.Ok(1);
/// if (result.HasValue) { int value = result.ValueOrDefault(); }
/// </code>
/// </example>
/// </remarks>
[Union]
public sealed class Result<T> : IUnion, IResult<T>
{
    private readonly T value;

    private readonly Error error;

    private readonly bool ok;

    /// <summary>
    /// Initializes a success result with <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The success payload.</param>
    public Result(T value)
    {
        this.value = value;
        this.ok = true;
        this.error = Error.Empty;
    }

    /// <summary>
    /// Initializes an error result.
    /// </summary>
    /// <param name="error">The error payload.</param>
    public Result(Error error)
    {
        this.value = default!;
        this.ok = false;
        this.error = error;
    }

    /// <summary>
    /// Gets whether this result is successful.
    /// </summary>
    /// <value><see langword="true"/> when value is available.</value>
    public bool HasValue => this.ok;

    /// <summary>
    /// Gets whether this result is in error.
    /// </summary>
    /// <value><see langword="true"/> when error is present.</value>
    public bool HasError => !this.ok;

    /// <summary>
    /// Gets either <typeparamref name="T"/> value or <see cref="Error"/>.
    /// </summary>
    /// <value>The value when successful; otherwise an error.</value>
    public object? Value => this.ok ? this.value : this.error;

    /// <summary>
    /// Wraps a result in a completed task.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>A completed <see cref="Task{TResult}"/>.</returns>
    public static implicit operator Task<Result<T>>(Result<T> result) => Task.FromResult(result);

    /// <summary>
    /// Unwraps a completed result task.
    /// </summary>
    /// <param name="result">Completed task.</param>
    /// <returns>The task result.</returns>
    public static implicit operator Result<T>(Task<Result<T>> result) => result.Result;

    /// <summary>
    /// Converts a result to a completed value-task.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>A completed <see cref="ValueTask{TResult}"/>.</returns>
    public static implicit operator ValueTask<Result<T>>(Result<T> result) => ValueTask.FromResult(result);

    /// <summary>
    /// Unwraps a completed value-task result.
    /// </summary>
    /// <param name="result">Completed value-task.</param>
    /// <returns>The task result.</returns>
    public static implicit operator Result<T>(ValueTask<Result<T>> result) => result.Result;

    /// <summary>
    /// Converts a value into a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result.</returns>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>
    /// Converts an <see cref="Error"/> to an error result.
    /// </summary>
    /// <param name="error">The error payload.</param>
    /// <returns>An error result.</returns>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>
    /// Converts an exception to an error result.
    /// </summary>
    /// <param name="exception">The source exception.</param>
    /// <returns>An error result.</returns>
    public static implicit operator Result<T>(Exception exception) => new(exception);

    /// <summary>
    /// Converts result into value or throws when error.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>The success value.</returns>
    /// <exception cref="ResultException">Thrown when in error state.</exception>
    public static implicit operator T(Result<T> result)
    {
        if (!result.ok)
            throw new ResultException("Result is in error state.");

        return result.value;
    }

    /// <summary>
    /// Converts result into error or throws when success.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>The error payload.</returns>
    /// <exception cref="ResultException">Thrown when in success state.</exception>
    public static implicit operator Error(Result<T> result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    /// <summary>
    /// Gets current error or default when successful.
    /// </summary>
    /// <returns><see cref="Error"/> when error state; otherwise default.</returns>
    public Error ErrorOrDefault()
    {
        if (!this.ok)
            return this.error;

        return default!;
    }

    /// <summary>
    /// Gets error or fallback value.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory used when successful.</param>
    /// <returns>Error when in error; otherwise fallback.</returns>
    public Error ErrorOrDefault(Func<Error> defaultValueFactory)
    {
        if (this.ok)
            return defaultValueFactory();

        return this.error;
    }

    /// <summary>
    /// Gets error or specified fallback.
    /// </summary>
    /// <param name="defaultValue">Fallback error value.</param>
    /// <returns>Error when in error; otherwise fallback.</returns>
    public Error ErrorOrDefault(Error defaultValue)
    {
        if (!this.ok)
            return this.error;

        return defaultValue;
    }

    /// <summary>
    /// Invokes <paramref name="inspector"/> with the value when present.
    /// </summary>
    /// <param name="inspector">Action to invoke.</param>
    public void Inspect(Action<T> inspector)
    {
        if (this.ok)
            inspector(this.value!);
    }

    /// <summary>
    /// Maps value into a new return type when successful.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Mapping function.</param>
    /// <returns>Mapped value or default.</returns>
    public U Map<U>(Func<T, U> map)
    {
        if (this.ok)
            return map(this.value);

        return default!;
    }

    /// <summary>
    /// Maps value or fallback when error.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="factory">Fallback factory.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory();
    }

    /// <summary>
    /// Maps value or maps error to fallback.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="factory">Error mapping function.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<Error, U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory(this.error);
    }

    /// <summary>
    /// Maps errors or returns default when successful.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <returns>Mapped value or default.</returns>
    public U MapError<U>(Func<Error, U> map)
    {
        if (!this.ok)
            return map(this.error);

        return default!;
    }

    /// <summary>
    /// Maps errors with fallback value.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <param name="factory">Success fallback.</param>
    /// <returns>Mapped value.</returns>
    public U MapError<U>(Func<Error, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    /// <summary>
    /// Maps exceptions for error state.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error exception mapping function.</param>
    /// <returns>Mapped value or default.</returns>
    public U MapError<U>(Func<Exception, U> map)
    {
        if (!this.ok)
            return map(this.error.Exception!);

        return default!;
    }

    /// <summary>
    /// Maps error exceptions or uses factory on success.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <param name="factory">Success fallback.</param>
    /// <returns>Mapped value.</returns>
    public U MapError<U>(Func<Exception, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error.Exception!);

        return factory();
    }

    /// <summary>
    /// Maps success value or exceptions to a single return type.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="errorMap">Error mapping function.</param>
    /// <returns>Mapped value from either state.</returns>
    public U MapEither<U>(Func<T, U> map, Func<Exception, U> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error.Exception!);
    }

    /// <summary>
    /// Maps to another <see cref="Result{U}"/> when successful.
    /// </summary>
    /// <typeparam name="U">New value type.</typeparam>
    /// <param name="mapValue">Mapper function.</param>
    /// <returns>Mapped typed result.</returns>
    public Result<U> MapResult<U>(Func<T, U> mapValue)
    {
        if (this.ok)
            return new(mapValue(this.value));

        return new(this.error);
    }

    /// <summary>
    /// Maps to typed result with mapped error type.
    /// </summary>
    /// <typeparam name="U">New value type.</typeparam>
    /// <typeparam name="E">Mapped error type.</typeparam>
    /// <param name="mapValue">Success mapping function.</param>
    /// <param name="mapError">Error mapping function.</param>
    /// <returns>Mapped typed result.</returns>
    public Result<U, E> MapResult<U, E>(Func<T, U> mapValue, Func<Error, E> mapError)
    {
        if (this.ok)
            return new(mapValue(this.value));

        return new(mapError(this.error));
    }

    /// <summary>
    /// Matches on value when present.
    /// </summary>
    /// <param name="map">Predicate for success value.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map)
    {
        if (this.ok)
            return map(this.value);

        return false;
    }

    /// <summary>
    /// Matches value or error into a boolean result.
    /// </summary>
    /// <param name="map">Success match predicate.</param>
    /// <param name="errorMap">Error match predicate.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map, Func<Error, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error);
    }

    /// <summary>
    /// Matches value or exception state into a boolean result.
    /// </summary>
    /// <param name="map">Success match predicate.</param>
    /// <param name="errorMap">Exception match predicate.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map, Func<Exception, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error.Exception!);
    }

    /// <summary>
    /// Returns current result or fallback value when error.
    /// </summary>
    /// <param name="value">Fallback value.</param>
    /// <returns>Current successful result, or fallback.</returns>
    public Result<T> Or(T value)
    {
        if (this.ok)
            return this;

        return new(value);
    }

    /// <summary>
    /// Returns current result or mapped fallback value when error.
    /// </summary>
    /// <param name="valueFactory">Fallback factory.</param>
    /// <returns>Current successful result, or fallback.</returns>
    public Result<T> Or(Func<T> valueFactory)
    {
        if (this.ok)
            return this;

        return new(valueFactory());
    }

    /// <summary>
    /// Tries to extract error payload.
    /// </summary>
    /// <param name="error">Error output.</param>
    /// <returns>True when error present.</returns>
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
    /// Tries to extract value payload.
    /// </summary>
    /// <param name="value">Value output.</param>
    /// <returns>True when value present.</returns>
    public bool TryGetValue(out T value)
    {
        if (this.ok)
        {
            value = this.value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to extract value from interface contract.
    /// </summary>
    /// <param name="error">Error output.</param>
    /// <returns>True when error present.</returns>
    public bool TryGetValue(out Error error)
    {
        if (this.ok)
        {
            error = this.error;
            return false;
        }

        error = default!;
        return false;
    }

    /// <summary>
    /// Gets the success value or default value.
    /// </summary>
    /// <returns>Success value or <see langword="default"/>.</returns>
    public T ValueOrDefault()
        => this.ok ? this.value : default!;

    /// <summary>
    /// Gets value or fallback.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Value when success; otherwise fallback.</returns>
    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.ok)
            return this.value;

        return defaultValueFactory();
    }

    /// <summary>
    /// Gets value or supplied fallback value.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value when success; otherwise fallback.</returns>
    public T ValueOrDefault(T defaultValue)
    {
        if (this.ok)
            return this.value;

        return defaultValue;
    }

    /// <summary>
    /// Returns value or throws if result has error.
    /// </summary>
    /// <returns>Success value.</returns>
    /// <exception cref="ResultException">When in error state.</exception>
    public T ValueOrThrow()
    {
        if (this.ok)
            return this.value;

        throw this.error.ToException();
    }

    /// <summary>
    /// Explicit interface implementation for <see cref="IResult.ErrorOrDefault"/>.
    /// </summary>
    /// <returns><see cref="IError"/> current error or <see langword="null"/>.</returns>
    IError IResult.ErrorOrDefault()
       => this.ErrorOrDefault();

    /// <summary>
    /// Explicit interface implementation for optional error retrieval.
    /// </summary>
    /// <param name="error">Output typed interface error.</param>
    /// <returns>True when error exists.</returns>
    bool IResult<T>.TryGetError(out IError error)
    {
        if (this.HasError)
        {
            error = this.error;
            return true;
        }

        error = default!;
        return false;
    }

    /// <summary>
    /// Explicit interface implementation for error default fallback.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns><see cref="IError"/> current error or fallback.</returns>
    IError IResult.ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }
}