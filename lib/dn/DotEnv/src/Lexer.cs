using System;
using System.Collections.Generic;
using System.Text;

namespace NeoBeard.DotEnv;

internal static class Lexer
{
    public static List<Token> Lex(ReadOnlySpan<char> input)
    {
        var tokens = new List<Token>();
        var buffer = new StringBuilder();
        var line = 1;
        var column = 0;
        var quote = QuoteState.None;
        var kind = TokenKind.None;
        var keyTerminated = false;
        Mark? start = null;

        var i = 0;
        while (i < input.Length)
        {
            column++;

            if (start is null)
            {
                start = new Mark { Line = line, Column = column };
            }

            var c = input[i];
            var next = i + 1 < input.Length ? input[i + 1] : '\0';

            // Handle quoted values
            if (quote != QuoteState.None)
            {
                switch (quote)
                {
                    case QuoteState.Single:
                        if (c == '\\' && next == '\'')
                        {
                            buffer.Append('\'');
                            i++;
                            column++;
                            i++;
                            continue;
                        }

                        if (c == '\'')
                        {
                            CaptureToken(tokens, buffer, kind, TokenKind.Value, quote, ref start, line, column);
                            kind = TokenKind.None;
                            quote = QuoteState.None;
                            start = null;
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;

                    case QuoteState.Double:
                        if (c == '\\' && next == '"')
                        {
                            buffer.Append('"');
                            i++;
                            column++;
                            i++;
                            continue;
                        }

                        if (c == '"')
                        {
                            CaptureToken(tokens, buffer, kind, TokenKind.Value, quote, ref start, line, column);
                            kind = TokenKind.None;
                            quote = QuoteState.None;
                            start = null;
                        }
                        else if (c == '\\')
                        {
                            var shift = HandleEscapedChar(buffer, input, i);
                            if (shift > 0)
                            {
                                i += shift;
                                column += shift;
                            }
                            else
                            {
                                buffer.Append(c);
                            }
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;

                    case QuoteState.Backtick:
                        if (c == '\\' && next == '`')
                        {
                            buffer.Append('`');
                            i++;
                            column++;
                            i++;
                            continue;
                        }

                        if (c == '`')
                        {
                            CaptureToken(tokens, buffer, kind, TokenKind.Value, quote, ref start, line, column);
                            kind = TokenKind.None;
                            quote = QuoteState.None;
                            start = null;
                        }
                        else if (c == '\\')
                        {
                            var shift = HandleEscapedChar(buffer, input, i);
                            if (shift > 0)
                            {
                                i += shift;
                                column += shift;
                            }
                            else
                            {
                                buffer.Append(c);
                            }
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;
                }

                i++;

                // Handle trailing content after quote closes
                if (quote == QuoteState.None)
                {
                    column++;
                    var inComment = false;

                    while (i < input.Length)
                    {
                        c = input[i];

                        if (c == '\n' || c == '\r')
                        {
                            var n = i + 1;
                            if (c == '\r' && n < input.Length && input[n] == '\n')
                            {
                                i++;
                            }

                            if (inComment)
                            {
                                CaptureToken(tokens, buffer, TokenKind.Comment, TokenKind.Comment, QuoteState.None, ref start, line, column);
                            }

                            column = 0;
                            line++;
                            break;
                        }

                        if (inComment)
                        {
                            buffer.Append(c);
                            column++;
                            i++;
                            continue;
                        }

                        if (c == '#')
                        {
                            inComment = true;
                            kind = TokenKind.Comment;
                            start = new Mark { Line = line, Column = column };
                            i++;
                            column++;
                            continue;
                        }

                        if (!char.IsWhiteSpace(c))
                        {
                            throw new DotEnvParseException(
                                $"Invalid syntax: unexpected character '{c}' after quoted value",
                                line,
                                column);
                        }

                        column++;
                        i++;
                    }
                }

                continue;
            }

            // Handle line breaks
            var isLineBreak = c == '\n' || c == '\0';
            var shift2 = 0;

            if (c == '\r')
            {
                if (next == '\n')
                {
                    shift2 = 1;
                }

                isLineBreak = true;
            }

            if (isLineBreak)
            {
                switch (kind)
                {
                    case TokenKind.Name:
                        CaptureToken(tokens, buffer, kind, TokenKind.Name, QuoteState.None, ref start, line, column);
                        i += shift2;
                        kind = TokenKind.Value;
                        column = 0;
                        line++;
                        break;

                    case TokenKind.None:
                        if (buffer.Length == 0)
                        {
                            CaptureToken(tokens, buffer, TokenKind.Newline, TokenKind.Newline, QuoteState.None, ref start, line, column);
                            i += shift2;
                            kind = TokenKind.None;
                            column = 0;
                            line++;
                        }

                        break;

                    case TokenKind.Comment:
                        CaptureToken(tokens, buffer, kind, TokenKind.Comment, QuoteState.None, ref start, line, column);
                        i += shift2;
                        kind = TokenKind.None;
                        column = 0;
                        line++;
                        break;

                    case TokenKind.Value:
                        CaptureToken(tokens, buffer, kind, TokenKind.Value, QuoteState.None, ref start, line, column);
                        i += shift2;
                        column = 0;
                        line++;
                        kind = TokenKind.None;
                        break;
                }

                i++;
                continue;
            }

            // Handle different token kinds
            switch (kind)
            {
                case TokenKind.None:
                    if (c == '\n' || c == '\r')
                    {
                        var s = 0;
                        if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                        {
                            s = 1;
                            column++;
                        }

                        CaptureToken(tokens, buffer, TokenKind.Newline, TokenKind.Newline, QuoteState.None, ref start, line, column);
                        i += s;
                        line++;
                        column = 0;
                        i++;
                        continue;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    if (c == '#')
                    {
                        kind = TokenKind.Comment;
                        start = new Mark { Line = line, Column = column };
                        i++;
                        continue;
                    }

                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        kind = TokenKind.Name;
                        start = new Mark { Line = line, Column = column };
                        buffer.Append(c);
                        i++;
                        continue;
                    }

                    throw new DotEnvParseException(
                        $"Invalid syntax: unexpected character '{c}'",
                        line,
                        column);

                case TokenKind.Name:
                    if (c == '#' && buffer.Length == 0)
                    {
                        kind = TokenKind.Comment;
                        start = null;
                        i++;
                        continue;
                    }

                    if (c == '=')
                    {
                        CaptureToken(tokens, buffer, kind, TokenKind.Name, QuoteState.None, ref start, line, column);
                        kind = TokenKind.Value;
                        i++;
                        continue;
                    }

                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        if (keyTerminated)
                        {
                            throw new DotEnvParseException(
                                "Invalid syntax: key terminated by whitespace",
                                line,
                                column);
                        }

                        buffer.Append(c);
                        i++;
                        continue;
                    }

                    if (char.IsWhiteSpace(c))
                    {
                        if (buffer.Length > 0)
                        {
                            keyTerminated = true;
                        }

                        i++;
                        continue;
                    }

                    throw new DotEnvParseException(
                        $"Invalid syntax: unexpected character in name '{c}'",
                        line,
                        column);

                case TokenKind.Comment:
                    if (buffer.Length == 0 && char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;

                case TokenKind.Value:
                    if (buffer.Length == 0)
                    {
                        switch (c)
                        {
                            case '"':
                                quote = QuoteState.Double;
                                start = new Mark { Line = line, Column = column };
                                i++;
                                continue;
                            case '\'':
                                quote = QuoteState.Single;
                                start = new Mark { Line = line, Column = column };
                                i++;
                                continue;
                            case '`':
                                quote = QuoteState.Backtick;
                                start = new Mark { Line = line, Column = column };
                                i++;
                                continue;
                            default:
                                if (c == '\n' || c == '\r')
                                {
                                    var s = 0;
                                    if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                                    {
                                        s = 1;
                                    }

                                    CaptureToken(tokens, buffer, TokenKind.Newline, TokenKind.Newline, QuoteState.None, ref start, line, column);
                                    kind = TokenKind.None;
                                    i += s;
                                    start = null;
                                    line++;
                                    column = 0;
                                    i++;
                                    continue;
                                }

                                if (char.IsWhiteSpace(c))
                                {
                                    i++;
                                    continue;
                                }

                                buffer.Append(c);
                                i++;
                                continue;
                        }
                    }

                    if (c == '#')
                    {
                        CaptureToken(tokens, buffer, kind, TokenKind.Value, QuoteState.None, ref start, line, column);
                        kind = TokenKind.Comment;
                        i++;
                        continue;
                    }

                    if (buffer.Length == 0 && char.IsWhiteSpace(c))
                    {
                        i++;
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
            }
        }

        // Handle remaining buffer content
        if (buffer.Length > 0)
        {
            switch (kind)
            {
                case TokenKind.Name:
                    CaptureToken(tokens, buffer, kind, TokenKind.Name, QuoteState.None, ref start, line, column);
                    break;
                case TokenKind.Value:
                    CaptureToken(tokens, buffer, kind, TokenKind.Value, QuoteState.None, ref start, line, column);
                    break;
                case TokenKind.Comment:
                    CaptureToken(tokens, buffer, kind, TokenKind.Comment, QuoteState.None, ref start, line, column);
                    break;
                case TokenKind.None:
                    CaptureToken(tokens, buffer, TokenKind.Name, TokenKind.Name, QuoteState.None, ref start, line, column);
                    break;
            }
        }

        return tokens;
    }

    private static int HandleEscapedChar(StringBuilder buffer, ReadOnlySpan<char> input, int i)
    {
        if (i + 1 >= input.Length)
        {
            return 0;
        }

        var next = input[i + 1];

        switch (next)
        {
            case 'n':
                buffer.Append('\n');
                return 1;
            case 'r':
                buffer.Append('\r');
                return 1;
            case 't':
                buffer.Append('\t');
                return 1;
            case 'b':
                buffer.Append('\b');
                return 1;
            case 'f':
                buffer.Append('\f');
                return 1;
            case '\\':
                buffer.Append('\\');
                return 1;
            case '"':
                buffer.Append('"');
                return 1;
            case '\'':
                buffer.Append('\'');
                return 1;
            case '`':
                buffer.Append('`');
                return 1;
            case 'u':
                if (i + 5 < input.Length)
                {
                    var hex = input.Slice(i + 2, 4);
                    if (TryParseHex(hex, out var codePoint))
                    {
                        buffer.Append((char)codePoint);
                        return 5;
                    }
                }

                return 0;
            case 'U':
                if (i + 9 < input.Length)
                {
                    var hex = input.Slice(i + 2, 8);
                    if (TryParseHex(hex, out var codePoint))
                    {
                        buffer.Append(char.ConvertFromUtf32(codePoint));
                        return 9;
                    }
                }

                return 0;
            default:
                buffer.Append('\\');
                return 0;
        }
    }

    private static bool TryParseHex(ReadOnlySpan<char> hex, out int value)
    {
        value = 0;
        for (var i = 0; i < hex.Length; i++)
        {
            var c = hex[i];
            var digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => -1,
            };

            if (digit < 0)
            {
                return false;
            }

            value = (value << 4) | digit;
        }

        return true;
    }

    private static void CaptureToken(
        List<Token> tokens,
        StringBuilder buffer,
        TokenKind currentKind,
        TokenKind captureKind,
        QuoteState quote,
        ref Mark? start,
        int line,
        int column)
    {
        var value = buffer.ToString();
        buffer.Clear();

        // Trim trailing whitespace for unquoted values
        if (quote == QuoteState.None && captureKind == TokenKind.Value)
        {
            value = value.TrimEnd();
        }

        tokens.Add(new Token
        {
            Kind = captureKind,
            RawValue = value,
            Quote = quote,
            Start = start ?? new Mark { Line = line, Column = column },
            End = new Mark { Line = line, Column = column - 1 },
        });

        start = null;
    }
}