namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the Outputs class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class Outputs : Dictionary<string, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var outputs = new Outputs();
    /// outputs["result"] = 42;
    /// Assert.Equal(42, outputs["RESULT"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var outputs = new Outputs(100);
    /// Assert.Empty(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var outputs = new Outputs(dict);
    /// Assert.Single(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IDictionary<string, object?> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var outputs = new Outputs(dict, StringComparer.Ordinal);
    /// Assert.Single(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IDictionary<string, object?> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", (object?)42), ("key2", "value") };
    /// var outputs = new Outputs(items);
    /// Assert.Equal(2, outputs.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IEnumerable<(string, object?)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", (object?)42) };
    /// var outputs = new Outputs(items, StringComparer.Ordinal);
    /// Assert.Single(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IEnumerable<(string, object?)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var outputs = new Outputs(items);
    /// Assert.Single(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IEnumerable<KeyValuePair<string, object?>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var outputs = new Outputs(items, StringComparer.Ordinal);
    /// Assert.Single(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IEnumerable<KeyValuePair<string, object?>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var outputs = new Outputs(StringComparer.Ordinal);
    /// outputs["key"] = 42;
    /// Assert.Null(outputs["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(IEqualityComparer<string> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Outputs"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var outputs = new Outputs(100, StringComparer.Ordinal);
    /// Assert.Empty(outputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Outputs(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }

    /// <summary>
    /// Gets a shared empty outputs collection.
    /// </summary>
    /// <value>An empty <see cref="Outputs"/> instance.</value>
    public static Outputs Empty => new EmptyOutputs();

    /// <summary>
    /// Gets whether this collection has no entries.
    /// </summary>
    /// <value><see langword="true"/> when <see cref="Count"/> is zero.</value>
    public virtual bool IsEmpty => this.Count == 0;

    /// <summary>
    /// Gets whether the collection is read only.
    /// </summary>
    /// <value><see langword="false"/> for mutable <see cref="Outputs"/> instances.</value>
    public virtual bool IsReadOnly => false;
}

/// <summary>
/// Represents the EmptyOutputs class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class EmptyOutputs : Outputs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyOutputs"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var empty = new EmptyOutputs();
    /// Assert.True(empty.IsEmpty);
    /// </code>
    /// </example>
    /// </remarks>
    public EmptyOutputs()
        : base(0)
    {
    }

    /// <summary>
    /// Gets whether this collection is empty.
    /// </summary>
    /// <value><see langword="true"/> always.</value>
    public override bool IsEmpty => true;

    /// <summary>
    /// Gets whether this collection is read only.
    /// </summary>
    /// <value><see langword="true"/> always.</value>
    public override bool IsReadOnly => true;

    /// <summary>
    /// Throws <see cref="NotSupportedException"/> since empty outputs cannot be modified.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var empty = new EmptyOutputs();
    /// Assert.Throws&lt;NotSupportedException&gt;(() => empty.Add("key", 42));
    /// </code>
    /// </example>
    public new void Add(string key, object? value)
    {
        throw new NotSupportedException("Cannot add items to an empty Outputs instance.");
    }
}
