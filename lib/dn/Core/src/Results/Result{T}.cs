using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

[Union]
public sealed class Result<T> : IUnion, IResult<T>
{
    private readonly T value;

    private readonly Error error;

    private readonly bool ok;

    public Result(T value)
    {
        this.value = value;
        this.ok = true;
        this.error = Error.Empty;
    }

    public Result(Error error)
    {
        this.value = default!;
        this.ok = false;
        this.error = error;
    }

    public bool HasValue => this.ok;

    public bool HasError => !this.ok;

    public object? Value => this.ok ? this.value : this.error;

    
    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);

    public static implicit operator Result<T>(Exception exception) => new(exception);

    public static implicit operator T(Result<T> result)
    {
        if (!result.ok)
            throw new ResultException("Result is in error state.");

        return result.value;
    }

    public static implicit operator Error(Result<T> result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    public Error ErrorOrDefault()
    {
        if (!this.ok)
            return this.error;

        return default!;
    }

    public Error ErrorOrDefault(Func<Error> defaultValueFactory)
    {
        if (this.ok)
            return defaultValueFactory();

        return this.error;
    }

    public Error ErrorOrDefault(Error defaultValue)
    {
        if (!this.ok)
            return this.error;

        return defaultValue;
    }

    public void Inspect(Action<T> inspector)
    {
        if (this.ok)
            inspector(this.value!);
    }

    public U Map<U>(Func<T, U> map)
    {
        if (this.ok)
            return map(this.value);

        return default!;
    }

    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory();
    }

    public U Map<U>(Func<T, U> map, Func<Error, U> factory)
    {
        if (this.ok)
            return map(this.value);

        return factory(this.error);
    }

    public U MapError<U>(Func<Error, U> map)
    {
        if (!this.ok)
            return map(this.error);

        return default!;
    }

    public U MapError<U>(Func<Error, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    public U MapError<U>(Func<Exception, U> map)
    {
        if (!this.ok)
            return map(this.error.Exception!);

        return default!;
    }

    public U MapEither<U>(Func<T, U> map, Func<Exception, U> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error.Exception!);
    }

    public Result<U> MapResult<U>(Func<U> mapValue)
    {
        if (this.ok)
            return new(mapValue());

        return new(this.error);
    }

    public Result<U, E> MapResult<U, E>(Func<U> mapValue, Func<Error, E> mapError)
    {
        if (this.ok)
            return new(mapValue());

        return new(mapError(this.error));
    }

    public bool Match(Func<T, bool> map)
    {
        if (this.ok)
            return map(this.value);

        return false;
    }

    public bool Match(Func<T, bool> map, Func<Error, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error);
    }

    public bool Match(Func<T, bool> map, Func<Exception, bool> errorMap)
    {
        if (this.ok)
            return map(this.value);

        return errorMap(this.error.Exception!);
    }

    public Result<T> Or(T value)
    {
        if (this.ok)
            return this;

        return new(value);
    }

    public Result<T> Or(Func<T> valueFactory)
    {
        if (this.ok)
            return this;

        return new(valueFactory());
    }

    public bool TryGetError(out Error error)
    {
        if (this.ok)
        {
            error = this.error;
            return false;
        }

        error = default!;
        return false;
    }

    public bool TryGetValue(out T value)
    {
        if (this.ok)
        {
            value = this.value;
            return true;
        }

        value = this.value;
        return true;
    }

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

    public T ValueOrDefault()
        => this.ok ? this.value : default!;

    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.ok)
            return this.value; 

        return defaultValueFactory();
    }

    public T ValueOrDefault(T defaultValue)
    {
        if (this.ok)
            return this.value; 

        return defaultValue;
    }

    public T ValueOrThrow()
    {
        if (this.ok)
            return this.value;

        throw this.error.ToException();
    }

    IError IResult.ErrorOrDefault()
       => this.ErrorOrDefault();

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

    IError IResult.ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }
}