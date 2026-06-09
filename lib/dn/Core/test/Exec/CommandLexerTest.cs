using System;
using System.Collections.Generic;

using NeoBeard.Exec;

using Xunit;

namespace NeoBeard.Exec.Tests;

/// <summary>
/// Unit tests for <see cref="CommandLexer"/>.
/// </summary>
public class CommandLexerTest
{
    /// <summary>
    /// Tests tokenization of a simple command with arguments.
    /// </summary>
    [Fact]
    public void Tokenize_SimpleCommand_ReturnsArguments()
    {
        var input = "echo hello world";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(3, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "hello", CommandTokenKind.Arg);
        AssertToken(tokens[2], "world", CommandTokenKind.Arg);
    }

    /// <summary>
    /// Tests tokenization with single-quoted argument.
    /// </summary>
    [Fact]
    public void Tokenize_SingleQuotedArg_ReturnsSingleQuotedToken()
    {
        var input = "echo 'hello world'";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(2, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "hello world", CommandTokenKind.SingleQuotedArg);
    }

    /// <summary>
    /// Tests tokenization with double-quoted argument.
    /// </summary>
    [Fact]
    public void Tokenize_DoubleQuotedArg_ReturnsDoubleQuotedToken()
    {
        var input = "echo \"hello world\"";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(2, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "hello world", CommandTokenKind.DoubleQuotedArg);
    }

    /// <summary>
    /// Tests tokenization with sub-process start and end.
    /// </summary>
    [Fact]
    public void Tokenize_SubProcessTokens_ReturnsSubProcessTokens()
    {
        var input = "echo (ls)";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(4, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "(", CommandTokenKind.SubProcessStart);
        AssertToken(tokens[2], "ls", CommandTokenKind.Arg);
        AssertToken(tokens[3], ")", CommandTokenKind.SubProcessEnd);
    }

    /// <summary>
    /// Tests tokenization with statement end.
    /// </summary>
    [Fact]
    public void Tokenize_StatementEnd_ReturnsStatementEndToken()
    {
        var input = "echo foo; echo bar";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(5, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "foo", CommandTokenKind.Arg);
        AssertToken(tokens[2], ";", CommandTokenKind.StatementEnd);
        AssertToken(tokens[3], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[4], "bar", CommandTokenKind.Arg);
    }

    /// <summary>
    /// Tests tokenization with &amp;&amp; and || operators.
    /// </summary>
    [Fact]
    public void Tokenize_AndOrOperators_ReturnsOperatorTokens()
    {
        var input = "foo && bar || baz";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(5, tokens.Count);
        AssertToken(tokens[0], "foo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "&&", CommandTokenKind.And);
        AssertToken(tokens[2], "bar", CommandTokenKind.Arg);
        AssertToken(tokens[3], "||", CommandTokenKind.Or);
        AssertToken(tokens[4], "baz", CommandTokenKind.Arg);
    }

    /// <summary>
    /// Tests tokenization with pipe operator.
    /// </summary>
    [Fact]
    public void Tokenize_PipeOperator_ReturnsPipeToken()
    {
        var input = "ls | grep foo";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(4, tokens.Count);
        AssertToken(tokens[0], "ls", CommandTokenKind.Arg);
        AssertToken(tokens[1], "|", CommandTokenKind.Pipe);
        AssertToken(tokens[2], "grep", CommandTokenKind.Arg);
        AssertToken(tokens[3], "foo", CommandTokenKind.Arg);
    }

    /// <summary>
    /// Tests tokenization with backslash and backtick line continuations.
    /// </summary>
    [Fact]
    public void Tokenize_LineContinuation_SplitsArguments()
    {
        var input = "echo foo \\\nbar";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Equal(3, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "foo", CommandTokenKind.Arg);
        AssertToken(tokens[2], "bar", CommandTokenKind.Arg);
    }

    [Fact]

    public void Tokenize_Quoted_CommandSubstitution()
    {
        var input = """echo "$(echo "nested")" """;
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        foreach (var t in tokens)
        {
            Console.WriteLine($"Token: '{t.Value}' Kind: {t.Kind}");
        }

        Assert.Equal(2, tokens.Count);
        AssertToken(tokens[0], "echo", CommandTokenKind.Arg);
        AssertToken(tokens[1], "$(echo \"nested\")", CommandTokenKind.DoubleQuotedArg);
    }

    /// <summary>
    /// Tests tokenization with empty input.
    /// </summary>
    [Fact]
    public void Tokenize_EmptyInput_ReturnsNoTokens()
    {
        var input = string.Empty;
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Empty(tokens);
    }

    /// <summary>
    /// Tests tokenization with only whitespace.
    /// </summary>
    [Fact]
    public void Tokenize_WhitespaceOnly_ReturnsNoTokens()
    {
        var input = "    ";
        var tokens = CommandLexer.Tokenize(input.AsSpan());

        Assert.Empty(tokens);
    }

    /// <summary>
    /// Helper to assert token value and kind.
    /// </summary>
    private static void AssertToken(CommandToken token, string expectedValue, CommandTokenKind expectedKind)
    {
        Assert.Equal(expectedValue, token.Value);
        Assert.Equal(expectedKind, token.Kind);
    }
}