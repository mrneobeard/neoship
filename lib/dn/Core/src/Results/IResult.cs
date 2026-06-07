using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

public interface IResult : IUnion
{
    bool HasValue { get; }

    bool HasError { get; }

    IError ErrorOrDefault();

    IError ErrorOrDefault(Func<IError> defaultValueFactory);
}


public interface IResult<T> : IResult
{
    bool TryGetValue(out T value);

    bool TryGetError(out IError error);

    T ValueOrDefault();

    T ValueOrDefault(Func<T> defaultValueFactory);
}


public interface IResult<T, E> : IResult<T>
{
    bool TryGetError(out IError<E> error);

    T ValueOrDefault(T defaultValue);

    new IError<E> ErrorOrDefault();

    IError<E> ErrorOrDefault(Func<IError<E>> defaultValueFactory);
}