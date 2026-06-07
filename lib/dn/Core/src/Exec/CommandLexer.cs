using NeoBeard.Text;

namespace NeoBeard.Exec;

/// <summary>
/// Represents the CommandLexer class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class CommandLexer
{
    private enum Quote
    {
        None = 0,

        Single = 1,

        Double = 2,
    }

    /// <summary>
    /// Tokenizes a command line string into a list of command tokens.
    /// </summary>
    /// <param name="commandLine">The command line to tokenize.</param>
    /// <returns>A read-only list of command tokens.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tokens = CommandLexer.Tokenize("echo 'hello world'");
    /// Assert.Equal(2, tokens.Count);
    /// Assert.Equal("echo", tokens[0].Value);
    /// Assert.Equal(CommandTokenKind.Arg, tokens[0].Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<CommandToken> Tokenize(ReadOnlySpan<char> commandLine)
    {
        var token = StringBuilderCache.Acquire();
        var quote = Quote.None;
        var tokens = new List<CommandToken>();
        var inCommandExpression = false;

        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];
            var d = char.MinValue;
            var e = char.MinValue;
            if (i + 1 < commandLine.Length)
                d = commandLine[i + 1];
            if (i + 2 < commandLine.Length)
                e = commandLine[i + 2];

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
                                tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.SingleQuotedArg));
                                token.Clear();
                            }
                        }
                        else
                        {
                            token.Append(c);
                        }

                        continue;

                    case Quote.Double:
                        // totally legal to have nested double quotes when there is
                        // a command expression like: "$(echo "nested")"
                        // so we need to track if we are in a command expression or not.
                        if (!inCommandExpression && c == '$' && d == '(')
                        {
                            inCommandExpression = true;
                            token.Append(c);
                            continue;
                        }

                        if (inCommandExpression && c == ')')
                        {
                            inCommandExpression = false;
                            token.Append(c);
                            continue;
                        }

                        if (c == '\"' && !inCommandExpression)
                        {
                            quote = Quote.Double;
                            if (token.Length > 0)
                            {
                                tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.DoubleQuotedArg));
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
                var remaining = commandLine.Length - 1 - i;
                if (remaining > 2)
                {
                    var j = commandLine[i + 1];
                    var k = commandLine[i + 2];

                    if ((j == '\\' || j == '`') && k == '\n')
                    {
                        i += 2;
                        if (token.Length > 0)
                        {
                            tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                        }

                        token.Clear();
                        continue;
                    }

                    if (remaining > 3)
                    {
                        var l = commandLine[i + 3];
                        if (k == '\r' && l == '\n')
                        {
                            i += 3;
                            if (token.Length > 0)
                            {
                                tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                            }

                            token.Clear();
                            continue;
                        }
                    }
                }

                if (token.Length > 0)
                {
                    tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
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

                    case ' ':
                        continue;

                    case '(':
                        tokens.Add(new CommandToken("(", CommandTokenKind.SubProcessStart));
                        continue;

                    case ')':
                        tokens.Add(new CommandToken(")", CommandTokenKind.SubProcessEnd));
                        continue;

                    case ';':
                        tokens.Add(new CommandToken(";", CommandTokenKind.StatementEnd));
                        continue;

                    case '&':
                        if (d is '&' && e is ' ' or char.MinValue)
                        {
                            tokens.Add(new CommandToken("&&", CommandTokenKind.And));
                            i++;
                            continue;
                        }

                        break;
                    case '|':
                        if (d is '|' && e is ' ' or char.MinValue)
                        {
                            tokens.Add(new CommandToken("||", CommandTokenKind.Or));
                            i++;
                            continue;
                        }

                        if (d is ' ' or char.MinValue)
                        {
                            tokens.Add(new CommandToken("|", CommandTokenKind.Pipe));
                            continue;
                        }

                        break;
                }
            }

            switch (c)
            {
                case '\\':
                    if (d is '(' or ')' or '&' or '|' or ';' or '\\')
                    {
                        i++;
                        token.Append(d);
                        break;
                    }

                    break;

                case ')':
                    if (token.Length > 0)
                    {
                        tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                        token.Clear();
                    }

                    tokens.Add(new CommandToken(")", CommandTokenKind.SubProcessEnd));
                    break;
                case ';':
                    if (token.Length > 0)
                    {
                        tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                        token.Clear();
                    }

                    tokens.Add(new CommandToken(";", CommandTokenKind.StatementEnd));
                    break;
                case '&':
                    if (d is '&' && e is ' ' or char.MinValue)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                            token.Clear();
                        }

                        tokens.Add(new CommandToken("&&", CommandTokenKind.And));
                        i++;
                        break;
                    }

                    if (token.Length > 0)
                    {
                        tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                        token.Clear();
                    }

                    tokens.Add(new CommandToken("&", CommandTokenKind.Invalid));
                    break;

                case '|':
                    if (d is '|' && e is ' ' or char.MinValue)
                    {
                        if (token.Length > 0)
                        {
                            tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                            token.Clear();
                        }

                        tokens.Add(new CommandToken("||", CommandTokenKind.Or));
                        i++;
                        break;
                    }

                    if (token.Length > 0)
                    {
                        tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
                        token.Clear();
                    }

                    tokens.Add(new CommandToken("|", CommandTokenKind.Pipe));
                    break;

                default:
                    token.Append(c);
                    break;
            }
        }

        if (token.Length > 0)
        {
            var value = token.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                tokens.Add(new CommandToken(token.ToString(), CommandTokenKind.Arg));
            }
        }

        StringBuilderCache.Release(token);

        return tokens;
    }

    // Lexer implementation goes here
}