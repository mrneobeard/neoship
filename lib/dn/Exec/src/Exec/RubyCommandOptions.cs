namespace NeoBeard.Exec;

/// <summary>
/// Represents Ruby command options for executing Ruby scripts or commands. It inherits from <see cref="ShellCommandOptions"/>
/// and <see cref="CommandOptions"/>.
/// </summary>
public sealed class RubyCommandOptions : ShellCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RubyCommandOptions"/> class with default
    /// settings for executing Ruby commands.
    /// </summary>
    public RubyCommandOptions()
    {
        this.File = "ruby";
        this.FileExtensions = [".rb", ".rbx"];
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

            args.Add(file);
            return [.. args];
        }

        if (args.Count == 0)
        {
            args.Add("-e");
        }

        args.Add(this.Script);
        return [.. args];
    }
}