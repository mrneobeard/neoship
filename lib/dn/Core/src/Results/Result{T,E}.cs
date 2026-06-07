using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

[Union]
public class Result<T,  E> : IUnion, IResult<T, E>
{
    private readonly Error<E> error;

    private readonly T? value;

    private readonly bool ok;

    public Result(T value)
    {
        this.error = Error<E>.Empty;
        this.value = value;
        this.ok = true;
    }

    public Result(Error<E> error)
    {
        this.error = error;
        this.value = default;   
        this.ok = false;
    }

    public bool HasValue => this.ok;

    public bool HasError => !this.ok;

    public object? Value
    {
        get
        {
            return this.value is null ? null : this.value;
        }
    }

    public static implicit operator Result<T, E>(T value) => new(value);

    public static implicit operator Result<T, E>(Error<E> error) => new(error);

    public static implicit operator Result<T>(Result<T, E> result)
    {
        if (result.ok)
            return new(result.value!);

        var e = result.error;

        if (e.Cause is not null)
            return  new Error(e.Cause, e.Message, e.Code);

        if (e.Exception is not null)
            return new Error(e.Exception, e.Message, e.Code);

        return new Error(e.Message, e.Code);
    }

    public static implicit operator Result(Result<T, E> result)
    {
        if (result.ok)
            return Result.OkDefault;

        var e = result.error;

        if (e.Cause is not null)
            return  new Error(e.Cause, e.Message, e.Code);

        if (e.Exception is not null)
            return new Error(e.Exception, e.Message, e.Code);

        return new Error(e.Message, e.Code);
    }

    public static implicit operator T(Result<T, E> result)
    {
        if (!result.ok)
            throw new ResultException("Result is in error state.");

        return result.value!;
    }

    public static implicit operator Error<E>(Result<T, E> result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    public Error<E> ErrorOrDefault()
    {
        if (!this.ok)
            return this.error;

        return default!;
    }

    public Error<E> ErrorOrDefault(Func<Error<E>> defaultValueFactory)
    {
        if (!this.ok)
            return defaultValueFactory();

        return this.error;
    }

    public Error<E> ErrorOrDefault(Func<E> defaultValueFactory)
    {
        if (!this.ok)
            return defaultValueFactory();

        return this.error;
    }

    public void Inspect(Action<T> inspector)
    {
        if (this.ok)
            inspector(this.value!);
    }

    public U Map<U>(Func<T, U> map)
    {
        if (this.ok)
            return map(this.value!);

        return default!;
    }

    public U Map<U>(Func<T, U> map, Func<U> factory)
    {
        if (this.ok)
            return map(this.value!);

        return factory();
    }

    public U Map<U>(Func<T, U> map, Func<Error<E>, U> factory)
    {
        if (this.ok)
            return map(this.value!);

        return factory(this.error);
    }

    public U MapError<U>(Func<Error<E>, U> map)
    {
        if (!this.ok)
            return map(this.error);

        return default!;
    }

    public U MapError<U>(Func<Error<E>, U> map, Func<U> factory)
    {
        if (!this.ok)
            return map(this.error);

        return factory();
    }

    public U MapError<U>(Func<E, U> map)
    {
        if (!this.ok)
            return map(this.error.Value);

        return default!;
    }

    public U MapEither<U>(Func<T, U> map, Func<E, U> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error.Value);
    }

    public bool Match(Func<T, bool> map)
    {
        if (this.ok)
            return map(this.value!);

        return false;
    }

    public bool Match(Func<T, bool> map, Func<Error<E>, bool> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error);
    }

    public bool Match(Func<T, bool> map, Func<E, bool> errorMap)
    {
        if (this.ok)
            return map(this.value!);

        return errorMap(this.error.Value);
    }

    public Result<T, E> Or(T value)
    {
        if (this.ok)
            return this;

        return new(value);
    }

    public Result<T, E> Or(Func<T> valueFactory)
    {
        if (this.ok)
            return this;

        return new(valueFactory());
    }
    
    public bool TryGetError(out Error<E> error)
        => this.TryGetError(out error);

    public bool TryGetValue(out T value)
    {
        if (this.ok)
        {
            value = this.value!;
            return true;
        }

        value = this.value!;
        return true;
    }

    public bool TryGetValue(out Error<E> error)
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
       => this.ok ? this.value! : default!;

    public T ValueOrDefault(Func<T> defaultValueFactory)
    {
        if (this.ok)
            return this.value!; 

        return defaultValueFactory();
    }

    public T ValueOrDefault(T defaultValue)
    {
        if (this.ok)
            return this.value!; 

        return defaultValue;
    }

    public T ValueOrThrow()
    {
        if (this.ok)
            return this.value!;

        throw this.error.ToException();
    }

    IError<E> IResult<T, E>.ErrorOrDefault()
        => this.ErrorOrDefault();

    IError<E> IResult<T, E>.ErrorOrDefault(Func<IError<E>> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }

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

    IError IResult.ErrorOrDefault()
        => this.ErrorOrDefault();

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

    IError IResult.ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }
}

    