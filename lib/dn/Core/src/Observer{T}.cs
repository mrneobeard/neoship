namespace NeoBeard;

/// <summary>
/// Represents the Observer class.
/// </summary>
/// <typeparam name="T">The type of the values observed.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class Observer<T>
{
    private readonly Action<T>? onNext;
    private readonly Action<Exception>? onError;
    private readonly Action? onCompleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer{T}"/> class with callbacks.
    /// </summary>
    /// <param name="onNext">The action to invoke when a value is received.</param>
    /// <param name="onError">The action to invoke when an error occurs.</param>
    /// <param name="onCompleted">The action to invoke when the observation completes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var observer = new Observer&lt;int&gt;(
    ///     value => Console.WriteLine($"Received: {value}"),
    ///     error => Console.WriteLine($"Error: {error.Message}"),
    ///     () => Console.WriteLine("Completed"));
    /// observer.OnNext(42);
    /// </code>
    /// </example>
    /// </remarks>
    public Observer(Action<T>? onNext, Action<Exception>? onError, Action? onCompleted)
    {
        this.onNext = onNext;
        this.onError = onError;
        this.onCompleted = onCompleted;
    }

    public static Observer<T> Create(
        Action<T>? onNext = null,
        Action<Exception>? onError = null,
        Action? onCompleted = null)
        => new(onNext, onError, onCompleted);

    /// <summary>
    /// Notifies the observer of a new value.
    /// </summary>
    /// <param name="value">The value to notify.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var received = 0;
    /// var observer = new Observer&lt;int&gt;(v => received = v, null, null);
    /// observer.OnNext(42);
    /// Assert.Equal(42, received);
    /// </code>
    /// </example>
    /// </remarks>
    public void OnNext(T value)
        => this.onNext?.Invoke(value);

    /// <summary>
    /// Notifies the observer of an error.
    /// </summary>
    /// <param name="error">The error to notify.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Exception? captured = null;
    /// var observer = new Observer&lt;int&gt;(null, ex => captured = ex, null);
    /// observer.OnError(new InvalidOperationException("error"));
    /// Assert.NotNull(captured);
    /// </code>
    /// </example>
    /// </remarks>
    public void OnError(Exception error)
        => this.onError?.Invoke(error);

    /// <summary>
    /// Notifies the observer that the observation has completed.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var completed = false;
    /// var observer = new Observer&lt;int&gt;(null, null, () => completed = true);
    /// observer.OnCompleted();
    /// Assert.True(completed);
    /// </code>
    /// </example>
    /// </remarks>
    public void OnCompleted()
        => this.onCompleted?.Invoke();
}