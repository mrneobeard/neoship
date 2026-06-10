using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

/// <summary>
/// Represents a typed result with a value of type <typeparamref name="T"/> and typed error of <typeparamref name="E"/>.
/// </summary>
/// <typeparam name="T">Success value type.</typeparam>
/// <typeparam name="E">Error payload type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = Result.Ok&lt;int, string&gt;(1);
/// if (result.HasError) { var e = result.ErrorOrDefault(); }
/// </code>
/// </example>
/// </remarks>
[Union]
public class Result<T, E> : IUnion, IResult<T, E>
{
    private readonly Error<E> error;

    private readonly T? value;

    private readonly bool ok;

    /// <summary>
    /// Initializes a success result with a value.
    /// </summary>
    /// <param name="value">Success payload.</param>
    public Result(T value)
    {
        this.error = Error<E>.Empty;
        this.value = value;
        this.ok = true;
    }

    /// <summary>
    /// Initializes an error result from typed error.
    /// </summary>
    /// <param name="error">Typed error payload.</param>
    public Result(Error<E> error)
    {
        this.error = error;
        this.value = default;
        this.ok = false;
    }

    /// <summary>
    /// Gets whether this result is successful.
    /// </summary>
    /// <value><see langword="true"/> when value is available.</value>
    public bool HasValue => this.ok;

    /// <summary>
    /// Gets whether this result is in error state.
    /// </summary>
    /// <value><see langword="true"/> when error is present.</value>
    public bool HasError => !this.ok;

    /// <summary>
    /// Gets either success value or error payload.
    /// </summary>
    /// <value>Value when success; otherwise error.</value>
    public object? Value
    {
        get
        {
            return this.value is null ? null : this.value;
        }
    }

    /// <summary>
    /// Converts a result to a completed task.
    /// </summary>
    /// <param name="result">The source result.</param>
    /// <returns>A completed <see cref="Task{TResult}"/>.</returns>
    public static implicit operator Task<Result<T, E>>(Result<T, E> result) => Task.FromResult(result);

    /// <summary>
    /// Unwraps a completed task into a result.
    /// </summary>
    /// <param name="result">Completed task.</param>
    /// <returns>Unwrapped result value.</returns>
    public static implicit operator Result<T, E>(Task<Result<T, E>> result) => result.Result;

    /// <summary>
    /// Converts result to completed value-task.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>A completed <see cref="ValueTask{TResult}"/>.</returns>
    public static implicit operator ValueTask<Result<T, E>>(Result<T, E> result) => ValueTask.FromResult(result);

    /// <summary>
    /// Unwraps completed value-task into result.
    /// </summary>
    /// <param name="result">Completed value-task.</param>
    /// <returns>Unwrapped result value.</returns>
    public static implicit operator Result<T, E>(ValueTask<Result<T, E>> result) => result.Result;

    /// <summary>
    /// Converts a typed value into success state.
    /// </summary>
    /// <param name="value">Success value.</param>
    /// <returns>A successful <see cref="Result{T, E}"/>.</returns>
    public static implicit operator Result<T, E>(T value) => new(value);

    /// <summary>
    /// Converts typed error into error state.
    /// </summary>
    /// <param name="error">Typed error value.</param>
    /// <returns>An error <see cref="Result{T, E}"/>.</returns>
    public static implicit operator Result<T, E>(Error<E> error) => new(error);

    /// <summary>
    /// Converts <see cref="Result{T,E}"/> to <see cref="Result{T}"/> if possible.
    /// </summary>
    /// <param name="result">Source typed result.</param>
    /// <returns>A value result of <typeparamref name="T"/>.</returns>
    public static implicit operator Result<T>(Result<T, E> result)
    {
        if (result.ok)
            return new(result.value!);

        var e = result.error;

        if (e.Cause is not null)
            return new Error(e.Cause, e.Message, e.Code);

        if (e.Exception is not null)
            return new Error(e.Exception, e.Message, e.Code);

        return new Error(e.Message, e.Code);
    }

    /// <summary>
    /// Converts <see cref="Result{T,E}"/> to <see cref="Result"/>.
    /// </summary>
    /// <param name="result">Source typed result.</param>
    /// <returns>An untyped <see cref="Result"/>.</returns>
    public static implicit operator Result(Result<T, E> result)
    {
        if (result.ok)
            return Result.OkDefault;

        var e = result.error;

        if (e.Cause is not null)
            return new Error(e.Cause, e.Message, e.Code);

        if (e.Exception is not null)
            return new Error(e.Exception, e.Message, e.Code);

        return new Error(e.Message, e.Code);
    }

    /// <summary>
    /// Converts result to success value or throws when in error.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>The success value.</returns>
    /// <exception cref="ResultException">Thrown when in error.</exception>
    public static implicit operator T(Result<T, E> result)
    {
        if (!result.ok)
            throw new ResultException("Result is in error state.");

        return result.value!;
    }

    /// <summary>
    /// Converts result to typed error or throws when successful.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>Typed error payload.</returns>
    /// <exception cref="ResultException">Thrown when successful.</exception>
    public static implicit operator Error<E>(Result<T, E> result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    /// <summary>
    /// Creates successful result.
    /// </summary>
    /// <param name="value">Success value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T, E> Ok(T value) => new(value);

    /// <summary>
    /// Creates an error result from typed error.
    /// </summary>
    /// <param name="error">Typed error.</param>
    /// <returns>An error result.</returns>
    public static Result<T, E> Fail(Error<E> error) => new(error);

    /// <summary>
    /// Creates typed error from value and exception.
    /// </summary>
    /// <param name="value">Error payload.</param>
    /// <param name="exception">Source exception.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="code">Optional code.</param>
    /// <returns>An error result.</returns>
    public static Result<T, E> Fail(E value, Exception exception, string? message = null, string? code = null) => new(Error<E>.From(value, exception, message, code));

    /// <summary>
    /// Creates typed error from value and cause.
    /// </summary>
    /// <param name="value">Error payload.</param>
    /// <param name="cause">Cause error.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="code">Optional code.</param>
    /// <returns>An error result.</returns>
    public static Result<T, E> Fail(E value, IError cause, string? message = null, string? code = null) => new(Error<E>.From(value, cause, message, code));

    /// <summary>
    /// Creates typed error from value and message.
    /// </summary>
    /// <param name="value">Error payload.</param>
    /// <param name="message">Failure message.</param>
    /// <param name="code">Optional code.</param>
    /// <returns>An error result.</returns>
    public static Result<T, E> Fail(E value, string message, string? code = null) => new(Error<E>.From(value, message, code));

    /// <summary>
    /// Gets current typed error or default.
    /// </summary>
    /// <returns>Typed error if in error; otherwise default.</returns>
    public Error<E> ErrorOrDefault()
    {
        if (!this.ok)
            return this.error;

        return default!;
    }

    /// <summary>
    /// Gets current error or fallback factory result.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory used when success.</param>
    /// <returns>Current error or fallback.</returns>
    public Error<E> ErrorOrDefault(Func<Error<E>> defaultValueFactory)
    {
        if (this.ok)
            return defaultValueFactory();

        return this.error;
    }

    /// <summary>
    /// Gets current error payload value or fallback.
    /// </summary>
    /// <param name="defaultValueFactory">Factory producing fallback payload.</param>
    /// <returns>Typed error when error; otherwise converted payload fallback.</returns>
    public Error<E> ErrorOrDefault(Func<E> defaultValueFactory)
    {
        if (this.ok)
            return defaultValueFactory();

        return this.error;
    }

    /// <summary>
    /// Invokes <paramref name="inspector"/> with value when available.
    /// </summary>
    /// <param name="inspector">Inspector action.</param>
    public void Inspect(Action<T> inspector)
    {
        if (this.ok)
            inspector(this.value!);
    }

    /// <summary>
    /// Maps value into another value or returns default.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Mapping function.</param>
    /// <returns>Mapped value when success; otherwise default.</returns>
    public U Map<U>(Func<T, U> map)
    {
        if (this.ok)
            return map(this.value!);

        return default!;
    }

    /// <summary>
    /// Maps value or fallback when error.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="factory">Fallback function.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.ok)
            return map(this.value!);

        return factory();
    }

    /// <summary>
    /// Maps value or error into one return value.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping.</param>
    /// <param name="factory">Error mapping.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<Error<E>, U> factory)
    {
        if (this.ok)
            return map(this.value!);

        return factory(this.error);
    }

    /// <summary>
    /// Maps typed error.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <returns>Mapped value when error; otherwise default.</returns>
    public U MapError<U>(Func<Error<E>, U> map)
    {
        if (!this.ok)
            return map(this.error);

        return default!;
    }

    /// <summary>
    /// Maps typed error or fallback for success.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <param name="factory">Success fallback.</param>
    /// <returns>Mapped value.</returns>
    public U MapError<U>(Func<Error<E>, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    /// <summary>
    /// Maps error payload value with provided mapper.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error payload mapper.</param>
    /// <returns>Mapped value when error; otherwise default.</returns>
    public U MapError<U>(Func<E, U> map)
    {
        if (!this.ok)
            return map(this.error.Value);

        return default!;
    }

    /// <summary>
    /// Maps value or payload value into one return type.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Value mapping function.</param>
    /// <param name="errorMap">Error payload mapping function.</param>
    /// <returns>Mapped value from success/error branch.</returns>
    public U MapEither<U>(Func<T, U> map, Func<E, U> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error.Value);
    }

    /// <summary>
    /// Maps value into <typeparamref name="U"/> when successful.
    /// </summary>
    /// <typeparam name="U">Mapped success type.</typeparam>
    /// <param name="mapValue">Value mapping function.</param>
    /// <returns>Mapped result.</returns>
    public Result<U, E> MapResult<U>(Func<T, U> mapValue)
    {
        if (this.ok)
            return new(mapValue(this.value!));

        return new(this.error);
    }

    /// <summary>
    /// Maps value and typed error into another typed result.
    /// </summary>
    /// <typeparam name="U">Mapped success type.</typeparam>
    /// <typeparam name="V">Mapped error type.</typeparam>
    /// <param name="mapValue">Success mapping function.</param>
    /// <param name="mapError">Error mapping function.</param>
    /// <returns>Mapped result.</returns>
    public Result<U, V> MapResult<U, V>(Func<T, U> mapValue, Func<Error<E>, Error<V>> mapError)
    {
        if (this.ok)
            return new(mapValue(this.value!));

        return new(mapError(this.error));
    }

    /// <summary>
    /// Matches on value when available.
    /// </summary>
    /// <param name="map">Success matcher.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map)
    {
        if (this.ok)
            return map(this.value!);

        return false;
    }

    /// <summary>
    /// Matches on success value or typed error.
    /// </summary>
    /// <param name="map">Success matcher.</param>
    /// <param name="errorMap">Error matcher.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map, Func<Error<E>, bool> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error);
    }

    /// <summary>
    /// Matches on success value or typed payload value.
    /// </summary>
    /// <param name="map">Success matcher.</param>
    /// <param name="errorMap">Error payload matcher.</param>
    /// <returns>Boolean match result.</returns>
    public bool Match(Func<T, bool> map, Func<E, bool> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error.Value);
    }

    /// <summary>
    /// Returns this value when success, or fallback on error.
    /// </summary>
    /// <param name="value">Fallback value.</param>
    /// <returns>Current success result or fallback value result.</returns>
    public Result<T, E> Or(T value)
    {
        if (this.ok)
            return this;

        return new(value);
    }

    /// <summary>
    /// Returns this value when success, or fallback via factory when error.
    /// </summary>
    /// <param name="valueFactory">Fallback factory.</param>
    /// <returns>Current success result or fallback value result.</returns>
    public Result<T, E> Or(Func<T> valueFactory)
    {
        if (this.ok)
            return this;

        return new(valueFactory());
    }

    /// <summary>
    /// Attempts to extract typed error.
    /// </summary>
    /// <param name="error">Output typed error.</param>
    /// <returns>True when error exists.</returns>
    public bool TryGetError(out Error<E> error)
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
    /// Attempts to extract value.
    /// </summary>
    /// <param name="value">Output success value.</param>
    /// <returns>True when value exists.</returns>
    public bool TryGetValue(out T value)
    {
        if (this.ok)
        {
            value = this.value!;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets current value or default.
    /// </summary>
    /// <returns>Success value or default.</returns>
    public T ValueOrDefault()
       => this.ok ? this.value! : default!;

    /// <summary>
    /// Gets value or fallback from factory.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Value when success; otherwise fallback.</returns>
    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.ok)
            return this.value!;

        return defaultValueFactory();
    }

    /// <summary>
    /// Gets value or explicit fallback value.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value when success; otherwise fallback.</returns>
    public T ValueOrDefault(T defaultValue)
    {
        if (this.ok)
            return this.value!;

        return defaultValue;
    }

    /// <summary>
    /// Returns value or throws on error.
    /// </summary>
    /// <returns>Success value.</returns>
    /// <exception cref="ResultException">Thrown on error.</exception>
    public T ValueOrThrow()
    {
        if (this.ok)
            return this.value!;

        throw this.error.ToException();
    }

    /// <summary>
    /// Explicit interface implementation returning typed error as interface.
    /// </summary>
    /// <returns><see cref="IError{E}"/>.</returns>
    IError<E> IResult<T, E>.ErrorOrDefault()
        => this.ErrorOrDefault();

    /// <summary>
    /// Explicit interface implementation for fallback typed error default.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Typed error interface.</returns>
    IError<E> IResult<T, E>.ErrorOrDefault(Func<IError<E>> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }

    /// <summary>
    /// Explicit interface implementation for IResult&lt;T&gt;.
    /// </summary>
    /// <param name="error">Output error.
    /// </param>
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
    /// Explicit interface implementation for non-generic IResult.
    /// </summary>
    /// <returns>Current error or default.</returns>
    IError IResult.ErrorOrDefault()
        => this.ErrorOrDefault();

    /// <summary>
    /// Explicit interface implementation for <see cref="IResult&lt;T, E&gt;"/> typed payload.
    /// </summary>
    /// <param name="error">Output typed error.</param>
    /// <returns>True when error exists.</returns>
    bool IResult<T, E>.TryGetError(out IError<E> error)
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
    /// Explicit interface implementation for non-generic error default.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Fallback interface error.</returns>
    IError IResult.ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }
}