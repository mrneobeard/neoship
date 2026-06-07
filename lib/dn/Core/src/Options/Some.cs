namespace NeoBeard.Options;

public readonly struct Some<T>
    where T: notnull
{
    public Some(T value)
    {
        this.Value = value;
    }

    public T Value { get; }

    public static implicit operator Some<T>(T value) => new(value);

    public static implicit operator T(Some<T> some) => some.Value;

    public static implicit operator Option<T>(Some<T> some) => new(some.Value);

    public override string ToString() => this.Value.ToString() ?? $"{nameof(Some<T>)}<{typeof(T).Name}>";
}

public readonly struct None
{
    public static None Value { get; } = new();

    public static implicit operator None(Never never) => new();

    public static implicit operator None(DBNull dbNull) => new();

    public static implicit operator None(ValueTuple valueTuple) => new();

    public static implicit operator Never(None none) => Never.Value;

    public static implicit operator DBNull(None none) => DBNull.Value;

    public static implicit operator ValueTuple(None none) => ValueTuple.Create();

    public static bool IsNone(object? value) 
        => value is None || value is Never || value is DBNull || value is ValueTuple || value is null || value is IOption o && o.HasNoValue;
}