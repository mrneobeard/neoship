using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeoBeard.DotEnv;

/// <summary>
/// Provides static methods for parsing dotenv files and content.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Parse a single file
/// var doc = DotEnvFile.ParseFile(".env");
///
/// // Parse multiple files with optional files (ending with ?)
/// var doc = DotEnvFile.ParseFiles(".env", ".env.local?", ".env.production?");
///
/// // Parse string content
/// var doc = DotEnvFile.Parse("KEY=value\nKEY2=value2");
///
/// // TryParse without throwing
/// var result = DotEnvFile.TryParseFile(".env");
/// if (result.IsOk)
/// {
///     var doc = result.Doc;
/// }
/// </code>
/// </example>
/// </remarks>
public static class DotEnvFile
{
    /// <summary>
    /// Parses a dotenv content string.
    /// </summary>
    /// <param name="input">The dotenv content to parse.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.Parse("KEY=value\nKEY2=\"quoted value\"");
    /// var value = doc.Get("KEY");
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc Parse(string input)
        => Parse(input.AsSpan());

    /// <summary>
    /// Parses dotenv content from a span.
    /// </summary>
    /// <param name="input">The dotenv content to parse.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var content = "KEY=value"u8;
    /// var doc = DotEnvFile.Parse(content);
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc Parse(ReadOnlySpan<char> input)
    {
        var tokens = Lexer.Lex(input);
        return BuildDocument(tokens);
    }

    /// <summary>
    /// Parses a dotenv file from the specified path.
    /// </summary>
    /// <param name="path">The path to the dotenv file.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = DotEnvFile.ParseFile(".env");
    /// var dbUrl = doc.Get("DATABASE_URL");
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Dotenv file not found: {path}", path);
        }

        var content = File.ReadAllText(path);
        return Parse(content);
    }

    /// <summary>
    /// Tries to parse a dotenv file from the specified path.
    /// </summary>
    /// <param name="path">The path to the dotenv file.</param>
    /// <returns>A result containing the parsed document or an error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = DotEnvFile.TryParseFile(".env");
    /// if (result.IsOk)
    /// {
    ///     var doc = result.Doc;
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Error: {result.Error?.Message}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static DotEnvResult TryParseFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new DotEnvResult(null, new FileNotFoundException($"Dotenv file not found: {path}", path));
            }

            var content = File.ReadAllText(path);
            return new DotEnvResult(Parse(content), null);
        }
        catch (Exception ex)
        {
            return new DotEnvResult(null, ex);
        }
    }

    /// <summary>
    /// Parses multiple dotenv files in order, with later files overriding earlier keys.
    /// Paths ending with ? are optional and will be skipped if the file doesn't exist.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <returns>The merged parsed document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when a required file does not exist.</exception>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// // .env is required, .env.local? is optional
    /// var doc = DotEnvFile.ParseFiles(".env", ".env.local?", ".env.production?");
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc ParseFiles(params string[] paths)
        => ParseFiles((IEnumerable<string>)paths);

    /// <summary>
    /// Parses multiple dotenv files in order, with later files overriding earlier keys.
    /// Paths ending with ? are optional and will be skipped if the file doesn't exist.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <returns>The merged parsed document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when a required file does not exist.</exception>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var paths = new[] { ".env", ".env.local?", ".env.production?" };
    /// var doc = DotEnvFile.ParseFiles(paths);
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc ParseFiles(IEnumerable<string> paths)
    {
        var result = new EnvDoc();

        foreach (var path in paths)
        {
            var (isOptional, cleanPath) = IsOptionalPath(path);

            if (!File.Exists(cleanPath))
            {
                if (isOptional)
                {
                    continue;
                }

                throw new FileNotFoundException($"Required dotenv file not found: {cleanPath}", cleanPath);
            }

            var content = File.ReadAllText(cleanPath);
            var doc = Parse(content);
            result.Merge(doc);
        }

        return result;
    }

    /// <summary>
    /// Tries to parse multiple dotenv files in order.
    /// Paths ending with ? are optional and will be skipped if the file doesn't exist.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <returns>A result containing the merged parsed document or an error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = DotEnvFile.TryParseFiles(".env", ".env.local?");
    /// if (result.IsOk)
    /// {
    ///     var doc = result.Doc;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static DotEnvResult TryParseFiles(params string[] paths)
        => TryParseFiles((IEnumerable<string>)paths);

    /// <summary>
    /// Tries to parse multiple dotenv files in order.
    /// Paths ending with ? are optional and will be skipped if the file doesn't exist.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <returns>A result containing the merged parsed document or an error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var paths = new[] { ".env", ".env.local?" };
    /// var result = DotEnvFile.TryParseFiles(paths);
    /// </code>
    /// </example>
    /// </remarks>
    public static DotEnvResult TryParseFiles(IEnumerable<string> paths)
    {
        try
        {
            var result = new EnvDoc();

            foreach (var path in paths)
            {
                var (isOptional, cleanPath) = IsOptionalPath(path);

                if (!File.Exists(cleanPath))
                {
                    if (isOptional)
                    {
                        continue;
                    }

                    return new DotEnvResult(null, new FileNotFoundException($"Required dotenv file not found: {cleanPath}", cleanPath));
                }

                var content = File.ReadAllText(cleanPath);
                var doc = Parse(content);
                result.Merge(doc);
            }

            return new DotEnvResult(result, null);
        }
        catch (Exception ex)
        {
            return new DotEnvResult(null, ex);
        }
    }

    /// <summary>
    /// Asynchronously parses a dotenv file from the specified path.
    /// </summary>
    /// <param name="path">The path to the dotenv file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the parsed document.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = await DotEnvFile.ParseFileAsync(".env");
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<EnvDoc> ParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Dotenv file not found: {path}", path);
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return Parse(content);
    }

    /// <summary>
    /// Asynchronously tries to parse a dotenv file from the specified path.
    /// </summary>
    /// <param name="path">The path to the dotenv file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a result with the parsed document or an error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = await DotEnvFile.TryParseFileAsync(".env");
    /// if (result.IsOk)
    /// {
    ///     var doc = result.Doc;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<DotEnvResult> TryParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new DotEnvResult(null, new FileNotFoundException($"Dotenv file not found: {path}", path));
            }

            var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return new DotEnvResult(Parse(content), null);
        }
        catch (Exception ex)
        {
            return new DotEnvResult(null, ex);
        }
    }

    /// <summary>
    /// Asynchronously parses multiple dotenv files in order.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the merged parsed document.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var doc = await DotEnvFile.ParseFilesAsync(new[] { ".env", ".env.local?" });
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<EnvDoc> ParseFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var result = new EnvDoc();

        foreach (var path in paths)
        {
            var (isOptional, cleanPath) = IsOptionalPath(path);

            if (!File.Exists(cleanPath))
            {
                if (isOptional)
                {
                    continue;
                }

                throw new FileNotFoundException($"Required dotenv file not found: {cleanPath}", cleanPath);
            }

            var content = await File.ReadAllTextAsync(cleanPath, cancellationToken).ConfigureAwait(false);
            var doc = Parse(content);
            result.Merge(doc);
        }

        return result;
    }

    /// <summary>
    /// Asynchronously tries to parse multiple dotenv files in order.
    /// </summary>
    /// <param name="paths">The paths to the dotenv files.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing a result with the merged parsed document or an error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = await DotEnvFile.TryParseFilesAsync(new[] { ".env", ".env.local?" });
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<DotEnvResult> TryParseFilesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new EnvDoc();

            foreach (var path in paths)
            {
                var (isOptional, cleanPath) = IsOptionalPath(path);

                if (!File.Exists(cleanPath))
                {
                    if (isOptional)
                    {
                        continue;
                    }

                    return new DotEnvResult(null, new FileNotFoundException($"Required dotenv file not found: {cleanPath}", cleanPath));
                }

                var content = await File.ReadAllTextAsync(cleanPath, cancellationToken).ConfigureAwait(false);
                var doc = Parse(content);
                result.Merge(doc);
            }

            return new DotEnvResult(result, null);
        }
        catch (Exception ex)
        {
            return new DotEnvResult(null, ex);
        }
    }

    /// <summary>
    /// Parses dotenv content from a stream.
    /// </summary>
    /// <param name="stream">The stream containing dotenv content.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var stream = File.OpenRead(".env");
    /// var doc = DotEnvFile.ParseStream(stream);
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc ParseStream(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    /// <summary>
    /// Parses multiple dotenv streams in order, with later streams overriding earlier keys.
    /// </summary>
    /// <param name="streams">The streams containing dotenv content.</param>
    /// <returns>The merged parsed document.</returns>
    /// <exception cref="DotEnvParseException">Thrown when parsing fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var s1 = File.OpenRead(".env");
    /// using var s2 = File.OpenRead(".env.local");
    /// var doc = DotEnvFile.ParseStreams(s1, s2);
    /// </code>
    /// </example>
    /// </remarks>
    public static EnvDoc ParseStreams(params Stream[] streams)
    {
        var result = new EnvDoc();

        foreach (var stream in streams)
        {
            var doc = ParseStream(stream);
            result.Merge(doc);
        }

        return result;
    }

    /// <summary>
    /// Asynchronously parses dotenv content from a stream.
    /// </summary>
    /// <param name="stream">The stream containing dotenv content.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the parsed document.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var stream = File.OpenRead(".env");
    /// var doc = await DotEnvFile.ParseStreamAsync(stream);
    /// </code>
    /// </example>
    /// </remarks>
    public static async Task<EnvDoc> ParseStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
        return Parse(content);
    }

    private static (bool IsOptional, string CleanPath) IsOptionalPath(string path)
    {
        if (path.EndsWith("?", StringComparison.Ordinal))
        {
            return (true, path.Substring(0, path.Length - 1));
        }

        return (false, path);
    }

    private static EnvDoc BuildDocument(List<Token> tokens)
    {
        var doc = new EnvDoc();
        string? key = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            switch (token.Kind)
            {
                case TokenKind.Newline:
                    key = null;
                    doc.AddNewline();
                    break;

                case TokenKind.Comment:
                    key = null;
                    doc.AddComment(token.RawValue);
                    break;

                case TokenKind.Name:
                    if (key is not null)
                    {
                        // Previous key had no value
                        doc.AddVariable(key, string.Empty);
                    }

                    key = token.RawValue;
                    break;

                case TokenKind.Value:
                    if (key is null)
                    {
                        throw new DotEnvParseException(
                            "Invalid syntax: value without a key",
                            token.Start.Line,
                            token.Start.Column);
                    }

                    var quote = token.Quote switch
                    {
                        QuoteState.Single => QuoteStyle.Single,
                        QuoteState.Double => QuoteStyle.Double,
                        QuoteState.Backtick => QuoteStyle.Backtick,
                        _ => QuoteStyle.None,
                    };

                    doc.AddVariable(key, token.RawValue, quote);
                    key = null;
                    break;
            }
        }

        // Handle trailing key without value
        if (key is not null)
        {
            doc.AddVariable(key, string.Empty);
        }

        return doc;
    }
}