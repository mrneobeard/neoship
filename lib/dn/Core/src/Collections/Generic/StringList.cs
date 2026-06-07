namespace NeoBeard.Collections.Generic;

/// <summary>
/// Represents the StringList class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class StringList : List<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringList"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new StringList();
    /// list.Add("item");
    /// Assert.Single(list);
    /// </code>
    /// </example>
    /// </remarks>
    public StringList()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringList"/> class with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new StringList(100);
    /// Assert.Empty(list);
    /// </code>
    /// </example>
    /// </remarks>
    public StringList(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringList"/> class from an enumerable.
    /// </summary>
    /// <param name="collection">The collection to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new StringList(new[] { "a", "b", "c" });
    /// Assert.Equal(3, list.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public StringList(IEnumerable<string> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringList"/> class from a string collection.
    /// </summary>
    /// <param name="collection">The string collection to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var collection = new System.Collections.Specialized.StringCollection { "a", "b" };
    /// var list = new StringList(collection);
    /// Assert.Equal(2, list.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public StringList(System.Collections.Specialized.StringCollection collection)
        : this(collection.GetEnumerator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringList"/> class from a string enumerator.
    /// </summary>
    /// <param name="enumerator">The string enumerator to copy from.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var collection = new System.Collections.Specialized.StringCollection { "a", "b" };
    /// var list = new StringList(collection.GetEnumerator());
    /// Assert.Equal(2, list.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public StringList(System.Collections.Specialized.StringEnumerator enumerator)
        : base()
    {
        while (enumerator.MoveNext())
        {
            if (enumerator.Current == null)
                continue;
            this.Add(enumerator.Current);
        }
    }

    /// <summary>
    /// Returns the index of an item using case-insensitive comparison.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns>The index of the item, or -1 if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new StringList { "Item", "Other" };
    /// var index = list.IndexOfFold("ITEM");
    /// Assert.Equal(0, index);
    /// </code>
    /// </example>
    /// </remarks>
    public int IndexOfFold(string item)
    {
        return this.FindIndex(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines whether an item exists using case-insensitive comparison.
    /// </summary>
    /// <param name="item">The item to find.</param>
    /// <returns><c>true</c> if the item exists; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new StringList { "Item", "Other" };
    /// Assert.True(list.ContainsFold("ITEM"));
    /// Assert.False(list.ContainsFold("missing"));
    /// </code>
    /// </example>
    /// </remarks>
    public bool ContainsFold(string item)
    {
        return this.IndexOfFold(item) >= 0;
    }
}