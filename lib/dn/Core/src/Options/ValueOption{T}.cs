namespace NeoBeard.Options;

/// <summary>
/// Represents a non-nullable option where missing value is tracked via <see cref="HasNoValue"/>.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct ValueOption<T> : IOption, IEquatable<ValueOption<T>>, IEquatable<T>
    where T : notnull
{
    private readonly T value;

    private readonly bool hasValue;

    /// <summary>
    /// Initializes with a present value.
    /// </summary>
    /// <param name="value">Present value.</param>
    public ValueOption(T value)
    {
        this.value = value;
        this.hasValue = true;
    }

    /// <summary>
    /// Initializes an empty value option.
    /// </summary>
    public ValueOption()
    {
        this.value = default!;
        this.hasValue = false;
    }

    /// <summary>
    /// Gets whether a value is present.
    /// </summary>
    /// <value><see langword="true"/> when value exists.</value>
    public bool HasValue => this.hasValue;

    /// <summary>
    /// Gets whether no value is present.
    /// </summary>
    /// <value><see langword="true"/> when value is missing.</value>
    public bool HasNoValue => !this.hasValue;

    /// <summary>
    /// Gets value or null when none.
    /// </summary>
    /// <value>Wrapped value or <see langword="null"/>.</value>
    public object? Value => this.hasValue ? this.value : null;

    /// <summary>
    /// Implicitly wraps a value as present option.
    /// </summary>
    /// <param name="value">Value to wrap.</param>
    /// <returns>Value option containing value.</returns>
    public static implicit operator ValueOption<T>(T value) => new(value);

    /// <summary>
    /// Converts none marker to empty option.
    /// </summary>
    /// <param name="none">None marker.</param>
    /// <returns>Empty value option.</returns>
    public static implicit operator ValueOption<T>(None none) => new();

    /// <summary>
    /// Converts <see cref="Never"/> marker to empty option.
    /// </summary>
    /// <param name="never">Never marker.</param>
    /// <returns>Empty value option.</returns>
    public static implicit operator ValueOption<T>(Never never) => new();

    /// <summary>
    /// Converts <see cref="DBNull"/> marker to empty option.
    /// </summary>
    /// <param name="dbNull">DBNull marker.</param>
    /// <returns>Empty value option.</returns>
    public static implicit operator ValueOption<T>(DBNull dbNull) => new();

    /// <summary>
    /// Converts tuple marker to empty option.
    /// </summary>
    /// <param name="valueTuple">Tuple marker.</param>
    /// <returns>Empty value option.</returns>
    public static implicit operator ValueOption<T>(ValueTuple valueTuple) => new();

    /// <summary>
    /// Creates present option from value.
    /// </summary>
    /// <param name="value">Value payload.</param>
    /// <returns>Present value option.</returns>
    public static ValueOption<T> Some(T value) => new(value);

    /// <summary>
    /// Gets empty option.
    /// </summary>
    /// <value>Empty value option.</value>
    public static ValueOption<T> NoneValue { get; } = new();

    public static ValueOption<T> None() => NoneValue;

    /// <summary>
    /// Compares equality with another value option.
    /// </summary>
    /// <param name="other">Other option.</param>
    /// <returns>True when equal.</returns>
    public bool Equals(ValueOption<T> other)
    {
        if (this.hasValue && other.hasValue)
            return this.value.Equals(other.value);

        return this.hasValue == other.hasValue;
    }

    /// <summary>
    /// Compares option value with raw value.
    /// </summary>
    /// <param name="other">Raw value.</param>
    /// <returns>True when value option is present and values equal.</returns>
    public bool Equals(T? other)
    {
        if (this.hasValue)
            return this.value.Equals(other);

        return false;
    }

    /// <summary>
    /// Overrides object equality.
    /// </summary>
    /// <param name="obj">Instance to compare.</param>
    /// <returns>True when equal.</returns>
    public override bool Equals(object? obj)
    {
        return obj is ValueOption<T> other && this.Equals(other);
    }

    /// <summary>
    /// Returns hash code for this value option.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.value, this.hasValue);
    }

    /// <summary>
    /// Inspects value when present.
    /// </summary>
    /// <param name="inspector">Inspector action.</param>
    /// <returns>This option.</returns>
    public ValueOption<T> Inspect(Action<T> inspector)
    {
        if (this.hasValue)
            inspector(this.value);

        return this;
    }

    /// <summary>
    /// Maps value to another value.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Mapping function.</param>
    /// <returns>Mapped value or default.</returns>
    public U Map<U>(Func<T, U> map)
    {
        if (this.hasValue)
            return map(this.value);

        return default!;
    }

    /// <summary>
    /// Maps value or fallback factory.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Value mapping function.</param>
    /// <param name="factory">Fallback function.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.hasValue)
            return map(this.value);

        return factory();
    }

    /// <summary>
    /// Maps value or supplied default.
    /// </summary>
    /// <typeparam name="U">Mapped return type.</typeparam>
    /// <param name="map">Value mapping function.</param>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Mapped value.</returns>
    public U Map<U>(Func<T, U> map, U defaultValue)
    {
        if (this.hasValue)
            return map(this.value);

        return defaultValue;
    }

    /// <summary>
    /// Matches value predicate when present.
    /// </summary>
    /// <param name="map">Predicate to evaluate.</param>
    /// <returns>Match result.</returns>
    public bool Match(Func<T, bool> map)
    {
        if (this.hasValue)
            return map(this.value);

        return false;
    }

    /// <summary>
    /// Returns this value option or fallback value option.
    /// </summary>
    /// <param name="value">Fallback value.</param>
    /// <returns>This or fallback value option.</returns>
    public ValueOption<T> Or(T value)
    {
        if (this.hasValue)
            return this;

        return new(value);
    }

    /// <summary>
    /// Tries to get value.
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
    /// Gets value or default.
    /// </summary>
    /// <returns>Value when present; otherwise default.</returns>
    public T ValueOrDefault()
    {
        if (this.hasValue)
            return this.value;

        return default!;
    }

    /// <summary>
    /// Gets value or fallback from factory.
    /// </summary>
    /// <param name="defaultValueFactory">Fallback factory.</param>
    /// <returns>Value or fallback.</returns>
    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.hasValue)
            return this.value;

        return defaultValueFactory();
    }

    /// <summary>
    /// Gets value or supplied default.
    /// </summary>
    /// <param name="defaultValue">Fallback value.</param>
    /// <returns>Value or fallback.</returns>
    public T ValueOrDefault(T defaultValue)
    {
        if (this.hasValue)
            return this.value;

        return defaultValue;
    }

    /// <summary>
    /// Gets value or throws when none.
    /// </summary>
    /// <returns>Value when present.</returns>
    /// <exception cref="OptionException">When option is empty.</exception>
    public T ValueOrThrow()
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException($"{nameof(ValueOption<T>)} is none.");
    }

    /// <summary>
    /// Gets value or throws factory exception when none.
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
    /// Gets value or throws exception with message from factory.
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
    /// Gets value or throws with fixed message.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <returns>Value when present.</returns>
    public T ValueOrThrow(string message)
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException(message);
    }
}