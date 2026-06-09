using System;

namespace NeoBeard.DotEnv;

/// <summary>
/// Exception thrown when parsing a dotenv file fails.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// try
/// {
///     var doc = DotEnv.Parse("invalid content");
/// }
/// catch (DotEnvParseException ex)
/// {
///     Console.WriteLine($"Parse error at line {ex.Line}, column {ex.Column}: {ex.Message}");
/// }
/// </code>
/// </example>
/// </remarks>
public sealed class DotEnvParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    public DotEnvParseException(string message, int line, int column)
        : base($"{message} at line {line}, column {column}")
    {
        this.Line = line;
        this.Column = column;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The line number where the error occurred.</param>
    /// <param name="column">The column number where the error occurred.</param>
    /// <param name="innerException">The inner exception.</param>
    public DotEnvParseException(string message, int line, int column, Exception? innerException)
        : base($"{message} at line {line}, column {column}", innerException)
    {
        this.Line = line;
        this.Column = column;
    }

    /// <summary>
    /// Gets the line number where the error occurred.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// try
    /// {
    ///     var doc = DotEnv.Parse("invalid");
    /// }
    /// catch (DotEnvParseException ex)
    /// {
    ///     Console.WriteLine($"Error at line {ex.Line}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public int Line { get; }

    /// <summary>
    /// Gets the column number where the error occurred.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// try
    /// {
    ///     var doc = DotEnv.Parse("invalid");
    /// }
    /// catch (DotEnvParseException ex)
    /// {
    ///     Console.WriteLine($"Error at column {ex.Column}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public int Column { get; }
}