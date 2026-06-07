namespace NeoBeard.Exec;

/// <summary>
/// Command options for executing a Node.js script or command. This class inherits
/// from <see cref="ShellCommandOptions"/> and provides default settings for
/// executing Node.js commands, including the default file name and supported
/// file extensions. It also overrides the <see cref="ShellCommandOptions.FinalizeArgs"/>
/// method to construct the appropriate command arguments based on whether the
/// provided script is a file path or inline code.
/// </summary>
public sealed class NodeJsCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeJsCommandOptions"/> class with
    /// default settings for executing Node.js commands.
    /// </summary>
    public NodeJsCommandOptions()
    {
        this.File = "node";
        this.FileExtensions = [".ts", ".js", ".cjs", ".mjs", ".tsx", ".jsx"];
    }

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
                args.AddRange([]);
            }

            args.Add(file);
            return [.. args];
        }

        if (args.Count == 0)
        {
            args.AddRange(["-e"]);
        }

        args.Add(this.Script);
        return [.. args];
    }
}