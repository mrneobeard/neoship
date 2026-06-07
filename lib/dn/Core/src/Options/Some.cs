namespace NeoBeard.Options;

/// <summary>
/// Wraps a strongly-typed present option value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// Some&lt;int&gt; some = 5;
/// int value = some;
/// </code>
/// </example>
/// </remarks>
public readonly struct Some<T>
    where T: notnull
{
    /// <summary>
    /// Initializes a new <see cref="Some{T}"/> instance with a present value.
    /// </summary>
    /// <param name="value">The non-null value to wrap.</param>
    /// <example>
    /// <code lang="csharp">
    /// var some = new Some&lt;string&gt;("value");
    /// </code>
    /// </example>
    public Some(T value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    /// <value>The present value.</value>
    /// <example>
    /// <code lang="csharp">
    /// var some = new Some&lt;int&gt;(10);
    /// Console.WriteLine(some.Value);
    /// </code>
    /// </example>
    public T Value { get; }

    /// <summary>
    /// Converts a value to <see cref="Some{T}"/>.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A <see cref="Some{T}"/> value containing <paramref name="value"/>.</returns>
    public static implicit operator Some<T>(T value) => new(value);

    /// <summary>
    /// Converts a <see cref="Some{T}"/> instance to the underlying value.
    /// </summary>
    /// <param name="some">The optional value.</param>
    /// <returns>The underlying value.</returns>
    public static implicit operator T(Some<T> some) => some.Value;

    /// <summary>
    /// Converts <see cref="Some{T}"/> to <see cref="Option{T}"/>.
    /// </summary>
    /// <param name="some">The source optional wrapper.</param>
    /// <returns>An <see cref="Option{T}"/> containing the same value.</returns>
    public static implicit operator Option<T>(Some<T> some) => new(some.Value);

    /// <summary>
    /// Returns the string form of the wrapped value.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> representing the wrapped value.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// Some&lt;int&gt; some = 5;
    /// Console.WriteLine(some.ToString());
    /// </code>
    /// </example>
    public override string ToString() => this.Value.ToString() ?? $"{nameof(Some<T>)}<{typeof(T).Name}>";
}

public readonly struct None<T>
    where T: notnull
{
    public static None<T> Value { get; } = new();

    public static implicit operator None<T>(None none) => new();
}

/// <summary>
/// Represents an option marker with no value.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// None none = None.Value;
/// </code>
/// </example>
/// </remarks>
public readonly struct None
{
    /// <summary>
    /// Gets the shared none marker.
    /// </summary>
    /// <value>The shared <see cref="None"/> marker value.</value>
    public static None Value { get; } = new();

    /// <summary>
    /// Converts <see cref="Never"/> to <see cref="None"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns><see cref="None.Value"/>.</returns>
    public static implicit operator None(Never never) => new();

    /// <summary>
    /// Converts <see cref="DBNull"/> to <see cref="None"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns><see cref="None.Value"/>.</returns>
    public static implicit operator None(DBNull dbNull) => new();

    /// <summary>
    /// Converts <see cref="ValueTuple"/> to <see cref="None"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns><see cref="None.Value"/>.</returns>
    public static implicit operator None(ValueTuple valueTuple) => new();

    /// <summary>
    /// Converts <see cref="None"/> to <see cref="Never"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns>The <see cref="Never.Value"/> singleton.</returns>
    public static implicit operator Never(None none) => Never.Value;

    /// <summary>
    /// Converts <see cref="None"/> to <see cref="DBNull"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns><see cref="DBNull.Value"/>.</returns>
    public static implicit operator DBNull(None none) => DBNull.Value;

    /// <summary>
    /// Converts <see cref="None"/> to an empty <see cref="ValueTuple"/>.
    /// </summary>
    /// <param name="none">The source marker.</param>
    /// <returns><see cref="ValueTuple"/>.</returns>
    public static implicit operator ValueTuple(None none) => ValueTuple.Create();

    /// <summary>
    /// Determines whether the supplied value represents a none value.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="value"/> is a none marker; otherwise <see langword="false"/>.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// if (None.IsNone(null)) { /* true */ }
    /// </code>
    /// </example>
    public static bool IsNone(object? value) 
        => value is None || value is Never || value is DBNull || value is ValueTuple || value is null || value is IOption o && o.HasNoValue;
}
