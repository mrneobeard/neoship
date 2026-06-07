using System.Text;

using NeoBeard.Extras;
using NeoBeard.Text.Extras;

namespace NeoBeard.Exec;

/// <summary>
/// Represents the output result of a command execution, including exit code, stdout, stderr, and timing information.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var command = new Command(["dotnet", "--version"]);
/// var output = command.Output();
///
/// if (output.IsOk)
/// {
///     Console.WriteLine($"Success: {output.Text()}");
/// }
/// else
/// {
///     Console.WriteLine($"Failed with exit code {output.ExitCode}");
///     Console.WriteLine($"Error: {output.ErrorText()}");
/// }
/// </code>
/// </example>
/// </remarks>
public readonly struct Output
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Output"/> struct with default values.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Output();
    /// Assert.Equal(0, output.ExitCode);
    /// Assert.Equal(string.Empty, output.FileName);
    /// </code>
    /// </example>
    /// </remarks>
    public Output()
    {
        this.ExitCode = 0;
        this.FileName = string.Empty;
        this.Stdout = [];
        this.Stderr = [];
        this.StartTime = DateTime.MinValue;
        this.ExitTime = DateTime.MinValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Output"/> struct with the specified values and error.
    /// </summary>
    /// <param name="fileName">The name or path of the executable that was run.</param>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="error">An exception that occurred during execution, if any.</param>
    /// <param name="stdout">The standard output bytes captured from the process.</param>
    /// <param name="stderr">The standard error bytes captured from the process.</param>
    /// <param name="startTime">The time the process started.</param>
    /// <param name="exitTime">The time the process exited.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Output("test", 1, new InvalidOperationException("Failed"), stdout: [1, 2, 3]);
    /// Assert.True(output.IsError);
    /// Assert.NotNull(output.Error);
    /// </code>
    /// </example>
    /// </remarks>
    public Output(
        string fileName,
        int exitCode,
        Exception error,
        byte[]? stdout = null,
        byte[]? stderr = null,
        DateTime? startTime = null,
        DateTime? exitTime = null)
    {
        this.FileName = fileName;
        this.ExitCode = exitCode;
        this.Error = error;
        this.Stdout = stdout ?? [];
        this.Stderr = stderr ?? [];
        this.StartTime = startTime ?? DateTime.MinValue;
        this.ExitTime = exitTime ?? DateTime.MinValue;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Output"/> struct with the specified values.
    /// </summary>
    /// <param name="fileName">The name or path of the executable that was run.</param>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="stdout">The standard output bytes captured from the process.</param>
    /// <param name="stderr">The standard error bytes captured from the process.</param>
    /// <param name="startTime">The time the process started.</param>
    /// <param name="exitTime">The time the process exited.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Output("dotnet", 0, stdout: Encoding.UTF8.GetBytes("8.0.100"));
    /// Assert.True(output.IsOk);
    /// Assert.Equal("8.0.100", output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output(
        string fileName,
        int exitCode,
        byte[]? stdout = null,
        byte[]? stderr = null,
        DateTime? startTime = null,
        DateTime? exitTime = null)
    {
        this.FileName = fileName;
        this.ExitCode = exitCode;
        this.Stdout = stdout ?? [];
        this.Stderr = stderr ?? [];
        this.StartTime = startTime ?? DateTime.MinValue;
        this.ExitTime = exitTime ?? DateTime.MinValue;
    }

    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    /// <value>The exit code returned by the process.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Output(0, "test", [], [], null, null);
    /// Assert.Equal(0, output.ExitCode);
    /// </code>
    /// </example>
    /// </remarks>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the file name or path of the executable that was run.
    /// </summary>
    /// <value>The name or path of the executable.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Output(0, "dotnet", [], [], null, null);
    /// Assert.Equal("dotnet", output.FileName);
    /// </code>
    /// </example>
    /// </remarks>
    public string FileName { get; }

    /// <summary>
    /// Gets the standard output bytes captured from the process.
    /// </summary>
    /// <value>A byte array containing the captured stdout.</value>
    public byte[] Stdout { get; }

    /// <summary>
    /// Gets the standard error bytes captured from the process.
    /// </summary>
    /// <value>A byte array containing the captured stderr.</value>
    public byte[] Stderr { get; }

    /// <summary>
    /// Gets the time the process started.
    /// </summary>
    /// <value>The start time of the process.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startTime = DateTime.Now;
    /// var output = new Output(0, "test", [], [], startTime, null);
    /// Assert.Equal(startTime, output.StartTime);
    /// </code>
    /// </example>
    /// </remarks>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the time the process exited.
    /// </summary>
    /// <value>The exit time of the process.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exitTime = DateTime.Now;
    /// var output = new Output(0, "test", [], [], null, exitTime);
    /// Assert.Equal(exitTime, output.ExitTime);
    /// </code>
    /// </example>
    /// </remarks>
    public DateTime ExitTime { get; }

    /// <summary>
    /// Gets or initializes the exception that occurred during execution, if any.
    /// </summary>
    /// <value>An exception that occurred during process execution, or <c>null</c> if no error occurred.</value>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0 and no error).
    /// </summary>
    /// <value><c>true</c> if the process completed successfully; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--version"]).Output();
    /// if (output.IsOk)
    /// {
    ///     Console.WriteLine("Command succeeded");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsOk => this.ExitCode == 0 && this.Error is null;

    /// <summary>
    /// Gets a value indicating whether the process failed (non-zero exit code or has an error).
    /// </summary>
    /// <value><c>true</c> if the process failed; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["invalid-command"]).Output();
    /// if (output.IsError)
    /// {
    ///     Console.WriteLine($"Command failed with exit code {output.ExitCode}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsError => this.ExitCode != 0 || this.Error is not null;

    /// <summary>
    /// Throws a <see cref="ProcessException"/> if the process exited with a non-zero exit code or had an error.
    /// </summary>
    /// <returns>This <see cref="Output"/> instance if the process succeeded.</returns>
    /// <exception cref="ProcessException">Thrown when the exit code is non-zero or an error occurred.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--version"])
    ///     .Output()
    ///     .ThrowOnBadExit();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output ThrowOnBadExit()
    {
        if (this.ExitCode != 0)
        {
            throw new ProcessException(
                this.ExitCode,
                this.FileName,
                $"Process '{this.FileName}' failed with exit code {this.ExitCode}.",
                this.Error);
        }

        if (this.Error is not null)
        {
            throw new ProcessException(
                this.ExitCode,
                this.FileName,
                $"Process '{this.FileName}' failed with error: {this.Error.Message}",
                this.Error);
        }

        return this;
    }

    /// <summary>
    /// Throws a <see cref="ProcessException"/> if the custom validation function returns <c>false</c>.
    /// </summary>
    /// <param name="isValid">A function that determines if the exit code and error combination is valid.</param>
    /// <returns>This <see cref="Output"/> instance if the validation passed.</returns>
    /// <exception cref="ProcessException">Thrown when the validation function returns <c>false</c>.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["git", "status"])
    ///     .Output()
    ///     .ThrowOnBadExit((exitCode, error) => exitCode == 0 || exitCode == 1);
    /// </code>
    /// </example>
    /// </remarks>
    public Output ThrowOnBadExit(Func<int, Exception?, bool> isValid)
    {
        if (isValid(this.ExitCode, this.Error))
        {
            return this;
        }

        throw new ProcessException(
            this.ExitCode,
            this.FileName,
            $"Process '{this.FileName}' failed with exit code {this.ExitCode}.",
            this.Error);
    }

    /// <summary>
    /// Gets the standard output as a string using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8 without BOM.</param>
    /// <returns>The standard output as a string, or <see cref="string.Empty"/> if no output was captured.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--version"]).Output();
    /// var version = output.Text().Trim();
    /// Console.WriteLine($"Dotnet version: {version}");
    /// </code>
    /// </example>
    /// </remarks>
    public string Text(Encoding? encoding = null)
    {
        encoding ??= System.Text.Encoding.UTF8NoBom;

        return this.Stdout.Length > 0
            ? encoding.GetString(this.Stdout)
            : string.Empty;
    }

    /// <summary>
    /// Gets the standard error as a string using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8 without BOM.</param>
    /// <returns>The standard error as a string, or <see cref="string.Empty"/> if no error output was captured.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "build", "nonexistent.csproj"]).Output();
    /// if (output.IsError)
    /// {
    ///     Console.WriteLine($"Build errors: {output.ErrorText()}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public string ErrorText(Encoding? encoding = null)
    {
        encoding ??= System.Text.Encoding.UTF8NoBom;

        return this.Stderr.Length > 0
            ? encoding.GetString(this.Stderr)
            : string.Empty;
    }

    /// <summary>
    /// Gets the standard output as a sequence of lines using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8 without BOM.</param>
    /// <returns>An enumerable of lines from the standard output.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "--list-sdks"]).Output();
    /// foreach (var line in output.Lines())
    /// {
    ///     Console.WriteLine($"SDK: {line}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public IEnumerable<string> Lines(Encoding? encoding = null)
    {
        encoding ??= System.Text.Encoding.UTF8NoBom;
        using var stream = new MemoryStream(this.Stdout);
        using var reader = new StreamReader(stream, encoding);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            yield return line;
        }

        yield break;
    }

    /// <summary>
    /// Gets the standard error as a sequence of lines using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8 without BOM.</param>
    /// <returns>An enumerable of lines from the standard error.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var output = new Command(["dotnet", "build", "myapp.csproj"]).Output();
    /// foreach (var line in output.ErrorLines())
    /// {
    ///     Console.WriteLine($"Error: {line}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public IEnumerable<string> ErrorLines(Encoding? encoding = null)
    {
        encoding ??= System.Text.Encoding.UTF8NoBom;
        using var stream = new MemoryStream(this.Stderr);
        using var reader = new StreamReader(stream, encoding);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            yield return line;
        }

        yield break;
    }
}