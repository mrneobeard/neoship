namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the Map class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class Map : Map<string, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map();
    /// map["key"] = "value";
    /// Assert.Equal("value", map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public Map()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map(100);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var map = new Map(dict);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IDictionary<string, object?> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, object?&gt; { ["key"] = 42 };
    /// var map = new Map(dict, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IDictionary<string, object?> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", (object?)42), ("key2", "value") };
    /// var map = new Map(items);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<(string, object?)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key", (object?)42) };
    /// var map = new Map(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<(string, object?)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var map = new Map(items);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<KeyValuePair<string, object?>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create&lt;string, object?&gt;("key", 42) };
    /// var map = new Map(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(IEnumerable<KeyValuePair<string, object?>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Map"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map(StringComparer.Ordinal);
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
    /// Initializes a new instance of the <see cref="Map"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new Map(100, StringComparer.Ordinal);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public Map(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }
}