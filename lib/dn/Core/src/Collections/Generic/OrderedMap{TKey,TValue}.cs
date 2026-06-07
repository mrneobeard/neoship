namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the OrderedMap class.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the ordered map.</typeparam>
/// <typeparam name="TValue">The type of the values in the ordered map.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class OrderedMap<TKey, TValue> : OrderedDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;int, string&gt;();
    /// map[1] = "one";
    /// Assert.Equal("one", map[1]);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;int, string&gt;(100);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;int, string&gt; { [1] = "one" };
    /// var map = new OrderedMap&lt;int, string&gt;(dict);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IDictionary<TKey, TValue> dictionary)
        : base(dictionary)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The key comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, int&gt; { ["key"] = 42 };
    /// var map = new OrderedMap&lt;string, int&gt;(dict, StringComparer.OrdinalIgnoreCase);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { (1, "one"), (2, "two") };
    /// var map = new OrderedMap&lt;int, string&gt;(items);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IEnumerable<(TKey, TValue)> collection)
    {
        foreach (var item in collection)
        {
            this.Add(item.Item1, item.Item2);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create(1, "one") };
    /// var map = new OrderedMap&lt;int, string&gt;(items);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The key comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", 42) };
    /// var map = new OrderedMap&lt;string, int&gt;(items, StringComparer.OrdinalIgnoreCase);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The key comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;string, int&gt;(StringComparer.OrdinalIgnoreCase);
    /// map["key"] = 42;
    /// Assert.Equal(42, map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(IEqualityComparer<TKey> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedMap{TKey, TValue}"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The key comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;int, string&gt;(100, EqualityComparer&lt;int&gt;.Default);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap(int capacity, IEqualityComparer<TKey> comparer)
        : base(capacity, comparer)
    {
    }

    /// <summary>
    /// Adds a key-value tuple to the map and returns the map for fluent chaining.
    /// </summary>
    /// <param name="item">The key-value tuple to add.</param>
    /// <returns>This map instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;int, string&gt;().Add((1, "one")).Add((2, "two"));
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap<TKey, TValue> Add((TKey, TValue) item)
    {
        this.Add(item.Item1, item.Item2);
        return this;
    }

    /// <summary>
    /// Adds multiple key-value tuples to the map and returns the map for fluent chaining.
    /// </summary>
    /// <param name="items">The key-value tuples to add.</param>
    /// <returns>This map instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedMap&lt;int, string&gt;().AddRange(new[] { (1, "one"), (2, "two") });
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedMap<TKey, TValue> AddRange(IEnumerable<(TKey, TValue)> items)
    {
        foreach (var item in items)
        {
            this.Add(item);
        }

        return this;
    }
}