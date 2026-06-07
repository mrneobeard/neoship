using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

/// <summary>
/// Represents an inline result with a value and typed error payload.
/// </summary>
/// <typeparam name="T">Success value type.</typeparam>
/// <typeparam name="E">Error payload type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// ValueResult&lt;int, string&gt; result = new ValueResult&lt;int, string&gt;(1);
/// if (result.HasError) { var e = result.ErrorOrDefault(); }
/// </code>
/// </example>
/// </remarks>
[Union]
public readonly struct ValueResult<T, E> : IResult<T, E>, IUnion
{
    private readonly T value;

    private readonly Error<E> error;

    private readonly bool ok;

    /// <summary>
    /// Initializes a successful inline value result.
    /// </summary>
    /// <param name="value">Success value.</param>
    public ValueResult(T value)
    {
        this.value = value;
        this.error = Error<E>.Empty;
        this.ok = true;
    }

    /// <summary>
    /// Initializes an error inline value result.
    /// </summary>
    /// <param name="error">Typed error payload.</param>
    public ValueResult(Error<E> error)
    {
        this.error = error;
        this.value = default!;
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
    /// Gets value or error depending on state.
    /// </summary>
    /// <value>Value when success; otherwise error.</value>
    public object? Value => this.ok ? this.value : this.error;

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    /// <param name="value">Success value.</param>
    /// <returns>Successful inline result.</returns>
    public static implicit operator ValueResult<T, E>(T value) => new(value);

    /// <summary>
    /// Implicit conversion from typed error to inline result.
    /// </summary>
    /// <param name="error">Typed error payload.</param>
    /// <returns>Error inline result.</returns>
    public static implicit operator ValueResult<T, E>(Error<E> error) => new(error);

    /// <summary>
    /// Implicit conversion to value type.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>Underlying value.</returns>
    public static implicit operator T(ValueResult<T, E> result) => result.value;

    /// <summary>
    /// Implicit conversion to typed error.
    /// </summary>
    /// <param name="result">Source result.</param>
    /// <returns>Typed error payload.</returns>
    public static implicit operator Error<E>(ValueResult<T, E> result) => result.error;

    /// <summary>
    /// Implicit conversion to <see cref="Result{T, E}"/>.
    /// </summary>
    /// <param name="result">Source value result.</param>
    /// <returns>Reference result with same state.</returns>
    public static implicit operator Result<T, E>(ValueResult<T, E> result) => new(result.value);

    /// <summary>
    /// Explicit interface implementation for non-generic error default.
    /// </summary>
    /// <returns>Interface error or default.</returns>
    IError IResult.ErrorOrDefault()
        => this.ErrorOrDefault();

    /// <summary>
    /// Gets typed error or fallback via interface-compatible factory.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Typed interface error.
    /// </returns>
    public IError<E> ErrorOrDefault(Func<IError<E>> defaultValueFactory)
        => this.ErrorOrDefault(defaultValueFactory);

    /// <summary>
    /// Explicit interface implementation returning typed error.
    /// </summary>
    /// <returns>Typed interface error.</returns>
    IError<E> IResult<T, E>.ErrorOrDefault()
        => this.ErrorOrDefault();

    /// <summary>
    /// Gets typed error or fallback from non-typed factory.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Interface error.</returns>
    public IError ErrorOrDefault(Func<IError> defaultValueFactory)
        => this.ErrorOrDefault(defaultValueFactory);

    /// <summary>
    /// Gets current typed error or default.
    /// </summary>
    /// <returns>Typed error when error; otherwise default.</returns>
    public Error<E> ErrorOrDefault()
    {
        if (this.ok)
            return default!;

        return this.error;
    }

    /// <summary>
    /// Gets error or fallback when successful.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Error when error; otherwise fallback.</returns>
    public Error<E> ErrorOrDefault(Func<Error<E>> defaultValueFactory)
    {
        if (!this.ok)
            return this.error;

        return defaultValueFactory();
    }

    /// <summary>
    /// Gets typed error or supplied fallback.
    /// </summary>
    /// <param name="defaultValue">Fallback error value.</param>
    /// <returns>Error when error; otherwise fallback.</returns>
    public Error<E> ErrorOrDefault(Error<E> defaultValue)
    {
        if (this.ok)
            return defaultValue;

        return this.error;
    }

    /// <summary>
    /// Invokes <paramref name="inspector"/> on value when present.
    /// </summary>
    /// <param name="inspector">Action invoked with value.</param>
    public void Inspect(Action<T> inspector)
    {
        if (this.ok)
            inspector(this.value);
    }

    /// <summary>
    /// Maps value to another value if successful.
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
    /// Maps value or fallback factory when error.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Mapping function.</param>
    /// <param name="factory">Fallback function.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory();
    }

    /// <summary>
    /// Maps value or typed error.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Value mapping function.</param>
    /// <param name="factory">Error mapping function.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<Error<E>, U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory(this.error);
    }

    /// <summary>
    /// Maps typed error to a result.
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
    /// Maps typed error with fallback for success.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Error mapping function.</param>
    /// <param name="factory">Fallback function.</param>
    /// <returns>Mapped value.</returns>
    public U MapError<U>(Func<Error<E>, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    /// <summary>
    /// Maps value or error into one return type.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Success mapping function.</param>
    /// <param name="errorMap">Error mapping function.</param>
    /// <returns>Mapped value.</returns>
    public U MapEither<U>(Func<T, U> map, Func<Error<E>, U> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error);
    }

    /// <summary>
    /// Matches on value when available.
    /// </summary>
    /// <param name="map">Success predicate.</param>
    /// <returns>Match result.</returns>
    public bool Match(Func<T, bool> map)
    {
        if (this.ok)
            return map(this.value);

        return false;
    }

    /// <summary>
    /// Matches on value or typed error.
    /// </summary>
    /// <param name="map">Success predicate.</param>
    /// <param name="errorMap">Error predicate.</param>
    /// <returns>Match result.</returns>
    public bool Match(Func<T, bool> map, Func<Error<E>, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error);
    }

    /// <summary>
    /// Matches on value or payload value.
    /// </summary>
    /// <param name="map">Success predicate.</param>
    /// <param name="errorMap">Payload predicate.</param>
    /// <returns>Match result.</returns>
    public bool Match(Func<T, bool> map, Func<E, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error.Value);
    }

    /// <summary>
    /// Returns this result or supplied value when in error.
    /// </summary>
    /// <param name="value">Fallback value.</param>
    /// <returns>Original result or fallback value result.</returns>
    public ValueResult<T, E> Or(T value)
    {
        if (this.ok)
            return this;

        return new(value);
    }

    /// <summary>
    /// Returns this result or fallback value from factory when in error.
    /// </summary>
    /// <param name="valueFactory">Fallback value factory.</param>
    /// <returns>Original result or fallback value result.</returns>
    public ValueResult<T, E> Or(Func<T> valueFactory)
    {
        if (this.ok)
            return this;

        return new(valueFactory());
    }

    /// <summary>
    /// Returns this result or error fallback.
    /// </summary>
    /// <param name="error">Fallback error.</param>
    /// <returns>Current result or fallback error result.</returns>
    public ValueResult<T, E> Or(Error<E> error)
    {
        if (this.ok)
            return this;

        return new(error);
    }

    /// <summary>
    /// Returns this result or fallback error from factory.
    /// </summary>
    /// <param name="errorFactory">Fallback error factory.</param>
    /// <returns>Current result or fallback error result.</returns>
    public ValueResult<T, E> Or(Func<Error<E>> errorFactory)
    {
        if (this.ok)
            return this;

        return new(errorFactory());
    }

    /// <summary>
    /// Tries to extract value.
    /// </summary>
    /// <param name="value">Output value.</param>
    /// <returns>True when value exists.</returns>
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
    /// Tries to extract interface error.
    /// </summary>
    /// <param name="error">Output interface error.</param>
    /// <returns>True when error exists.</returns>
    public bool TryGetError(out IError error)
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
    /// Tries to extract typed error.
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
    /// Tries to extract typed error payload.
    /// </summary>
    /// <param name="error">Output payload.</param>
    /// <returns>True when error exists.</returns>
    public bool TryGetError(out E error)
    {
        if (this.ok)
        {
            error = default!;
            return false;
        }

        error = this.error.Value;
        return true;
    }

    /// <summary>
    /// Gets value or default.
    /// </summary>
    /// <returns>Success value or default.</returns>
    public T ValueOrDefault()
    {
        if (this.ok)
            return this.value;

        return default!;
    }

    /// <summary>
    /// Gets value or fallback from factory.
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
    /// Gets value or fallback from typed value.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value when success; otherwise fallback.</returns>
    public bool TryGetError(out IError<E> error)
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
    /// Gets value or fallback from typed value.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value when success; otherwise fallback value.</returns>
    public T ValueOrDefault(T defaultValue)
    {
        if (this.ok)
            return this.value;

        return defaultValue;
    }
}
