using System.Runtime.CompilerServices;

using NeoBeard.Results;

namespace NeoBeard.Options;

public sealed class Option<T> : IUnion, IOption
    where T: notnull
{
    private readonly T value;

    private readonly bool hasValue;

    private readonly None none;

    public Option(Some<T> some)
    {
        this.value = some.Value;
        this.hasValue = true;
        this.none = Never.Value;
    }

    public Option(T value)
    {
        this.value = value;
        this.hasValue = true;
        this.none = Never.Value;
    }

    public Option()
    {
        this.value = default!;
        this.hasValue = false;
        this.none = Never.Value;
    }

    object? IUnion.Value => this.hasValue ? this.value : this.none;

    public bool HasValue => this.hasValue;

    public bool HasNoValue => !this.hasValue;

    public static implicit operator Option<T>(T value) => value is null ? None : new(value);

    public static implicit operator Option<T>(None none) => None;

    public static implicit operator Option<T>(Never never) => None;

    public static implicit operator Option<T>(DBNull dbNull) => None;

    public static implicit operator Option<T>(ValueTuple valueTuple) => None;

    public static Option<T> Some(T value) => new(value);

    public static Option<T> None { get; } = new();

    public object? Value => this.hasValue ? this.value : this.none;

    public static Option<T> From(T? value)
    {
        if (value is null)
            return None;

        return new(value);
    }

    public Option<T> Inspect(Action<T> inspector)
    {
        if (this.hasValue)
            inspector(this.value);

        return this;
    }

    public U Map<U>(Func<T, U> map)
    {
        if (this.hasValue)
            return map(this.value);

        return default!;
    }

    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.hasValue)
            return map(this.value);

        return factory();
    }

    public U Map<U>(Func<T, U> map, U defaultValue)
    {
        if (this.hasValue)
            return map(this.value);

        return defaultValue;
    }

    public bool Match(Func<T, bool> map)
    {
        if (this.hasValue)
            return map(this.value);

        return false;
    }

    public Option<T> Or(T value)
    {
        if (this.hasValue)
            return this;

        return new(value);
    }

    public Option<T> Or(Func<T> valueFactory)
    {
        if (this.hasValue)
            return this;

        return new(valueFactory());
    }

    public bool TryGetValue(out T value)
    {
        if (this.hasValue)
        {
            value = this.value;
            return true;
        }

        value = this.value;
        return true;
    }

    public T ValueOrDefault()
        => this.hasValue ? this.value : default!;

    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.hasValue)
            return this.value; 

        return defaultValueFactory();
    }

    public T ValueOrDefault(T defaultValue)
    {
        if (this.hasValue)
            return this.value; 

        return defaultValue;
    }

    public T ValueOrThrow()
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException($"{nameof(Option<T>)} is is none.");
    }

    public T ValueOrThrow(Func<Exception> exceptionFactory)
    {
        if (this.hasValue)
            return this.value;

        throw exceptionFactory();
    }

    public T ValueOrThrow(Func<string> messageFactory)
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException(messageFactory());
    }

    public T ValueOrThrow(string message)
    {
        if (this.hasValue)
            return this.value;

        throw new OptionException(message);
    }

    public Result<T> ToResult()
    {
        if (this.hasValue)
            return this.value;

        return Result.Fail<T>(new OptionException($"{nameof(Option<T>)} is is none."));
    }
}