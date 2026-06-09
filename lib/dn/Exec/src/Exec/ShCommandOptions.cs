namespace NeoBeard.Exec;

/// <summary>
/// Options for executing a sh script or command. It inherits from <see cref="ShellCommandOptions"/>
/// and <see cref="CommandOptions"/>.
/// </summary>
public sealed class ShCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShCommandOptions"/> class with
    /// default settings for executing sh commands.
    /// </summary>
    public ShCommandOptions()
    {
        this.File = "sh";
        this.FileExtensions = [".sh"];
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
        if (args.Count == 0)
        {
            args.AddRange(["-e"]);
        }

        if (isFilePath)
        {
            args.Add(script);
            return [.. args];
        }

        if (this.UseScriptAsFile)
        {
            var filePath = this.GenerateDisposableFile(this.Script);
            args.AddRange([filePath]);
        }
        else
        {
            args.AddRange(["-c", this.Script]);
        }

        if (this.Args.Count > 0)
        {
            args.AddRange(this.Args);
        }

        return [.. args];
    }
}