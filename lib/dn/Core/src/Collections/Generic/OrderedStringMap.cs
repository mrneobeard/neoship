namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the OrderedStringMap class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class OrderedStringMap : OrderedMap<string, string?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedStringMap();
    /// map["key"] = "value";
    /// Assert.Equal("value", map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedStringMap(100);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, string?&gt; { ["key"] = "value" };
    /// var map = new OrderedStringMap(dict);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IDictionary<string, string?> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, string?&gt; { ["key"] = "value" };
    /// var map = new OrderedStringMap(dict, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IDictionary<string, string?> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", "value1"), ("key2", "value2") };
    /// var map = new OrderedStringMap(items);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IEnumerable<(string, string?)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key", "value") };
    /// var map = new OrderedStringMap(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IEnumerable<(string, string?)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", "value") };
    /// var map = new OrderedStringMap(items);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IEnumerable<KeyValuePair<string, string?>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", "value") };
    /// var map = new OrderedStringMap(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IEnumerable<KeyValuePair<string, string?>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedStringMap(StringComparer.Ordinal);
    /// map["key"] = "value";
    /// Assert.Throws&lt;KeyNotFoundException&gt;(() => map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(IEqualityComparer<string> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderedStringMap"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new OrderedStringMap(100, StringComparer.Ordinal);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedStringMap(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }
}