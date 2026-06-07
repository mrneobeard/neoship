namespace NeoBeard.Exec;

/// <summary>
/// Represents Bun command options for executing Bun scripts or commands. It inherits from <see cref="ShellCommandOptions"/>
/// and <see cref="CommandOptions"/>.
/// </summary>
public sealed class BunCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BunCommandOptions"/> class with default settings for executing bun commands.
    /// </summary>
    public BunCommandOptions()
    {
        this.File = "bun";
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
                args.AddRange(["run", "--bun"]);
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