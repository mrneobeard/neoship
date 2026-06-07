namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the Inputs class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class Inputs : Dictionary<string, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inputs = new Inputs();
    /// inputs["param"] = "value";
    /// Assert.Equal("value", inputs["PARAM"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs()
     : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inputs = new Inputs(100);
    /// Assert.Empty(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var inputs = new Inputs(dict);
    /// Assert.Single(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IDictionary<string, object?> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var inputs = new Inputs(dict, StringComparer.Ordinal);
    /// Assert.Single(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IDictionary<string, object?> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", (object?)42), ("key2", "value") };
    /// var inputs = new Inputs(items);
    /// Assert.Equal(2, inputs.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IEnumerable<(string, object?)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key", (object?)42) };
    /// var inputs = new Inputs(items, StringComparer.Ordinal);
    /// Assert.Single(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IEnumerable<(string, object?)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var inputs = new Inputs(items);
    /// Assert.Single(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IEnumerable<KeyValuePair<string, object?>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var inputs = new Inputs(items, StringComparer.Ordinal);
    /// Assert.Single(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IEnumerable<KeyValuePair<string, object?>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inputs = new Inputs(StringComparer.Ordinal);
    /// inputs["key"] = 42;
    /// Assert.Throws&lt;KeyNotFoundException&gt;(() => inputs["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(IEqualityComparer<string> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Inputs"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var inputs = new Inputs(100, StringComparer.Ordinal);
    /// Assert.Empty(inputs);
    /// </code>
    /// </example>
    /// </remarks>
    public Inputs(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }

    /// <summary>
    /// Gets a shared empty input collection.
    /// </summary>
    /// <value>An empty <see cref="Inputs"/> instance.</value>
    /// <returns>An empty collection.</returns>
    public static Inputs Empty => new EmptyInputs();

    /// <summary>
    /// Gets whether the dictionary contains no entries.
    /// </summary>
    /// <value><see langword="true"/> when <see cref="Count"/> is zero.</value>
    /// <returns>Whether the collection is empty.</returns>
    public virtual bool IsEmpty => this.Count == 0;

    /// <summary>
    /// Gets whether the collection is read only.
    /// </summary>
    /// <value><see langword="false"/> for mutable <see cref="Inputs"/> instances.</value>
    /// <returns>Whether writes are allowed.</returns>
    public virtual bool IsReadOnly => false;
}

/// <summary>
/// Represents the EmptyInputs class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class EmptyInputs : Inputs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyInputs"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var empty = new EmptyInputs();
    /// Assert.True(empty.IsEmpty);
    /// </code>
    /// </example>
    /// </remarks>
    public EmptyInputs()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Gets whether the collection is empty.
    /// </summary>
    /// <value><see langword="true"/> always.</value>
    public override bool IsEmpty => true;

    /// <summary>
    /// Gets whether the collection is read only.
    /// </summary>
    /// <value><see langword="true"/> always.</value>
    public override bool IsReadOnly => true;

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> since empty inputs cannot be modified.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <example>
    /// <code lang="csharp">
    /// var empty = new EmptyInputs();
    /// Assert.Throws&lt;InvalidOperationException&gt;(() => empty.Add("key", 42));
    /// </code>
    /// </example>
    public new void Add(string key, object? value)
    {
        throw new InvalidOperationException("Cannot add to Empty Inputs.");
    }

    /// <summary>
    /// Returns the string representation of the empty inputs.
    /// </summary>
    /// <returns>The string "Empty Inputs".</returns>
    /// <example>
    /// <code lang="csharp">
    /// var empty = new EmptyInputs();
    /// Assert.Equal("Empty Inputs", empty.ToString());
    /// </code>
    /// </example>
    public override string ToString() => "Empty Inputs";
}
