
namespace NeoBeard.Results;

public readonly struct Error : IError
{
    public Error(Exception exception, string? message = null, string? code = null)
    {
        this.Message = message ?? exception.Message;
        this.Code = code ?? exception.GetType().Name;
        this.Exception = exception;
    }

    public Error(IError cause, string? message = null, string? code = null)
    {
        this.Message = message ?? cause.Message;
        this.Code =  code ?? cause.Code;
        this.Exception = cause.Exception;
        this.Cause = cause;
    }

    public Error(string message, string? code = null)
    {
        this.Message = message;
        this.Code = code ?? "error";
        this.Exception = null;
    }

    public string Message { get; }

    public string? Code { get; }

    public IError? Cause { get; }

    public Exception? Exception { get; }

    public static Error Empty { get; } = new(string.Empty, string.Empty);

    public static implicit operator Error(string message) => new(message);

    public static implicit operator Error(Exception exception) => new(exception);

    public static implicit operator string(Error error) => error.Message;

    public static implicit operator Exception(Error error) => error.ToException();

    public static Error From(IError error)
    {
        if (error is Error e)
            return e;

        if (error.Cause is not null)
            return new Error(error.Cause, error.Message, error.Code);

        if (error.Exception is not null)
            return new Error(error.Exception, error.Message, error.Code);

        return new Error(error.Message, error.Code);
    }

    public static Error FromMessage(string message, string? code = null)
        => new(message, code);

    public static Error FromException(Exception exception, string? message = null, string? code = null)
        => new(exception, message, code);


    public Exception ToException() 
        => this.Exception ?? new ResultException(this.Message, this);

    public override string ToString() => $"{nameof(Error)}: {Message}";
}


public readonly struct Error<T> : IError<T>
{
    public Error(T value, string? message = null, string? code = null) 
    {
        this.Value = value;
        this.Message = message ?? value?.ToString() ?? string.Empty;
        this.Code = code;
        this.Exception = null;
    }

    public Error(T value, Exception exception, string? message = null, string? code = null)
    {
        this.Value = value;
        this.Message = message ?? exception.Message;
        this.Code = code ?? exception.GetType().Name;
        this.Exception = exception;
    }

    public Error(T value, IError cause, string? message = null, string? code = null)
    {
        this.Value = value;
        this.Message = message ?? cause.Message;
        this.Code =  code ?? cause.Code;
        this.Exception = cause.Exception;
        this.Cause = cause;
    }

    public T Value { get; }

    public string Message { get; }

    public string? Code { get; }

    public IError? Cause { get; }

    public Exception? Exception { get; }

    public static Error<T> Empty { get; } = new(default!, string.Empty, string.Empty);

    public static implicit operator Error<T>(T value)
    {
        if (value is Exception ex)
            return new Error<T>(value, ex.Message, ex.GetType().Name);

        return new Error<T>(value);
    }

    public static implicit operator T(Error<T> error) => error.Value;

    public static implicit operator string(Error<T> error) => error.Message;

    public static implicit operator Exception(Error<T> error) => error.ToException();

    public Exception ToException() 
        => this.Exception ?? new ResultException(this.Message, this);

    public override string ToString() => $"{nameof(Error<T>)}<{typeof(T).Name}>: {Message}";
}


public interface IError
{
    string Message { get; }

    string? Code { get; }

    IError? Cause { get; }

    Exception? Exception { get; }
}

public interface IError<T> : IError
{
    T Value { get; }
}
