using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace NeoBeard.Exec;

/// <summary>
/// Provides configuration options for executing a command.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new CommandOptions
/// {
///     File = "dotnet",
///     Args = ["--version"],
///     Cwd = "/home/user/project",
///     Stdout = Stdio.Piped,
///     Stderr = Stdio.Piped
/// };
///
/// using var process = new ChildProcess(options);
/// var output = process.WaitForOutput();
/// Console.WriteLine(output.Text());
/// </code>
/// </example>
/// </remarks>
public class CommandOptions
{
    /// <summary>
    /// Gets or sets an action to modify the <see cref="ProcessStartInfo"/> before the process starts.
    /// </summary>
    /// <value>An action that can modify the process start information.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions
    /// {
    ///     WriteCommand = info => info.WindowStyle = ProcessWindowStyle.Hidden
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public Action<ProcessStartInfo>? WriteCommand { get; set; }

    /// <summary>
    /// Gets or sets the executable file name or path.
    /// </summary>
    /// <value>The name or path of the executable to run.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { File = "dotnet" };
    /// Assert.Equal("dotnet", options.File);
    /// </code>
    /// </example>
    /// </remarks>
    public string File { get; internal protected set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments to pass to the executable.
    /// </summary>
    /// <value>A list of arguments for the executable.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { Args = ["--version"] };
    /// Assert.Single(options.Args);
    /// </code>
    /// </example>
    /// </remarks>
    public CommandArgs Args { get; internal protected set; } = [];

    /// <summary>
    /// Gets or sets the working directory for the process.
    /// </summary>
    /// <value>The working directory path, or <c>null</c> to use the current directory.</value>
    public string? Cwd { get; set; }

    /// <summary>
    /// Gets or sets environment variables to set for the process.
    /// </summary>
    /// <value>A dictionary of environment variables, or <c>null</c> to inherit the current environment.</value>
    public IDictionary<string, string?>? Env { get; set; }

    /// <summary>
    /// Gets or sets the list of disposable objects to be disposed when the process is disposed.
    /// </summary>
    /// <value>A list of <see cref="IDisposable"/> objects.</value>
    public List<IDisposable> Disposables { get; internal protected set; } = [];

    /// <summary>
    /// Gets or sets how standard output should be handled.
    /// </summary>
    /// <value>The <see cref="Stdio"/> configuration for standard output.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { Stdout = Stdio.Piped };
    /// Assert.Equal(Stdio.Piped, options.Stdout);
    /// </code>
    /// </example>
    /// </remarks>
    public Stdio Stdout { get; set; }

    /// <summary>
    /// Gets or sets how standard error should be handled.
    /// </summary>
    /// <value>The <see cref="Stdio"/> configuration for standard error.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { Stderr = Stdio.Piped };
    /// Assert.Equal(Stdio.Piped, options.Stderr);
    /// </code>
    /// </example>
    /// </remarks>
    public Stdio Stderr { get; set; }

    /// <summary>
    /// Gets or sets how standard input should be handled.
    /// </summary>
    /// <value>The <see cref="Stdio"/> configuration for standard input.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { Stdin = Stdio.Piped };
    /// Assert.Equal(Stdio.Piped, options.Stdin);
    /// </code>
    /// </example>
    /// </remarks>
    public Stdio Stdin { get; set; }

    /// <summary>
    /// Gets or sets the user name to use when starting the process.
    /// </summary>
    /// <value>The user name, or <c>null</c> to use the current user.</value>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the verb to use when starting the process (e.g., "runas" for elevated privileges).
    /// </summary>
    /// <value>The verb for process start, or <c>null</c> for default behavior.</value>
    public string? Verb { get; set; }

    /// <summary>
    /// Gets or sets the secure password for the user account.
    /// </summary>
    /// <value>A secure string containing the password, or <c>null</c>.</value>
    [SupportedOSPlatform("windows")]
    public SecureString? Password { get; set; }

    /// <summary>
    /// Gets or sets the password in clear text for the user account.
    /// </summary>
    /// <value>The password as plain text, or <c>null</c>.</value>
    [SupportedOSPlatform("windows")]
    public string? PasswordInClearText { get; set; }

    /// <summary>
    /// Gets or sets the domain for the user account.
    /// </summary>
    /// <value>The domain name, or <c>null</c>.</value>
    [SupportedOSPlatform("windows")]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to load the user profile.
    /// </summary>
    /// <value><c>true</c> to load the user profile; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { LoadUserProfile = true };
    /// Assert.True(options.LoadUserProfile);
    /// </code>
    /// </example>
    /// </remarks>
    public bool LoadUserProfile { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to create a window for the process.
    /// </summary>
    /// <value><c>true</c> to not create a window; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { CreateNoWindow = true };
    /// Assert.True(options.CreateNoWindow);
    /// </code>
    /// </example>
    /// </remarks>
    public bool CreateNoWindow { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use the operating system shell to start the process.
    /// </summary>
    /// <value><c>true</c> to use the shell; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions { UseShellExecute = true };
    /// Assert.True(options.UseShellExecute);
    /// </code>
    /// </example>
    /// </remarks>
    public bool UseShellExecute { get; set; } = false;

    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> from these options.
    /// </summary>
    /// <returns>A new <see cref="ProcessStartInfo"/> configured with these options.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions
    /// {
    ///     File = "dotnet",
    ///     Args = ["--version"],
    ///     Stdout = Stdio.Piped
    /// };
    ///
    /// var startInfo = options.ToStartInfo();
    /// Assert.Equal("dotnet", startInfo.FileName);
    /// Assert.True(startInfo.RedirectStandardOutput);
    /// </code>
    /// </example>
    /// </remarks>
    public virtual ProcessStartInfo ToStartInfo()
    {
        var si = new ProcessStartInfo();
        return this.ToStartInfo(si);
    }

    /// <summary>
    /// Configures an existing <see cref="ProcessStartInfo"/> from these options.
    /// </summary>
    /// <param name="startInfo">The <see cref="ProcessStartInfo"/> to configure.</param>
    /// <returns>The configured <see cref="ProcessStartInfo"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new CommandOptions
    /// {
    ///     File = "dotnet",
    ///     Args = ["build"],
    ///     Cwd = "/src/myproject"
    /// };
    ///
    /// var startInfo = new ProcessStartInfo();
    /// options.ToStartInfo(startInfo);
    /// Assert.Equal("/src/myproject", startInfo.WorkingDirectory);
    /// </code>
    /// </example>
    /// </remarks>
    public virtual ProcessStartInfo ToStartInfo(ProcessStartInfo startInfo)
    {
        var si = startInfo;
        si.FileName = this.File;
        var exe = PathFinder.Default.Find(this.File);
        if (!exe.IsNullOrWhiteSpace())
        {
            si.FileName = exe;
        }

        si.CreateNoWindow = this.CreateNoWindow;
        si.UseShellExecute = this.UseShellExecute;
        si.RedirectStandardInput = this.Stdin == Stdio.Piped;

        var windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (windows)
            si.LoadUserProfile = startInfo.LoadUserProfile;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !this.User.IsNullOrWhiteSpace())
        {
            si.UserName = this.User;

            if (startInfo.Password is not null)
            {
                si.Password = startInfo.Password;
            }
            else if (!startInfo.PasswordInClearText.IsNullOrWhiteSpace())
            {
                si.PasswordInClearText = startInfo.PasswordInClearText;
            }

            if (!startInfo.Domain.IsNullOrWhiteSpace())
            {
                si.Domain = startInfo.Domain;
            }
        }

        var args = this.FinalizeArgs();

#if NET5_0_OR_GREATER
        foreach (var arg in args)
        {
            si.ArgumentList.Add(arg);
        }
#else
        si.Arguments = args.ToString();
#endif

        if (!this.Cwd.IsNullOrWhiteSpace())
            si.WorkingDirectory = this.Cwd;

        if (this.Env is not null)
        {
            foreach (var kvp in this.Env)
            {
                si.Environment[kvp.Key] = kvp.Value;
            }
        }

        si.RedirectStandardOutput = this.Stdout != Stdio.Inherit;
        si.RedirectStandardError = this.Stderr != Stdio.Inherit;
        si.RedirectStandardInput = this.Stdin != Stdio.Inherit;
        if (si.RedirectStandardError || si.RedirectStandardOutput)
        {
            si.CreateNoWindow = true;
            si.UseShellExecute = false;
        }

        return si;
    }

    /// <summary>
    /// Finalizes the arguments list for the command.
    /// </summary>
    /// <returns>The finalized list of arguments.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// public class CustomOptions : CommandOptions
    /// {
    ///     protected override CommandArgs FinalizeArgs()
    ///     {
    ///         var args = base.FinalizeArgs();
    ///         args.Insert(0, "--custom-flag");
    ///         return args;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected virtual CommandArgs FinalizeArgs()
        => this.Args;
}