using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using NeoBeard.Text;
using NeoBeard.Text.Extras;

namespace NeoBeard.Exec;

/// <summary>
/// Represents a mutable list of command-line arguments with parsing and formatting helpers.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// CommandArgs args = CommandArgs.From("dotnet build --configuration Release");
/// string first = args.Shift();
/// </code>
/// </example>
/// </remarks>
public partial class CommandArgs : List<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandArgs"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// </code>
    /// </example>
    /// </remarks>
    public CommandArgs()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandArgs"/> class from existing arguments.
    /// </summary>
    /// <param name="args">The arguments to copy.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs(new[] { "dotnet", "--info" });
    /// </code>
    /// </example>
    /// </remarks>
    public CommandArgs(IEnumerable<string> args)
        : base(args)
    {
    }

    private enum Quote
    {
        None = 0,

        Single = 1,

        Double = 2,
    }

    /// <summary>
    /// Gets or sets a value indicating whether generated argument strings are escaped.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs { Escape = false };
    /// args.Add("--flag", "value");
    /// </code>
    /// </example>
    /// </remarks>
    public bool Escape { get; set; } = true;

    /// <summary>
    /// Converts an argument string into a <see cref="CommandArgs"/> instance.
    /// </summary>
    /// <param name="args">The command-line string.</param>
    /// <returns>A parsed <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = "dotnet --info";
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(string args)
        => From(args);

    /// <summary>
    /// Converts a <see cref="StringBuilder"/> into a <see cref="CommandArgs"/> instance.
    /// </summary>
    /// <param name="args">The command-line string builder.</param>
    /// <returns>A parsed <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = new StringBuilder("dotnet --info");
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(StringBuilder args)
        => From(args);

    /// <summary>
    /// Converts a string array into a <see cref="CommandArgs"/> instance.
    /// </summary>
    /// <param name="args">The argument values.</param>
    /// <returns>A <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = new[] { "dotnet", "--info" };
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(string[] args)
        => new(args);

    /// <summary>
    /// Converts a <see cref="Collection{T}"/> into a <see cref="CommandArgs"/> instance.
    /// </summary>
    /// <param name="args">The source collection.</param>
    /// <returns>A <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = new Collection&lt;string&gt;(new List&lt;string&gt; { "dotnet", "--info" });
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(Collection<string> args)
        => new(args);

    /// <summary>
    /// Converts a <see cref="StringCollection"/> into a <see cref="CommandArgs"/> instance.
    /// </summary>
    /// <param name="args">The source collection.</param>
    /// <returns>A <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var collection = new StringCollection { "dotnet", "--info" };
    /// CommandArgs args = collection;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator CommandArgs(StringCollection args)
        => From(args);

    /// <summary>
    /// Converts a <see cref="CommandArgs"/> instance into a command-line string.
    /// </summary>
    /// <param name="args">The source arguments.</param>
    /// <returns>A command-line string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string commandLine = (string)CommandArgs.From("dotnet --info");
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator string(CommandArgs args)
        => args.ToString();

    /// <summary>
    /// Converts a <see cref="CommandArgs"/> instance to a <see cref="Collection{T}"/>.
    /// </summary>
    /// <param name="args">The source arguments.</param>
    /// <returns>A collection containing the argument values.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Collection&lt;string&gt; items = CommandArgs.From("dotnet --info");
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Collection<string>(CommandArgs args)
        => new Collection<string>(args);

    /// <summary>
    /// Splits a command-line span into argument tokens.
    /// </summary>
    /// <param name="args">The command-line span to split.</param>
    /// <returns>A list of parsed argument tokens.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// IReadOnlyList&lt;string&gt; tokens = CommandArgs.SplitArguments("dotnet --info".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<string> SplitArguments(ReadOnlySpan<char> args)
    {
        var token = StringBuilderCache.Acquire();
        var quote = Quote.None;
        var tokens = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var c = args[i];

            if (quote != Quote.None)
            {
                switch (quote)
                {
                    case Quote.Single:
                        if (c == '\'')
                        {
                            quote = Quote.None;
                            if (token.Length > 0)
                            {
                                tokens.Add(token.ToString());
                                token.Clear();
                            }
                        }
                        else
                        {
                            token.Append(c);
                        }

                        continue;

                    case Quote.Double:
                        if (c == '\"')
                        {
                            quote = Quote.Double;
                            if (token.Length > 0)
                            {
                                tokens.Add(token.ToString());
                                token.Clear();
                            }
                        }
                        else
                        {
                            token.Append(c);
                        }

                        continue;
                }

                token.Append(c);
                continue;
            }

            if (c == ' ')
            {
                // handle backtick (`) and backslash (\) to notate a new line and different argument.
                var remaining = args.Length - 1 - i;
                if (remaining > 2)
                {
                    var j = args[i + 1];
                    var k = args[i + 2];

                    if ((j == '\\' || j == '`') && k == '\n')
                    {
                        i += 2;
                        if (token.Length > 0)
                        {
                            tokens.Add(token.ToString());
                        }

                        token.Clear();
                        continue;
                    }

                    if (remaining > 3)
                    {
                        var l = args[i + 3];
                        if (k == '\r' && l == '\n')
                        {
                            i += 3;
                            if (token.Length > 0)
                            {
                                tokens.Add(token.ToString());
                            }

                            token.Clear();
                            continue;
                        }
                    }
                }

                if (token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }

                continue;
            }

            if (token.Length == 0)
            {
                switch (c)
                {
                    case '\'':
                        quote = Quote.Single;
                        continue;

                    case '\"':
                        quote = Quote.Double;
                        continue;
                }
            }

            token.Append(c);
        }

        if (token.Length > 0)
            tokens.Add(token.ToString());

        StringBuilderCache.Release(token);

        return tokens;
    }

    /// <summary>
    /// Parses command-line arguments from a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="builder">The string builder containing command-line arguments.</param>
    /// <returns>A parsed <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = CommandArgs.From(new StringBuilder("dotnet --info"));
    /// </code>
    /// </example>
    /// </remarks>
    public static CommandArgs From(StringBuilder builder)
    {
        return From(builder.ToString());
    }

    /// <summary>
    /// Creates a <see cref="CommandArgs"/> instance from a <see cref="StringCollection"/>.
    /// </summary>
    /// <param name="collection">The source collection.</param>
    /// <returns>A <see cref="CommandArgs"/> instance containing non-null items.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var collection = new StringCollection { "dotnet", "--info" };
    /// CommandArgs args = CommandArgs.From(collection);
    /// </code>
    /// </example>
    /// </remarks>
    public static CommandArgs From(StringCollection collection)
    {
        var next = new CommandArgs();
        foreach (var item in collection)
        {
            if (item is null)
                continue;

            next.Add(item);
        }

        return next;
    }

    /// <summary>
    /// Parses command-line arguments from a string.
    /// </summary>
    /// <param name="args">The command-line string.</param>
    /// <returns>A parsed <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = CommandArgs.From("dotnet --info");
    /// </code>
    /// </example>
    /// </remarks>
    public static CommandArgs From(string args)
    {
        return new CommandArgs(SplitArguments(args.AsSpan()));
    }

    /// <summary>
    /// Creates a <see cref="CommandArgs"/> instance from an enumerable sequence.
    /// </summary>
    /// <param name="args">The source arguments.</param>
    /// <returns>A <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = CommandArgs.From(new[] { "dotnet", "--info" }.AsEnumerable());
    /// </code>
    /// </example>
    /// </remarks>
    public static CommandArgs From(IEnumerable<string> args)
    {
        return new CommandArgs(args);
    }

    /// <summary>
    /// Creates a <see cref="CommandArgs"/> instance from the specified values.
    /// </summary>
    /// <param name="args">The argument values.</param>
    /// <returns>A <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// CommandArgs args = CommandArgs.From("dotnet", "--info");
    /// </code>
    /// </example>
    /// </remarks>
    public static CommandArgs From(params string[] args)
    {
        return new CommandArgs(args);
    }

    /// <summary>
    /// Adds an argument from a character span.
    /// </summary>
    /// <param name="item">The argument to add.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// args.Add("--info".AsSpan());
    /// </code>
    /// </example>
    /// </remarks>
    [SuppressMessage("", "SA1100:Do not prefix calls with base unless local implementation exists", Justification = "required")]
    public void Add(ReadOnlySpan<char> item)
        => base.Add(item.ToString());

    /// <summary>
    /// Adds two arguments.
    /// </summary>
    /// <param name="item1">The first argument.</param>
    /// <param name="item2">The second argument.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// args.Add("dotnet", "--info");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string item1, string item2)
    {
        this.Add(item1);
        this.Add(item2);
    }

    /// <summary>
    /// Adds three arguments.
    /// </summary>
    /// <param name="item1">The first argument.</param>
    /// <param name="item2">The second argument.</param>
    /// <param name="item3">The third argument.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// args.Add("dotnet", "build", "--help");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string item1, string item2, string item3)
    {
        this.Add(item1);
        this.Add(item2);
        this.Add(item3);
    }

    /// <summary>
    /// Adds four arguments.
    /// </summary>
    /// <param name="item1">The first argument.</param>
    /// <param name="item2">The second argument.</param>
    /// <param name="item3">The third argument.</param>
    /// <param name="item4">The fourth argument.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// args.Add("dotnet", "build", "-c", "Release");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string item1, string item2, string item3, string item4)
    {
        this.Add(item1);
        this.Add(item2);
        this.Add(item3);
        this.Add(item4);
    }

    /// <summary>
    /// Adds five arguments.
    /// </summary>
    /// <param name="item1">The first argument.</param>
    /// <param name="item2">The second argument.</param>
    /// <param name="item3">The third argument.</param>
    /// <param name="item4">The fourth argument.</param>
    /// <param name="item5">The fifth argument.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = new CommandArgs();
    /// args.Add("dotnet", "test", "-c", "Release", "--no-build");
    /// </code>
    /// </example>
    /// </remarks>
    public void Add(string item1, string item2, string item3, string item4, string item5)
    {
        this.Add(item1);
        this.Add(item2);
        this.Add(item3);
        this.Add(item4);
        this.Add(item5);
    }

    /// <summary>
    /// Removes and returns the first argument.
    /// </summary>
    /// <returns>The first argument, or an empty string when no arguments are available.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = CommandArgs.From("dotnet --info");
    /// string first = args.Shift();
    /// </code>
    /// </example>
    /// </remarks>
    public string Shift()
    {
        if (this.Count == 0)
            return string.Empty;

        var item = this[0];
        this.RemoveAt(0);
        return item;
    }

    /// <summary>
    /// Inserts an argument at the beginning of the list.
    /// </summary>
    /// <param name="item">The argument to insert.</param>
    /// <returns>The current <see cref="CommandArgs"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var args = CommandArgs.From("--info").Unshift("dotnet");
    /// </code>
    /// </example>
    /// </remarks>
    public CommandArgs Unshift(string item)
    {
        this.Insert(0, item);
        return this;
    }

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>A command-line string created from the current arguments.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string commandLine = CommandArgs.From("dotnet", "--info").ToString();
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString()
    {
        var sb = StringBuilderCache.Acquire();

        foreach (var argument in this)
        {
            if (sb.Length > 0)
                sb.Append(' ');

            sb.AppendCliArgument(argument);
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }
}