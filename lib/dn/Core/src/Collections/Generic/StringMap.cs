namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the StringMap class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class StringMap : Map<string, string?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new StringMap();
    /// map["key"] = "value";
    /// Assert.Equal("value", map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new StringMap(100);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(int capacity)
        : base(capacity, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, string?&gt; { ["key"] = "value" };
    /// var map = new StringMap(dict);
    /// Assert.Equal("value", map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IDictionary<string, string?> dictionary)
        : base(dictionary, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a name-value collection.
    /// </summary>
    /// <param name="collection">The name-value collection to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var collection = new System.Collections.Specialized.NameValueCollection { ["key"] = "value" };
    /// var map = new StringMap(collection);
    /// Assert.Equal("value", map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(System.Collections.Specialized.NameValueCollection collection)
    {
        foreach (string key in collection.Keys)
        {
            if (key == null)
                continue;

            this.Add(key, collection[key]);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a dictionary with a custom comparer.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dict = new Dictionary&lt;string, string?&gt; { ["key"] = "value" };
    /// var map = new StringMap(dict, StringComparer.Ordinal);
    /// Assert.Equal("value", map["key"]);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IDictionary<string, string?> dictionary, IEqualityComparer<string> comparer)
        : base(dictionary, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a collection of tuples.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", "value1"), ("key2", "value2") };
    /// var map = new StringMap(items);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IEnumerable<(string, string?)> collection)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a collection of tuples with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value tuples.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { ("key1", "value1"), ("key2", "value2") };
    /// var map = new StringMap(items, StringComparer.Ordinal);
    /// Assert.Equal(2, map.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IEnumerable<(string, string?)> collection, IEqualityComparer<string> comparer)
        : base(collection.ToDictionary(x => x.Item1, x => x.Item2), comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a collection of key-value pairs.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", "value") };
    /// var map = new StringMap(items);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IEnumerable<KeyValuePair<string, string?>> collection)
        : base(collection, StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class from a collection of key-value pairs with a custom comparer.
    /// </summary>
    /// <param name="collection">The collection of key-value pairs.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var items = new[] { KeyValuePair.Create("key", "value") };
    /// var map = new StringMap(items, StringComparer.Ordinal);
    /// Assert.Single(map);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IEnumerable<KeyValuePair<string, string?>> collection, IEqualityComparer<string> comparer)
        : base(collection, comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new StringMap(StringComparer.Ordinal);
    /// map["key"] = "value";
    /// Assert.Null(map["KEY"]);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(IEqualityComparer<string> comparer)
        : base(comparer)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringMap"/> class with capacity and custom comparer.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <param name="comparer">The string comparer to use.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var map = new StringMap(100, StringComparer.Ordinal);
    /// Assert.Empty(map);
    /// </code>
    /// </example>
    /// </remarks>
    public StringMap(int capacity, IEqualityComparer<string> comparer)
        : base(capacity, comparer)
    {
    }
}