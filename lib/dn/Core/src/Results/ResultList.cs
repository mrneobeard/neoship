using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoBeard.Results;

/// <summary>
/// Represents a typed list wrapper over <see cref="Result{TValue}"/> values.
/// </summary>
/// <typeparam name="TValue">The value type carried by each result.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var list = new ResultsList&lt;int&gt;(new[] { Result.Ok(1), Result.Ok(2) });
/// Assert.False(list.IsError);
/// </code>
/// </example>
/// </remarks>
public class ResultsList<TValue>
    where TValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsList{TValue}"/> class with an empty list.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new ResultsList&lt;int&gt;();
    /// Assert.Empty(list.Results);
    /// </code>
    /// </example>
    /// </remarks>
    public ResultsList()
    {
        this.Results = new List<Result<TValue>>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsList{TValue}"/> class with a capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new ResultsList&lt;int&gt;(4);
    /// list.Results.Add(Result.Ok(1));
    /// Assert.Single(list.Results);
    /// </code>
    /// </example>
    /// </remarks>
    public ResultsList(int capacity)
    {
        this.Results = new List<Result<TValue>>(capacity);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsList{TValue}"/> class from existing results.
    /// </summary>
    /// <param name="results">The seed results.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var results = new[] { Result.Ok(1), Result.Ok(2) };
    /// var list = new ResultsList&lt;int&gt;(results);
    /// Assert.Equal(2, list.Results.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public ResultsList(IEnumerable<Result<TValue>> results)
    {
        this.Results = results.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultsList{TValue}"/> class from an existing list.
    /// </summary>
    /// <param name="results">The source list to wrap.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="results"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var results = new List&lt;Result&lt;int&gt;&gt; { Result.Ok(1), Result.Ok(2) };
    /// var list = new ResultsList&lt;int&gt;(results);
    /// Assert.Equal(2, list.Results.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public ResultsList(IList<Result<TValue>> results)
    {
        this.Results = results ?? throw new ArgumentNullException(nameof(results));
    }

    /// <summary>
    /// Gets the wrapped list of results.
    /// </summary>
    /// <value>An immutable reference to the list of <see cref="Result{TValue}"/> values.</value>
    public IList<Result<TValue>> Results { get; }

    /// <summary>
    /// Gets whether any wrapped result is in an error state.
    /// </summary>
    /// <value><see langword="true"/> when at least one entry has an error.</value>
    public bool IsError => this.Results.Any(r => r.HasError);

    /// <summary>
    /// Gets whether all wrapped results are successful.
    /// </summary>
    /// <value><see langword="true"/> when there are no errors.</value>
    public bool IsOk => !this.IsError;

    /// <summary>
    /// Extracts all successful values from the list.
    /// </summary>
    /// <param name="throwOnError">Whether to throw when an error result exists.</param>
    /// <returns>A list of successful values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var results = new[] { Result.Ok(1), Result.Fail&lt;int&gt;(new Exception("error")), Result.Ok(2) };
    /// var list = new ResultsList&lt;int&gt;(results);
    /// var values = list.ToValues(throwOnError: false);
    /// Assert.Equal(2, values.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public List<TValue> ToValues(bool throwOnError = true)
    {
        if (this.IsError && throwOnError)
            throw this.ToAggregateException();

        var values = new List<TValue>();
        foreach (var result in this.Results)
        {
            if (result.TryGetValue(out TValue value))
                values.Add(value);
        }

        return values;
    }

    /// <summary>
    /// Builds an aggregate exception from all error results.
    /// </summary>
    /// <returns>An <see cref="AggregateException"/> containing error exceptions.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var results = new[] { Result.Fail&lt;int&gt;(new InvalidOperationException("error1")), Result.Ok(2) };
    /// var list = new ResultsList&lt;int&gt;(results);
    /// var aggregate = list.ToAggregateException();
    /// Assert.Single(aggregate.InnerExceptions);
    /// </code>
    /// </example>
    /// </remarks>
    public AggregateException ToAggregateException()
    {
        var errors = new List<Exception>();
        foreach (var result in this.Results)
        {
            if (result.TryGetError(out Error error))
                errors.Add(error.ToException());
        }

        if (errors.Count == 0)
            return new AggregateException("No errors present in results list.");

        return new AggregateException(errors);
    }
}
