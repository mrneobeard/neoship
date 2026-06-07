namespace NeoBeard.Exec;

/// <summary>
/// Represents Deno command options for executing Deno scripts or
/// commands. It inherits from <see cref="ShellCommandOptions"/>
/// and <see cref="CommandOptions"/>.
/// </summary>
public sealed class DenoCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenoCommandOptions"/> class with default settings for executing Deno commands.
    /// </summary>
    public DenoCommandOptions()
    {
        this.File = "deno";
        this.FileExtensions = [".ts", ".js", ".mjs", ".tsx", ".jsx"];
    }

    /// <summary>
    /// Finalizes the command arguments based on the provided script and options. It determines whether the script is a file path or inline code and constructs the appropriate arguments for execution.
    /// </summary>
    /// <returns>>A list of command arguments to execute the Deno script or command.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the script is not set.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new DenoCommandOptions { Script = "console.log('test')" };
    /// var args = options.ToStartInfo().Arguments;
    /// Assert.Contains("eval", args);
    /// </code>
    /// </example>
    /// </remarks>
    protected override CommandArgs FinalizeArgs()
    {
        if (this.Script.IsNullOrWhiteSpace())
        {
            throw new InvalidOperationException("Script must be set to get script arguments.");
        }

        var script = this.Script.Trim();
        var isFilePath = !script.Any(o => o is '\n' or '\r') &&
                          this.FileExtensions.Any(ext => script.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        var args = this.ScriptArgs.ToList();

        if (isFilePath || this.UseScriptAsFile)
        {
            string file = string.Empty;
            if (!isFilePath)
            {
                file = this.GenerateDisposableFile(this.Script);
            }
            else
            {
                file = script;
            }

            if (args.Count == 0)
            {
                args.AddRange(["run", "--allow-all", "--allow-scripts"]);
            }

            args.Add(file);
            return [.. args];
        }

        if (args.Count == 0)
        {
            args.AddRange(["eval", "--allow-scripts"]);
        }

        args.Add(this.Script);
        return [.. args];
    }
}