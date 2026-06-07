namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the Map class.
/// </summary>
/// <typeparam name="TValue">The type of the values in the map.</typeparam>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class Map<TValue> : Map<string, TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map&lt;int&gt;();
    /// map["key"] = 42;
    /// Assert.Equal(42, map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Map()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map&lt;int&gt;(100);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, int&gt; { ["key"] = 42 };
    /// var map = new Map&lt;int&gt;(dict);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IDictionary<string, TValue> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, int&gt; { ["key"] = 42 };
    /// var map = new Map&lt;int&gt;(dict, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IDictionary<string, TValue> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", 42), ("key2", 100) };
    /// var map = new Map&lt;int&gt;(items);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<(string, TValue)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key", 42) };
    /// var map = new Map&lt;int&gt;(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<(string, TValue)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", 42) };
    /// var map = new Map&lt;int&gt;(items);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<KeyValuePair<string, TValue>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", 42) };
    /// var map = new Map&lt;int&gt;(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<KeyValuePair<string, TValue>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map&lt;int&gt;(StringComparer.Ordinal);
    /// map["key"] = 42;
    /// Assert.Throws&lt;KeyNotFoundException&gt;(() => map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEqualityComparer<string> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map{TValue}"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map&lt;int&gt;(100, StringComparer.Ordinal);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }
}