using NeoBeard.IO;

namespace NeoBeard.Exec;

/// <summary>
/// Provides configuration options for shell command execution.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new ShellCommandOptions
/// {
///     File = "bash",
///     Script = "echo hello world",
///     ScriptArgs = ["arg1", "arg2"],
///     Stdout = Stdio.Piped
/// };
///
/// using var process = new ChildProcess(options);
/// var output = process.WaitForOutput();
/// Console.WriteLine(output.Text());
/// </code>
/// </example>
/// </remarks>
public class ShellCommandOptions : CommandOptions
{
    /// <summary>
    /// Gets or sets the arguments to pass to the script.
    /// </summary>
    /// <value>A list of script arguments.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     Script = "echo $1 $2",
    ///     ScriptArgs = ["hello", "world"]
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public CommandArgs ScriptArgs { get; set; } = [];

    /// <summary>
    /// Gets or sets the valid file extensions for this shell.
    /// </summary>
    /// <value>An array of file extensions.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "powershell",
    ///     FileExtensions = [".ps1", ".psm1"]
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public virtual string[] FileExtensions { get; set; } = [];

    /// <summary>
    /// Gets or sets the script content to execute.
    /// </summary>
    /// <value>The script content as a string.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     Script = "echo hello; echo world"
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public string Script { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to write the script to a temporary file before execution.
    /// </summary>
    /// <value><c>true</c> to run as a file; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     Script = "echo hello",
    ///     UseScriptAsFile = true
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public bool UseScriptAsFile { get; set; } = false;

    /// <summary>
    /// Gets or sets the default file extension for temporary script files.
    /// </summary>
    /// <value>The default extension (e.g., ".sh"), or <c>null</c> to use the first <see cref="FileExtensions"/> entry.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     Script = "echo hello",
    ///     UseScriptAsFile = true,
    ///     DefaultExtension = ".sh"
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Finalizes the arguments list, including the script and script arguments.
    /// </summary>
    /// <returns>The finalized list of arguments.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     Script = "echo $1",
    ///     ScriptArgs = ["hello"]
    /// };
    /// var args = options.FinalizeArgs();
    /// Assert.Contains("hello", args);
    /// Assert.Contains("echo $1", args);
    /// </code>
    /// </example>
    /// </remarks>
    protected override CommandArgs FinalizeArgs()
    {
        var args = this.ScriptArgs.ToList();
        if (!this.Script.IsNullOrWhiteSpace())
        {
            args.Add(this.Script);
        }

        return new CommandArgs(args);
    }

    /// <summary>
    /// Generates a temporary file with the script content.
    /// </summary>
    /// <param name="script">The script content to write to the file.</param>
    /// <param name="extension">The file extension to use, or <c>null</c> to use the default.</param>
    /// <returns>The path to the generated temporary file.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new ShellCommandOptions
    /// {
    ///     File = "bash",
    ///     DefaultExtension = ".sh"
    /// };
    /// var tempPath = options.GenerateDisposableFile("echo hello", ".sh");
    /// Console.WriteLine($"Script file: {tempPath}");
    /// </code>
    /// </example>
    /// </remarks>
    protected string GenerateDisposableFile(string script, string? extension = null)
    {
        var ext = extension ?? this.DefaultExtension ?? this.FileExtensions[0];

        var tmpDir = NeoBeard.Env.Get("NeoBeard_TEMP_DIR") ?? System.IO.Path.GetTempPath();
        var tempFileName = Path.GetRandomFileName();
        var tempFile = Path.Combine(tmpDir, $"{tempFileName}{ext}");
        System.IO.File.WriteAllText(tempFile, script);
        var disposable = new DisposableFile(tempFile, (self) => this.Disposables.Remove(self));
        this.Disposables.Add(disposable);
        return tempFile;
    }
}