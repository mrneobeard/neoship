namespace NeoBeard.Options;

public static class Option
{
    /// <summary>
    /// Gets the empty option.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <returns>The empty option.</returns>
    public static Option<T> None<T>()
        where T: notnull
        => Option<T>.NoneValue;
    
    /// <summary>
    /// Creates a new option with a value.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="value">The value of the option.</param>
    /// <returns>The option with the value.</returns>
    public static Option<T> Some<T>(T value)
        where T: notnull
        => Option<T>.Some(value);

    /// <summary>
    /// Creates a new option from a nullable value.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    /// <param name="value">The value of the option.</param>
    /// <returns>The option with the value.</returns>
    public static Option<T> From<T>(T? value)
        where T : notnull
    {
        if (value is T value2)
            return new(value2);

        return None<T>();
    }
}

public static class ValueOption
{
    /// <summary>
    /// Gets the empty value option.
    /// </summary>
    /// <typeparam name="T">The type of the value option.</typeparam>
    /// <returns>The empty value option.</returns>
    public static ValueOption<T> None<T>()
        where T: notnull
        => ValueOption<T>.NoneValue;

    /// <summary>
    /// Creates a new value option with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value option.</typeparam>
    /// <param name="value">The value of the value option.</param>
    /// <returns>The value option with the value.</returns>
    public static ValueOption<T> Some<T>(T value)
        where T: notnull
        => ValueOption<T>.Some(value);

    /// <summary>
    /// Creates a new value option from a nullable value.
    /// </summary>
    /// <typeparam name="T">The type of the value option.</typeparam>
    /// <param name="value">The value of the value option.</param>
    /// <returns>The value option with the value.</returns>
    public static ValueOption<T> From<T>(T? value)
        where T: notnull
    {
        if (value is T value2)
            return new(value2);

        return None<T>();
    }
}