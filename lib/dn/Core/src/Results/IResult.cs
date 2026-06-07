using System.Runtime.CompilerServices;

namespace NeoBeard.Results;

/// <summary>
/// Represents the root contract for a discriminated result object.
/// </summary>
/// <remarks>
/// A result implementation exposes either a success value or an error state and does not require generic type parameters.
/// <example>
/// <code lang="csharp">
/// IResult result = Result.Fail(new Exception("boom"));
/// if (result.HasError) { var error = result.ErrorOrDefault(); }
/// </code>
/// </example>
/// </remarks>
public interface IResult : IUnion
{
    /// <summary>
    /// Gets a value indicating whether the result is in the success state.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the result holds a value; otherwise, <see langword="false"/>.
    /// </value>
    /// <example>
    /// <code lang="csharp">
    /// IResult result = Result.Ok();
    /// bool hasValue = result.HasValue;
    /// </code>
    /// </example>
    bool HasValue { get; }

    /// <summary>
    /// Gets a value indicating whether the result is in the error state.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the result holds an error; otherwise, <see langword="false"/>.
    /// </value>
    /// <example>
    /// <code lang="csharp">
    /// IResult result = Result.Fail(Error.From("fail"));
    /// bool hasError = result.HasError;
    /// </code>
    /// </example>
    bool HasError { get; }

    /// <summary>
    /// Returns the active error.
    /// </summary>
    /// <returns>An <see cref="IError"/> if the result is in an error state; otherwise <see langword="null"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// IError error = result.ErrorOrDefault();
    /// Console.WriteLine(error.Message);
    /// </code>
    /// </example>
    IError ErrorOrDefault();

    /// <summary>
    /// Returns the active error or a caller-provided default.
    /// </summary>
    /// <param name="defaultValueFactory">Factory invoked when the result is successful.</param>
    /// <returns>
    /// An <see cref="IError"/> if the result is in error; otherwise the factory output.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// IError error = result.ErrorOrDefault(() => Error.From("no-error"));
    /// </code>
    /// </example>
    IError ErrorOrDefault(Func<IError> defaultValueFactory);
}


/// <summary>
/// Represents a typed result contract with a success payload of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IResult&lt;int&gt; result = Result.Ok(42);
/// if (result.TryGetValue(out var value)) Console.WriteLine(value);
/// </code>
/// </example>
/// </remarks>
public interface IResult<T> : IResult
{
    /// <summary>
    /// Attempts to read the success value.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true"/>, receives the success value; otherwise <see langword="default"/>.
    /// </param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is available; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// if (result.TryGetValue(out var value)) { /* ... */ }
    /// </code>
    /// </example>
    bool TryGetValue(out T value);

    /// <summary>
    /// Attempts to read the error value.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="true"/>, receives the error; otherwise <see langword="default"/>.
    /// </param>
    /// <returns><see langword="true"/> when an error exists; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// if (result.TryGetError(out IError error)) { /* ... */ }
    /// </code>
    /// </example>
    bool TryGetError(out IError error);

    /// <summary>
    /// Returns the success value if present, otherwise the default value for <typeparamref name="T"/>.
    /// </summary>
    /// <returns>
    /// A <typeparamref name="T"/> value when available; otherwise <see langword="default"/>.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// var value = result.ValueOrDefault();
    /// </code>
    /// </example>
    T ValueOrDefault();

    /// <summary>
    /// Returns the success value if present, otherwise a caller-provided value.
    /// </summary>
    /// <param name="defaultValueFactory">Factory used when no value is present.</param>
    /// <returns>A <typeparamref name="T"/> value.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var value = result.ValueOrDefault(() => 0);
    /// </code>
    /// </example>
    T ValueOrDefault(Func<T> defaultValueFactory);
}


public interface IResult<T, E> : IResult<T>
{
    /// <summary>
    /// Attempts to read the typed error value.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="true"/>, receives the error; otherwise <see langword="default"/>.
    /// </param>
    /// <returns><see langword="true"/> when an error exists; otherwise <see langword="false"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// if (result.TryGetError(out IError&lt;MyError&gt; error)) { /* ... */ }
    /// </code>
    /// </example>
    bool TryGetError(out IError<E> error);

    /// <summary>
    /// Returns the success value if present; otherwise the supplied default value.
    /// </summary>
    /// <param name="defaultValue">Fallback value when no success value exists.</param>
    /// <returns>The <typeparamref name="T"/> result value or <paramref name="defaultValue"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// var value = result.ValueOrDefault("unknown");
    /// </code>
    /// </example>
    T ValueOrDefault(T defaultValue);

    /// <summary>
    /// Returns the typed error value or a default error instance.
    /// </summary>
    /// <returns>An <see cref="IError{E}"/> when the result is in error; otherwise <see langword="default"/>.</returns>
    /// <example>
    /// <code lang="csharp">
    /// IError&lt;int&gt; error = result.ErrorOrDefault();
    /// </code>
    /// </example>
    new IError<E> ErrorOrDefault();

    /// <summary>
    /// Returns the typed error value or a caller-provided default.
    /// </summary>
    /// <param name="defaultValueFactory">Factory used when no error is present.</param>
    /// <returns>
    /// An <see cref="IError{E}"/> when error state exists; otherwise the factory result.
    /// </returns>
    /// <example>
    /// <code lang="csharp">
    /// var error = result.ErrorOrDefault(() =&gt; Error&lt;string&gt;.From("ok"));
    /// </code>
    /// </example>
    IError<E> ErrorOrDefault(Func<IError<E>> defaultValueFactory);
}
