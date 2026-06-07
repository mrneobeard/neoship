using System.Runtime.CompilerServices;

using NeoBeard.Results;

namespace NeoBeard.Options;

/// <summary>
/// Represents an option that can contain either a value of <typeparamref name="T"/> or none.
/// </summary>
/// <typeparam name="T">Option value type.</typeparam>
public sealed class Option<T> : IUnion, IOption
    where T: notnull
{
    private readonly T value;

    private readonly bool hasValue;

    private readonly None none;

    /// <summary>
    /// Initializes an option with a <see cref="Some{T}"/> value.
    /// </summary>
    /// <param name="some">Some wrapper.</param>
    public Option(Some<T> some)
    {
        this.value = some.Value;
        this.hasValue = true;
        this.none = Never.Value;
    }

    /// <summary>
    /// Initializes a successful value option.
    /// </summary>
    /// <param name="value">Value payload.</param>
    public Option(T value)
    {
        this.value = value;
        this.hasValue = true;
        this.none = Never.Value;
    }

    /// <summary>
    /// Initializes an empty option.
    /// </summary>
    public Option()
    {
        this.value = default!;
        this.hasValue = false;
        this.none = Never.Value;
    }

    object? IUnion.Value => this.hasValue ? this.value : this.none;

    /// <summary>
    /// Gets whether the option contains a value.
    /// </summary>
    /// <value><see langword="true"/> when value is present.</value>
    public bool HasValue => this.hasValue;

    /// <summary>
    /// Gets whether the option has no value.
    /// </summary>
    /// <value><see langword="true"/> when empty.</value>
    public bool HasNoValue => !this.hasValue;

    /// <summary>
    /// Implicitly creates an option from a value.
    /// </summary>
    /// <param name="value">Value to wrap.</param>
    /// <returns>Option value when not null; otherwise <see cref="None.Value"/>.</returns>
    public static implicit operator Option<T>(T value) => value is null ? NoneValue : new(value);

    /// <summary>
    /// Implicitly converts <see cref="None"/> marker.
    /// </summary>
    /// <param name="none">None marker.</param>
    /// <returns>An empty option.</returns>
    public static implicit operator Option<T>(None none) => NoneValue;

    /// <summary>
    /// Implicitly converts <see cref="Never"/> marker.
    /// </summary>
    /// <param name="never">Never marker.</param>
    /// <returns>An empty option.</returns>
    public static implicit operator Option<T>(Never never) => NoneValue;

    /// <summary>
    /// Implicitly converts <see cref="DBNull"/> into none.
    /// </summary>
    /// <param name="dbNull">DBNull marker.</param>
    /// <returns>An empty option.</returns>
    public static implicit operator Option<T>(DBNull dbNull) => NoneValue;

    /// <summary>
    /// Implicitly converts empty tuple into none.
    /// </summary>
    /// <param name="valueTuple">Tuple marker.</param>
    /// <returns>An empty option.</returns>
    public static implicit operator Option<T>(ValueTuple valueTuple) => NoneValue;

    /// <summary>
    /// Creates a present option.
    /// </summary>
    /// <param name="value">Value payload.</param>
    /// <returns>An option containing <paramref name="value"/>.</returns>
    public static Option<T> Some(T value) => new(value);

    /// <summary>
    /// Gets the singleton empty option.
    /// </summary>
    /// <value>An empty option.</value>
    public static Option<T> NoneValue { get; } = new();

    /// <summary>
    /// Gets either the wrapped value or none marker.
    /// </summary>
    /// <value>Wrapped value when available; otherwise <see cref="None"/>.</value>
    public object? Value => this.hasValue ? this.value : this.none;

    /// <summary>
    /// Creates option from a nullable value.
    /// </summary>
    /// <param name="value">Nullable value.</param>
    /// <returns>Option with value when not null; otherwise empty option.</returns>
    public static Option<T> From(T? value)
    {
        if (value is T value2)
            return new(value2);

        return NoneValue;
    }

    public static Option<T> None()
        => NoneValue;

    /// <summary>
    /// Calls <paramref name="inspector"/> when value exists and returns this option.
    /// </summary>
    /// <param name="inspector">Inspector invoked on value.</param>
    /// <returns>This option.</returns>
    public Option<T> Inspect(Action<T> inspector)
    {
        if (this.hasValue)
            inspector(this.value);

        return this;
    }

    /// <summary>
    /// Maps present value to another type.
    /// </summary>
    /// <typeparam name="U">Mapped value type.</typeparam>
    /// <param name="map">Mapper function.</param>
    /// <returns>Mapped value when present; otherwise default.</returns>
    public U Map<U>(Func<T, U> map)
    {
        if (this.hasValue)
            return map(this.value);

        return default!;
    }

    /// <summary>
    /// Maps value or fallback when none.
    /// </summary>
    /// <typeparam name="U">Mapped value type.</typeparam>
    /// <param name="map">Mapper function.</param>
    /// <param name="factory">Fallback factory.</param>
    /// <returns>Mapped value or fallback.</returns>
    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.hasValue)
            return map(this.value);

        return factory();
    }

    /// <summary>
    /// Maps value or default value when none.
    /// </summary>
    /// <typeparam name="U">Mapped value type.</typeparam>
    /// <param name="map">Mapper function.</param>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Mapped value or fallback.</returns>
    public U Map<U>(Func<T, U> map, U defaultValue)
    {
        if (this.hasValue)
            return map(this.value);

        return defaultValue;
    }

    /// <summary>
    /// Matches value predicate if present.
    /// </summary>
    /// <param name="map">Predicate to run.</param>
    /// <returns>True when predicate matches on value.</returns>
    public bool Match(Func<T, bool> map)
    {
        if (this.hasValue)
            return map(this.value);

        return false;
    }

    /// <summary>
    /// Returns this value when present, otherwise fallback value.
    /// </summary>
    /// <param name="value">Fallback value.</param>
    /// <returns>This option or fallback value.
    public Option<T> Or(T value)
    {
        if (this.hasValue)
            return this;

        return new(value);
    }

    /// <summary>
    /// Returns this value when present, otherwise value from factory.
    /// </summary>
    /// <param name="valueFactory">Fallback factory.</param>
    /// <returns>This option or new option from fallback.</returns>
    public Option<T> Or(Func<T> valueFactory)
    {
        if (this.hasValue)
            return this;

        return new(valueFactory());
    }

    /// <summary>
    /// Tries to retrieve option value.
    /// </summary>
    /// <param name="value">Output value.</param>
    /// <returns>True when value exists.</returns>
    public bool TryGetValue(out T value)
    {
        if (this.hasValue)
        {
            value = this.value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Gets value or default value.
    /// </summary>
    /// <returns>Value when present; otherwise default.</returns>
    public T ValueOrDefault()
        => this.hasValue ? this.value : default!;

    /// <summary>
    /// Gets value or fallback.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.
    /// </param>
    /// <returns>Value when present; otherwise fallback.</returns>
    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.hasValue)
            return this.value; 

        return defaultValueFactory();
    }

    /// <summary>
    /// Gets value or explicit default.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value when present; otherwise fallback.</returns>
    public T ValueOrDefault(T defaultValue)
    {
        if (this.hasValue)
            return this.value; 

        return defaultValue;
    }

    /// <summary>
    /// Gets value or throws <see cref="OptionException"/> when empty.
    /// </summary>
    /// <returns>Value when present.</returns>
    /// <exception cref="OptionException">Thrown when option is empty.</exception>
    public T ValueOrThrow()
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException($"{nameof(Option<T>)} is none.");
    }

    /// <summary>
    /// Gets value or throws factory-provided exception when empty.
    /// </summary>
    /// <param name="exceptionFactory">Exception factory.</param>
    /// <returns>Value when present.</returns>
    public T ValueOrThrow(Func<Exception> exceptionFactory)
    {
        if (this.hasValue)
            return this.value;

        throw exceptionFactory();
    }

    /// <summary>
    /// Gets value or throws <see cref="OptionException"/> built from message when empty.
    /// </summary>
    /// <param name="messageFactory">Message factory.</param>
    /// <returns>Value when present.</returns>
    public T ValueOrThrow(Func<string> messageFactory)
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException(messageFactory());
    }

    /// <summary>
    /// Gets value or throws <see cref="OptionException"/> with fixed message.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>Value when present.</returns>
    public T ValueOrThrow(string message)
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException(message);
    }

    /// <summary>
    /// Converts option to <see cref="Result{T}"/>.
    /// </summary>
    /// <returns>Successful result when value exists; otherwise failure.</returns>
    public Result<T> ToResult()
    {
        if (this.hasValue)
            return this.value;

        return new OptionException($"{nameof(Option<T>)} is none.");
    }
}
