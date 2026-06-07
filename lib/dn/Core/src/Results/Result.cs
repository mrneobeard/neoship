using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

[Union]
public sealed class Result : IResult
{
    private readonly Error error;

    private readonly bool ok;

    private readonly Never never;

    public Result()
    {
        this.error = Error.Empty;
        this.ok = true;
        this.never = Never.Value;
    }

    public Result(Error error)
    {
        this.error = error;
        this.ok = false;
        this.never = Never.Value;
    }

    public object? Value => this.ok ? this.never : this.error;

    public bool HasValue => this.ok;

    public bool HasError => !this.ok;

    public static implicit operator Result(Error error) => new(error);

    public static implicit operator Result(Exception exception) => new(exception);

    public static implicit operator Result(Never never) => new();

    public static implicit operator Result(string message) => new(message);

    public static implicit operator Result(Result<Never> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    public static implicit operator Result(Result<ValueTuple> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    public static implicit operator Result(Result<Never, Error> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    public static implicit operator Result(Result<ValueTuple, Error> result)
    {
        return result.HasValue ? new() : new(result.ErrorOrDefault());
    }

    public static implicit operator Error(Result result)
    {
        if (result.ok)
            throw new ResultException("Result is in value state.");

        return result.error;
    }

    public static Result OkDefault { get; } = new();

    public static Result Ok() => OkDefault;

    public static Result<T> Ok<T>(T value) => new(value);

    public static Result<T, E> Ok<T, E>(T value) => new(value);

    public static Result Fail(Error error) => new(error);

    public static Result<T> Fail<T>(Error error) => new(error);

    public static Result<T, E> Fail<T, E>(Error<T> error) => new(error);

    public static Result Try(Action action)
    {
        try
        {
            action();
            return Ok();
        }
        catch (Exception ex)
        {
            return Fail(ex);
        }
    }

    public static Result<T> Try<T>(Func<T> action)
    {
        try
        {
            return Ok(action());
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static Result<T, E> Try<T, E>(Func<T> action, Func<Exception, E> errorFactory)
    {
        try
        {
            var value = action();
            return value;
        }
        catch (Exception ex)
        {
            return new Result<T, E>(errorFactory(ex));
        }
    }

    public static Result<T, E> Try<T, E>(Func<T> action, Func<Exception, Error<E>> errorFactory)
    {
        try
        {
            var value = action();
            return value;
        }
        catch (Exception ex)
        {
            return errorFactory(ex);
        }
    }

    public static async Task<Result> TryAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        try
        {
            await action(cancellationToken);
            return new();
        } 
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static async Task<Result<T>> TryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await action(cancellationToken);
            return new(value);
        } 
        catch (Exception ex)
        {
            return ex;
        }
    }

    public static async Task<Result<T, E>> TryAsync<T, E>(Func<CancellationToken, Task<T>> action, Func<Exception, E> errorFactory, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await action(cancellationToken);
            return new(value);
        } 
        catch (Exception ex)
        {
            return new Result<T, E>(errorFactory(ex));
        }
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

    public U Map<U>(Func<U> map)
    {
        if (this.ok)
            return map();

        return default!;
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

    public U MapEither<U>(Func<U> map, Func<Error, U> errorMap)
    {
        if (this.ok)
            return map();

        return errorMap(this.error);
    }
    
    public Result<U, E> MapResult<U, E>(Func<U> mapValue, Func<Error, E> mapError)
    {
        if (this.ok)
            return new(mapValue());

        return new(mapError(this.error));
    }

    public Result<U> MapResult<U>(Func<U> mapValue)
    {
        if (this.ok)
            return new(mapValue());

        return new(this.error);
    }

    public bool TryGetValue(out Error? error)
    {
        if (this.ok)
        {
            error = this.error;
            return false;
        }

        error = default!;
        return false;
    }

    public bool TryGetValue(out Never? never)
    {
        if (this.ok)
        {
            never = this.never;
            return true;
        }

        never = this.never;
        return true;
    }

    public bool TryGetError(out Error? error)
         => this.TryGetValue(out error);

    IError IResult.ErrorOrDefault()
    {
        return this.ErrorOrDefault();
    }

    IError IResult.ErrorOrDefault(Func<IError> defaultValueFactory)
    {
        if (this.HasError)
            return this.error;

        return defaultValueFactory();
    }
}