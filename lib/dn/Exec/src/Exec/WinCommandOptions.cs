namespace NeoBeard.Exec;

/// <summary>
/// Represents win cmd options for executing cmd commands. It inherits from <see cref="ShellCommandOptions"/>
/// and <see cref="CommandOptions"/>.
/// </summary>
public sealed class WinCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WinCommandOptions"/> class with default
    /// settings for executing cmd commands.
    /// </summary>
    public WinCommandOptions()
    {
        this.File = "cmd.exe";
        this.FileExtensions = [".cmd", ".bat"];
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
            args.AddRange(["/D", "/E:ON", "/V:OFF", "/S", "/C"]);
        }

        if (!isFilePath)
        {
            script = $@"
@echo off
{script}
";
            var filePath = this.GenerateDisposableFile(script);
            args.AddRange(["CALL", filePath]);
        }
        else
        {
            args.AddRange(["CALL", this.Script]);
        }

        return [.. args];
    }
}