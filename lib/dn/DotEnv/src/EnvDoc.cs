using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

// SA1202: Elements should be ordered by access modifier.
// SA1204: Static elements should appear before instance elements.
// Internal helper methods are placed near their usage for clarity.
#pragma warning disable SA1202, SA1204

namespace NeoBeard.DotEnv;

/// <summary>
/// Represents a parsed dotenv document, preserving order, comments, and formatting.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var doc = DotEnv.ParseFile(".env");
/// var value = doc.Get("DATABASE_URL");
/// doc.Set("API_KEY", "secret123");
/// doc.Expand(v => Env.Expand(v));
/// var content = doc.ToString();
/// </code>
/// </example>
/// </remarks>
public sealed class EnvDoc : IEnumerable<EnvElement>
{
    private readonly List<EnvElement> elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvDoc"/> class.
    /// </summary>
    public EnvDoc()
    {
        this.elements = new List<EnvElement>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvDoc"/> class with the specified elements.
    /// </summary>
    /// <param name="elements">The elements to initialize the document with.</param>
    public EnvDoc(IEnumerable<EnvElement> elements)
    {
        this.elements = new List<EnvElement>(elements);
    }

    /// <summary>
    /// Gets the number of elements in the document.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value\n# comment");
    /// Console.WriteLine(doc.Count); // 3 (variable, newline, comment)
    /// </code>
    /// </example>
    /// </remarks>
    public int Count => this.elements.Count;

    /// <summary>
    /// Gets or sets the value of a variable by key.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <returns>The variable value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc["KEY"] = "value";
    /// var value = doc["KEY"];
    /// </code>
    /// </example>
    /// </remarks>
    public string? this[string key]
    {
        get => this.Get(key);
        set
        {
            if (value is null)
            {
                this.Remove(key);
            }
            else
            {
                this.Set(key, value);
            }
        }
    }

    /// <summary>
    /// Gets all variable keys in the document.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY1=value1\nKEY2=value2");
    /// foreach (var key in doc.Keys)
    /// {
    ///     Console.WriteLine(key);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> Keys
    {
        get
        {
            var keys = new List<string>();
            foreach (var element in this.elements)
            {
                if (element is EnvVariable v)
                {
                    keys.Add(v.Key);
                }
            }

            return keys;
        }
    }

    /// <summary>
    /// Gets all comment texts in the document.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("# comment1\nKEY=value\n# comment2");
    /// foreach (var comment in doc.Comments)
    /// {
    ///     Console.WriteLine(comment);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<string> Comments
    {
        get
        {
            var comments = new List<string>();
            foreach (var element in this.elements)
            {
                if (element is EnvComment c)
                {
                    comments.Add(c.Text);
                }
            }

            return comments;
        }
    }

    /// <summary>
    /// Gets the value of a variable by key.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <returns>The variable value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value");
    /// var value = doc.Get("KEY");
    /// </code>
    /// </example>
    /// </remarks>
    public string? Get(string key)
    {
        foreach (var element in this.elements)
        {
            if (element is EnvVariable v && v.Key == key)
            {
                return v.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get the value of a variable by key.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value if found.</param>
    /// <returns>True if the variable was found; otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value");
    /// if (doc.TryGetValue("KEY", out var value))
    /// {
    ///     Console.WriteLine(value);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public bool TryGetValue(string key, out string? value)
    {
        value = this.Get(key);
        return value is not null;
    }

    /// <summary>
    /// Sets the value of a variable. If the variable exists, it is updated; otherwise, it is added.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <param name="value">The variable value.</param>
    /// <param name="quote">The quote style for the value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc.Set("KEY", "value", QuoteStyle.Double);
    /// </code>
    /// </example>
    /// </remarks>
    public void Set(string key, string value, QuoteStyle quote = QuoteStyle.Auto)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        value ??= string.Empty;

        var actualQuote = quote == QuoteStyle.Auto ? DetermineQuoteStyle(value) : quote;

        for (var i = 0; i < this.elements.Count; i++)
        {
            if (this.elements[i] is EnvVariable v && v.Key == key)
            {
                v.Value = value;
                v.Quote = actualQuote;
                return;
            }
        }

        this.elements.Add(new EnvVariable(key, value, actualQuote));
    }

    /// <summary>
    /// Removes a variable by key.
    /// </summary>
    /// <param name="key">The variable key.</param>
    /// <returns>True if the variable was removed; otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value");
    /// var removed = doc.Remove("KEY");
    /// </code>
    /// </example>
    /// </remarks>
    public bool Remove(string key)
    {
        for (var i = 0; i < this.elements.Count; i++)
        {
            if (this.elements[i] is EnvVariable v && v.Key == key)
            {
                this.elements.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears all elements from the document.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value");
    /// doc.Clear();
    /// Assert.Equal(0, doc.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public void Clear()
    {
        this.elements.Clear();
    }

    /// <summary>
    /// Adds a newline element to the document.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc.Set("KEY1", "value1");
    /// doc.AddNewline();
    /// doc.Set("KEY2", "value2");
    /// </code>
    /// </example>
    /// </remarks>
    public void AddNewline()
    {
        this.elements.Add(new EnvNewline());
    }

    /// <summary>
    /// Adds a comment element to the document.
    /// </summary>
    /// <param name="text">The comment text (without the # prefix).</param>
    /// <param name="isInline">Whether this is an inline comment after a variable.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc.AddComment("This is a comment");
    /// </code>
    /// </example>
    /// </remarks>
    public void AddComment(string text, bool isInline = false)
    {
        this.elements.Add(new EnvComment(text, isInline));
    }

    /// <summary>
    /// Merges another document into this one, overwriting existing keys.
    /// </summary>
    /// <param name="other">The document to merge.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc1 = DotEnvFile.Parse("KEY1=value1");
    /// var doc2 = DotEnvFile.Parse("KEY2=value2\nKEY1=override");
    /// doc1.Merge(doc2);
    /// Assert.Equal("override", doc1.Get("KEY1"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Merge(EnvDoc other)
    {
        if (other is null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        foreach (var element in other.elements)
        {
            if (element is EnvVariable v)
            {
                this.Set(v.Key, v.Value, v.Quote);
            }
        }
    }

    /// <summary>
    /// Merges a dictionary into this document, overwriting existing keys.
    /// </summary>
    /// <param name="dictionary">The dictionary to merge.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc.Set("KEY1", "value1");
    /// var dict = new Dictionary&lt;string, string&gt; { ["KEY2"] = "value2", ["KEY1"] = "override" };
    /// doc.Merge(dict);
    /// Assert.Equal("override", doc.Get("KEY1"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Merge(IDictionary<string, string> dictionary)
    {
        if (dictionary is null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        foreach (var kvp in dictionary)
        {
            this.Set(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Creates a new document by merging multiple documents.
    /// </summary>
    /// <param name="docs">The documents to merge.</param>
    /// <returns>A new merged document.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc1 = DotEnvFile.Parse("KEY1=value1");
    /// var doc2 = DotEnvFile.Parse("KEY2=value2");
    /// var merged = EnvDoc.Merge(doc1, doc2);
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc Merge(params EnvDoc[] docs)
    {
        var result = new EnvDoc();
        foreach (var doc in docs)
        {
            result.Merge(doc);
        }

        return result;
    }

    /// <summary>
    /// Expands all variable values in this document using the provided expander function.
    /// This mutates the document in-place and returns it for fluent chaining.
    /// </summary>
    /// <param name="expander">
    /// A function that takes a value string and returns the expanded version.
    /// Use <c>Env.Expand</c> from NeoBeard.Core for variable expansion and command substitution.
    /// </param>
    /// <returns>This document with all values expanded.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.ParseFiles(".env", ".env.local?");
    /// doc.Expand(value => Env.Expand(value));
    /// </code>
    /// </example>
    /// </remarks>
    public EnvDoc Expand(Func<string, string> expander)
    {
        if (expander is null)
        {
            throw new ArgumentNullException(nameof(expander));
        }

        foreach (var element in this.elements)
        {
            if (element is EnvVariable v && v.Value is not null)
            {
                v.Value = expander(v.Value);
            }
        }

        return this;
    }

    /// <summary>
    /// Creates a new document with expanded values (non-mutating).
    /// </summary>
    /// <param name="expander">The expander function.</param>
    /// <returns>A new document with expanded values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var original = DotEnvFile.Parse("KEY=${HOME}");
    /// var expanded = original.ExpandClone(value => Env.Expand(value));
    /// // original still has "${HOME}", expanded has the actual path
    /// </code>
    /// </example>
    /// </remarks>
    public EnvDoc ExpandClone(Func<string, string> expander)
    {
        var clone = new EnvDoc();
        foreach (var element in this.elements)
        {
            switch (element)
            {
                case EnvVariable v:
                    var expandedValue = v.Value is not null ? expander(v.Value) : string.Empty;
                    clone.elements.Add(new EnvVariable(v.Key, expandedValue, v.Quote));
                    break;
                case EnvComment c:
                    clone.elements.Add(new EnvComment(c.Text, c.IsInline));
                    break;
                case EnvNewline:
                    clone.elements.Add(new EnvNewline());
                    break;
            }
        }

        return clone;
    }

    /// <summary>
    /// Converts the document to a dictionary of variable keys and values.
    /// </summary>
    /// <returns>A dictionary containing all variables.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.Parse("KEY1=value1\nKEY2=value2");
    /// var dict = doc.ToDictionary();
    /// Assert.Equal("value1", dict["KEY1"]);
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<string, string> ToDictionary()
    {
        var dict = new Dictionary<string, string>();
        foreach (var element in this.elements)
        {
            if (element is EnvVariable v)
            {
                dict[v.Key] = v.Value;
            }
        }

        return dict;
    }

    /// <summary>
    /// Converts the document to a dictionary with the specified key and value types.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
    /// <param name="keySelector">A function to transform the key string to <typeparamref name="TKey"/>.</param>
    /// <param name="valueSelector">A function to transform the value string to <typeparamref name="TValue"/>.</param>
    /// <returns>A dictionary containing all variables with transformed keys and values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.Parse("PORT=8080\nTIMEOUT=30");
    /// var dict = doc.ToDictionary&lt;string, int&gt;(k => k, v => int.Parse(v));
    /// Assert.Equal(8080, dict["PORT"]);
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        Func<string, TKey> keySelector,
        Func<string, TValue> valueSelector)
        where TKey : notnull
    {
        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        if (valueSelector is null)
        {
            throw new ArgumentNullException(nameof(valueSelector));
        }

        var dict = new Dictionary<TKey, TValue>();
        foreach (var element in this.elements)
        {
            if (element is EnvVariable v)
            {
                dict[keySelector(v.Key)] = valueSelector(v.Value);
            }
        }

        return dict;
    }

    /// <summary>
    /// Converts the document to an ordered dictionary preserving the order of variables.
    /// </summary>
    /// <returns>An ordered dictionary containing all variables in their original order.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.Parse("KEY1=value1\nKEY2=value2\nKEY3=value3");
    /// var dict = doc.ToOrderedDictionary();
    /// var keys = dict.Keys.Cast&lt;string&gt;().ToList();
    /// Assert.Equal(new[] { "KEY1", "KEY2", "KEY3" }, keys);
    /// </code>
    /// </example>
    /// </remarks>
    public OrderedDictionary ToOrderedDictionary()
    {
        var dict = new OrderedDictionary();
        foreach (var element in this.elements)
        {
            if (element is EnvVariable v)
            {
                dict[v.Key] = v.Value;
            }
        }

        return dict;
    }

    /// <summary>
    /// Serializes the document to its string representation.
    /// </summary>
    /// <returns>The .env formatted string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = new EnvDoc();
    /// doc.Set("KEY", "value", QuoteStyle.Double);
    /// var content = doc.ToString();
    /// // content = "KEY=\"value\"\n"
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
    {
        var sb = new StringBuilder();

        for (var i = 0; i < this.elements.Count; i++)
        {
            var element = this.elements[i];
            var next = i + 1 < this.elements.Count ? this.elements[i + 1] : null;

            switch (element)
            {
                case EnvNewline:
                    sb.AppendLine();
                    break;
                case EnvComment c:
                    if (!c.IsInline)
                    {
                        sb.Append("# ");
                    }

                    sb.Append(c.Text);
                    if (!c.IsInline)
                    {
                        sb.AppendLine();
                    }

                    break;
                case EnvVariable v:
                    sb.Append(v.Key);
                    sb.Append('=');

                    if (v.Quote != QuoteStyle.None)
                    {
                        var quoteChar = v.Quote switch
                        {
                            QuoteStyle.Single => '\'',
                            QuoteStyle.Double => '"',
                            QuoteStyle.Backtick => '`',
                            _ => '"',
                        };
                        sb.Append(quoteChar);
                        sb.Append(v.Value);
                        sb.Append(quoteChar);
                    }
                    else
                    {
                        sb.Append(v.Value);
                    }

                    // Handle inline comment
                    if (next is EnvComment inlineComment && inlineComment.IsInline)
                    {
                        sb.Append(" # ");
                        sb.Append(inlineComment.Text);
                        i++; // Skip the inline comment in the loop
                    }

                    sb.AppendLine();
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets an enumerator for the elements in the document.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value\n# comment");
    /// foreach (var element in doc)
    /// {
    ///     Console.WriteLine(element.Kind);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public IEnumerator<EnvElement> GetEnumerator()
        => this.elements.GetEnumerator();

    /// <summary>
    /// Gets an enumerator for the elements in the document.
    /// </summary>
    /// <returns>An enumerator.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnv.Parse("KEY=value");
    /// var enumerable = (IEnumerable)doc;
    /// foreach (var element in enumerable)
    /// {
    ///     Console.WriteLine(element);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    IEnumerator IEnumerable.GetEnumerator()
        => this.elements.GetEnumerator();

    private static QuoteStyle DetermineQuoteStyle(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return QuoteStyle.None;
        }

        // Check if value contains special characters that require quoting
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                case '\'':
                case '\n':
                case '\r':
                case '\t':
                case '=':
                case '#':
                case '\b':
                case '\f':
                case '\v':
                    return QuoteStyle.Double;
            }
        }

        // Check for escape sequences
        if (value.Contains("\\n") || value.Contains("\\r") || value.Contains("\\t") ||
            value.Contains("\\u") || value.Contains("\\U") || value.Contains("\\b") ||
            value.Contains("\\f") || value.Contains("\\\\"))
        {
            return QuoteStyle.Double;
        }

        return QuoteStyle.None;
    }

    internal void AddVariable(string key, string value, QuoteStyle quote = QuoteStyle.None)
    {
        this.elements.Add(new EnvVariable(key, value, quote));
    }

    internal void AddInlineComment(string text)
    {
        this.elements.Add(new EnvComment(text, isInline: true));
    }
}

#pragma warning restore SA1202, SA1204