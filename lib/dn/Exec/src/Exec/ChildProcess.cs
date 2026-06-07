using System.Diagnostics;
using System.Text;

namespace NeoBeard.Exec;

/// <summary>
/// Represents a child process that can be managed, piped, and disposed.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var options = new CommandOptions
/// {
///     File = "dotnet",
///     Args = ["--version"],
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
public sealed class ChildProcess : IDisposable
{
    private readonly Process process;

    private readonly int processId;

    private readonly List<IDisposable> disposables = new();

    private bool disposed;

    private DateTime exitTime;

    private byte[] stdout = Array.Empty<byte>();

    private byte[] stderr = Array.Empty<byte>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildProcess"/> class from a <see cref="ProcessStartInfo"/>.
    /// </summary>
    /// <param name="startInfo">The process start information.</param>
    /// <exception cref="ArgumentNullException">Thrown when startInfo is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo
    /// {
    ///     FileName = "dotnet",
    ///     Arguments = "--version",
    ///     RedirectStandardOutput = true,
    ///     RedirectStandardError = true
    /// };
    ///
    /// using var process = new ChildProcess(startInfo);
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess(ProcessStartInfo startInfo)
    {
        if (startInfo == null)
            throw new ArgumentNullException(nameof(startInfo));

        this.process = new Process { StartInfo = startInfo };
        Command.GlobalWriteCommand?.Invoke(this.process.StartInfo);
        this.process.Start();
        this.processId = this.process.Id;
        this.exitTime = DateTime.MinValue;

        try
        {
            this.StartTime = this.process.StartTime;
        }
        catch (Exception e)
        {
            this.StartTime = DateTime.Now;
            Debug.WriteLine(e);
        }

        this.processId = this.process.Id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildProcess"/> class from an existing <see cref="Process"/>.
    /// </summary>
    /// <param name="process">An existing process that has not exited.</param>
    /// <exception cref="ArgumentNullException">Thrown when process is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process has already exited.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "--version")
    /// {
    ///     RedirectStandardOutput = true
    /// };
    /// var proc = Process.Start(startInfo);
    /// using var child = new ChildProcess(proc!);
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess(Process process)
    {
        this.process = process ?? throw new ArgumentNullException(nameof(process));
        if (this.process.HasExited)
            throw new InvalidOperationException("Cannot create a ChildProcess from an already exited process.");

        this.processId = this.process.Id;
        this.exitTime = DateTime.MinValue;
        Command.GlobalWriteCommand?.Invoke(this.process.StartInfo);
        if (this.process.Threads.Count == 0)
            this.process.Start();

        try
        {
            this.StartTime = this.process.StartTime;
        }
        catch (Exception e)
        {
            this.StartTime = DateTime.Now;
            Debug.WriteLine(e);
        }

        this.processId = this.process.Id;
    }

    internal ChildProcess(CommandOptions options)
    {
        this.process = new Process();
        options.ToStartInfo(this.process.StartInfo);

        var cmd = options.WriteCommand ?? Command.GlobalWriteCommand;
        cmd?.Invoke(this.process.StartInfo);

        this.exitTime = DateTime.MinValue;
        this.process.Start();
        this.processId = this.process.Id;
        var now = DateTime.Now;
        try
        {
            this.StartTime = this.process.StartTime;
        }
        catch (Exception e)
        {
            this.StartTime = now;
            Debug.WriteLine(e);
        }

        this.processId = this.process.Id;
    }

    internal ChildProcess(ShellCommandOptions options)
    {
        this.process = new Process();
        options.ToStartInfo(this.process.StartInfo);

        var cmd = options.WriteCommand ?? Command.GlobalWriteCommand;
        cmd?.Invoke(this.process.StartInfo);

        this.exitTime = DateTime.MinValue;
        this.process.Start();
        this.processId = this.process.Id;
        var now = DateTime.Now;
        try
        {
            this.StartTime = this.process.StartTime;
        }
        catch (Exception e)
        {
            this.StartTime = now;
            Debug.WriteLine(e);
        }

        this.processId = this.process.Id;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ChildProcess"/> class.
    /// </summary>
    ~ChildProcess()
    {
        this.Dispose(false);
    }

    /// <summary>
    /// Gets the process identifier.
    /// </summary>
    /// <value>The unique identifier for the process.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new ChildProcess(new ProcessStartInfo("dotnet", "--version"));
    /// Console.WriteLine($"Process ID: {process.Id}");
    /// </code>
    /// </example>
    /// </remarks>
    public int Id => this.processId;

    /// <summary>
    /// Gets the time the process started.
    /// </summary>
    /// <value>The start time of the process.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new ChildProcess(new ProcessStartInfo("dotnet", "--version"));
    /// Console.WriteLine($"Started at: {process.StartTime}");
    /// </code>
    /// </example>
    /// </remarks>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the time the process exited.
    /// </summary>
    /// <value>The exit time of the process, or <see cref="DateTime.MinValue"/> if not exited.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new ChildProcess(new ProcessStartInfo("dotnet", "--version"));
    /// process.Wait();
    /// Console.WriteLine($"Exited at: {process.ExitTime}");
    /// </code>
    /// </example>
    /// </remarks>
    public DateTime ExitTime => this.exitTime;

    /// <summary>
    /// Gets the standard error stream.
    /// </summary>
    /// <value>The base stream for standard error.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "build")
    /// {
    ///     RedirectStandardError = true
    /// };
    /// using var process = new ChildProcess(startInfo);
    /// </code>
    /// </example>
    /// </remarks>
    public Stream Stderr => this.process.StandardError.BaseStream;

    /// <summary>
    /// Gets the standard error reader.
    /// </summary>
    /// <value>A <see cref="StreamReader"/> for standard error.</value>
    public StreamReader StderrReader => this.process.StandardError;

    /// <summary>
    /// Gets the standard input stream.
    /// </summary>
    /// <value>The base stream for standard input.</value>
    public Stream Stdin => this.process.StandardInput.BaseStream;

    /// <summary>
    /// Gets the standard input writer.
    /// </summary>
    /// <value>A <see cref="StreamWriter"/> for standard input.</value>
    public StreamWriter StdinWriter => this.process.StandardInput;

    /// <summary>
    /// Gets the standard output stream.
    /// </summary>
    /// <value>The base stream for standard output.</value>
    public Stream Stdout => this.process.StandardOutput.BaseStream;

    /// <summary>
    /// Gets the standard output reader.
    /// </summary>
    /// <value>A <see cref="StreamReader"/> for standard output.</value>
    public StreamReader StdoutReader => this.process.StandardOutput;

    /// <summary>
    /// Gets a value indicating whether standard output is redirected.
    /// </summary>
    /// <value><c>true</c> if standard output is redirected; otherwise, <c>false</c>.</value>
    public bool IsStdoutRedirected => this.process.StartInfo.RedirectStandardOutput;

    /// <summary>
    /// Gets a value indicating whether standard output has been piped.
    /// </summary>
    /// <value><c>true</c> if standard output has been piped; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardOutput = true });
    /// Assert.False(child.IsStdoutPiped);
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsStdoutPiped { get; private set; }

    /// <summary>
    /// Gets a value indicating whether standard error is redirected.
    /// </summary>
    /// <value><c>true</c> if standard error is redirected; otherwise, <c>false</c>.</value>
    public bool IsStderrRedirected => this.process.StartInfo.RedirectStandardError;

    /// <summary>
    /// Gets a value indicating whether standard error has been piped.
    /// </summary>
    /// <value><c>true</c> if standard error has been piped; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardError = true });
    /// Assert.False(child.IsStderrPiped);
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsStderrPiped { get; private set; }

    /// <summary>
    /// Gets a value indicating whether standard input is redirected.
    /// </summary>
    /// <value><c>true</c> if standard input is redirected; otherwise, <c>false</c>.</value>
    public bool IsStdinRedirected => this.process.StartInfo.RedirectStandardInput;

    /// <summary>
    /// Converts a <see cref="ChildProcess"/> to a <see cref="Process"/>.
    /// </summary>
    /// <param name="child">The child process to convert.</param>
    /// <returns>The underlying <see cref="Process"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet"));
    /// Process proc = child;
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator Process(ChildProcess child)
    {
        return child.process;
    }

    /// <summary>
    /// Adds a disposable object to be disposed when the process is disposed.
    /// </summary>
    /// <param name="disposable">The disposable object to add.</param>
    /// <returns>This <see cref="ChildProcess"/> instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tempFile = new DisposableFile("/tmp/myfile.txt");
    /// using var process = new ChildProcess(new ProcessStartInfo("cat", "/tmp/myfile.txt"))
    ///     .AddDisposable(tempFile);
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess AddDisposable(IDisposable disposable)
    {
        this.disposables.Add(disposable);
        return this;
    }

    /// <summary>
    /// Adds multiple disposable objects to be disposed when the process is disposed.
    /// </summary>
    /// <param name="disposables">The disposable objects to add.</param>
    /// <returns>This <see cref="ChildProcess"/> instance for method chaining.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var disposables = new List&lt;IDisposable&gt; { new DisposableFile("/tmp/a.txt"), new DisposableFile("/tmp/b.txt") };
    /// using var process = new ChildProcess(new ProcessStartInfo("cat"))
    ///     .AddDisposables(disposables);
    /// </code>
    /// </example>
    /// </remarks>
    public ChildProcess AddDisposables(IEnumerable<IDisposable> disposables)
    {
        this.disposables.AddRange(disposables);
        return this;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ChildProcess"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new ChildProcess(new ProcessStartInfo("dotnet"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Kills the process immediately.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var process = new ChildProcess(new ProcessStartInfo("sleep", "100"));
    /// process.Kill();
    /// </code>
    /// </example>
    /// </remarks>
    public void Kill()
    {
        this.process.Kill();
    }

    /// <summary>
    /// Pipes the standard output to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var ms = new MemoryStream();
    /// process.PipeTo(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeTo(Stream stream)
    {
        this.GuardPiped();
        this.process.StandardOutput.BaseStream.CopyTo(stream);
    }

    /// <summary>
    /// Pipes the standard output to a text writer.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var writer = new StringWriter();
    /// process.PipeTo(writer);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeTo(TextWriter writer)
    {
        this.GuardPiped();
        this.process.StandardOutput.PipeTo(writer);
    }

    /// <summary>
    /// Pipes the standard output to a collection of lines.
    /// </summary>
    /// <param name="lines">The collection to add lines to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// var lines = new List&lt;string&gt;();
    /// process.PipeTo(lines);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeTo(ICollection<string> lines)
    {
        this.GuardPiped();
        this.process.StandardOutput.PipeTo(lines);
    }

    /// <summary>
    /// Pipes the standard output to a file.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// process.PipeTo(new FileInfo("/tmp/output.txt"));
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeTo(FileInfo file)
    {
        this.GuardPiped();
        this.process.StandardOutput.PipeTo(file);
    }

    /// <summary>
    /// Pipes the standard output to another child process's standard input.
    /// </summary>
    /// <param name="child">The child process to pipe to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected/already piped, or child stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo1 = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// var startInfo2 = new ProcessStartInfo("cat") { RedirectStandardInput = true, RedirectStandardOutput = true };
    /// using var p1 = new ChildProcess(startInfo1);
    /// using var p2 = new ChildProcess(startInfo2);
    /// p1.PipeTo(p2);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeTo(ChildProcess child)
    {
        this.GuardPiped();

        if (!child.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe to child proccess' input when child process input is not redirected.");

        this.process.StandardOutput.BaseStream.CopyTo(child.process.StandardInput.BaseStream);
        child.process.StandardInput.BaseStream.Close();
    }

    /// <summary>
    /// Asynchronously pipes the standard output to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="bufferSize">The buffer size. Defaults to 4096.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var ms = new MemoryStream();
    /// await process.PipeToAsync(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public Task PipeToAsync(Stream stream, int bufferSize = -1, CancellationToken cancellationToken = default)
    {
        this.GuardPiped();

        if (bufferSize < 1)
            bufferSize = 4096;

        return this.process.StandardOutput.BaseStream.CopyToAsync(stream, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the standard output to a text writer.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="bufferSize">The buffer size. Defaults to 4096.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardOutput = true });
    /// using var writer = new StringWriter();
    /// await child.PipeToAsync(writer);
    /// </code>
    /// </example>
    /// </remarks>
    public Task PipeToAsync(TextWriter writer, int bufferSize = -1, CancellationToken cancellationToken = default)
    {
        this.GuardPiped();
        return this.process.StandardOutput.PipeToAsync(writer, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the standard output to a collection of lines.
    /// </summary>
    /// <param name="lines">The collection to add lines to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardOutput = true });
    /// var lines = new List&lt;string&gt;();
    /// await child.PipeToAsync(lines);
    /// </code>
    /// </example>
    /// </remarks>
    public Task PipeToAsync(ICollection<string> lines, CancellationToken cancellationToken = default)
    {
        this.GuardPiped();
        return this.process.StandardOutput.PipeToAsync(lines, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the standard output to a file.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="bufferSize">The buffer size. Defaults to 4096.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardOutput = true });
    /// var file = new FileInfo("output.txt");
    /// await child.PipeToAsync(file, Encoding.UTF8);
    /// </code>
    /// </example>
    /// </remarks>
    public Task PipeToAsync(FileInfo file, Encoding? encoding, int bufferSize = -1, CancellationToken cancellationToken = default)
    {
        this.GuardPiped();
        return this.process.StandardOutput.PipeToAsync(file, encoding, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the standard output to another child process's standard input.
    /// </summary>
    /// <param name="child">The child process to pipe to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when stdout is not redirected/already piped, or child stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo1 = new ProcessStartInfo("echo", "hello") { RedirectStandardOutput = true };
    /// var startInfo2 = new ProcessStartInfo("cat") { RedirectStandardInput = true };
    /// using var p1 = new ChildProcess(startInfo1);
    /// using var p2 = new ChildProcess(startInfo2);
    /// await p1.PipeToAsync(p2);
    /// </code>
    /// </example>
    /// </remarks>
    public async Task PipeToAsync(ChildProcess child, CancellationToken cancellationToken = default)
    {
        this.GuardPiped();

        if (!child.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe to child's input when child input is not redirected.");

#if NETLEGACY
        await this.process.StandardOutput.BaseStream.CopyToAsync(child.process.StandardInput.BaseStream)
            .ConfigureAwait(false);
        child.process.StandardInput.BaseStream.Close();
#else
        await this.process.StandardOutput.BaseStream.CopyToAsync(child.process.StandardInput.BaseStream, cancellationToken)
            .ConfigureAwait(false);
        child.process.StandardInput.BaseStream.Close();
#endif
    }

    /// <summary>
    /// Pipes the standard error to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stderr is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("git", "status") { RedirectStandardError = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var ms = new MemoryStream();
    /// process.PipeErrorTo(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeErrorTo(Stream stream)
    {
        this.GuardErrorPiped();
        this.process.StandardError.BaseStream.CopyTo(stream);
    }

    /// <summary>
    /// Pipes the standard error to a text writer.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stderr is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardError = true });
    /// using var writer = new StringWriter();
    /// child.PipeErrorTo(writer);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeErrorTo(TextWriter writer)
    {
        this.GuardErrorPiped();
        this.process.StandardError.PipeTo(writer);
    }

    /// <summary>
    /// Pipes the standard error to a collection of lines.
    /// </summary>
    /// <param name="lines">The collection to add lines to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stderr is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardError = true });
    /// var lines = new List&lt;string&gt;();
    /// child.PipeErrorTo(lines);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeErrorTo(ICollection<string> lines)
    {
        this.GuardErrorPiped();
        this.process.StandardError.PipeTo(lines);
    }

    /// <summary>
    /// Pipes the standard error to a file.
    /// </summary>
    /// <param name="file">The file to write to.</param>
    /// <exception cref="InvalidOperationException">Thrown when stderr is not redirected or already piped.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var child = new ChildProcess(new ProcessStartInfo("dotnet") { RedirectStandardError = true });
    /// var file = new FileInfo("errors.txt");
    /// child.PipeErrorTo(file);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeErrorTo(FileInfo file)
    {
        this.GuardErrorPiped();
        this.process.StandardError.PipeTo(file);
    }

    /// <summary>
    /// Pipes from a collection of lines to the process's standard input.
    /// </summary>
    /// <param name="lines">The lines to write to stdin.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("cat") { RedirectStandardInput = true, RedirectStandardOutput = true };
    /// using var process = new ChildProcess(startInfo);
    /// process.PipeFrom(new[] { "line1", "line2" });
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeFrom(ICollection<string> lines)
    {
        if (!this.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe stdin from stream when input is not redirected.");

        this.process.StandardInput.Write(lines);
    }

    /// <summary>
    /// Pipes from a file to the process's standard input.
    /// </summary>
    /// <param name="file">The file to read from.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("cat") { RedirectStandardInput = true };
    /// using var process = new ChildProcess(startInfo);
    /// process.PipeFrom(new FileInfo("/tmp/input.txt"));
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeFrom(FileInfo file)
    {
        if (!this.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe stdin from stream when input is not redirected.");

        this.process.StandardInput.Write(file);
    }

    /// <summary>
    /// Pipes from a stream to the process's standard input.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("cat") { RedirectStandardInput = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
    /// process.PipeFrom(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeFrom(Stream stream)
    {
        if (!this.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe stdin from stream when input is not redirected.");

        this.process.StandardInput.Write(stream);
    }

    /// <summary>
    /// Pipes from a text reader to the process's standard input.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <exception cref="InvalidOperationException">Thrown when stdin is not redirected.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("cat") { RedirectStandardInput = true };
    /// using var process = new ChildProcess(startInfo);
    /// using var reader = new StringReader("hello world");
    /// process.PipeFrom(reader);
    /// </code>
    /// </example>
    /// </remarks>
    public void PipeFrom(TextReader reader)
    {
        if (!this.IsStdinRedirected)
            throw new InvalidOperationException("Cannot pipe stdin from stream when input is not redirected.");

        this.process.StandardInput.Write(reader);
    }

    /// <summary>
    /// Waits for the process to exit and returns the exit code.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "--version");
    /// using var process = new ChildProcess(startInfo);
    /// var exitCode = process.Wait();
    /// Assert.Equal(0, exitCode);
    /// </code>
    /// </example>
    /// </remarks>
    public int Wait()
    {
        if (this.process.HasExited)
        {
            this.exitTime = this.process.ExitTime;
            return this.process.ExitCode;
        }

        this.process.WaitForExit();
        this.exitTime = this.process.ExitTime;
        return this.process.ExitCode;
    }

    /// <summary>
    /// Waits for the process to exit and returns the captured output.
    /// </summary>
    /// <returns>An <see cref="Output"/> containing the process results.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "--version")
    /// {
    ///     RedirectStandardOutput = true,
    ///     RedirectStandardError = true
    /// };
    /// using var process = new ChildProcess(startInfo);
    /// var output = process.WaitForOutput();
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public Output WaitForOutput()
    {
        try
        {
            if (this.IsStdoutRedirected && !this.IsStdoutPiped)
            {
                using var ms = new MemoryStream();
                this.process.StandardOutput.PipeTo(ms);
                ms.Flush();
                this.stdout = ms.ToArray();
            }

            if (this.IsStderrRedirected && !this.IsStderrPiped)
            {
                using var ms = new MemoryStream();
                this.process.StandardError.PipeTo(ms);
                ms.Flush();
                this.stderr = ms.ToArray();
            }

            if (this.process.HasExited)
            {
                this.exitTime = this.process.ExitTime;
            }
            else
            {
                this.process.WaitForExit();
                this.exitTime = this.process.ExitTime;
            }

            return new Output(
                this.process.StartInfo.FileName,
                this.process.ExitCode,
                this.stdout,
                this.stderr,
                this.StartTime,
                this.exitTime);
        }
        catch (Exception e)
        {
            this.exitTime = DateTime.Now;
            return new Output(
                this.process.StartInfo.FileName,
                -1,
                e,
                this.stdout,
                this.stderr,
                this.StartTime,
                this.exitTime);
        }
    }

    /// <summary>
    /// Asynchronously waits for the process to exit and returns the exit code.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the exit code of the process.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "--version");
    /// using var process = new ChildProcess(startInfo);
    /// var exitCode = await process.WaitAsync(CancellationToken.None);
    /// Assert.Equal(0, exitCode);
    /// </code>
    /// </example>
    /// </remarks>
    public async Task<int> WaitAsync(CancellationToken cancellationToken)
    {
        if (this.process.HasExited)
        {
            this.exitTime = this.process.ExitTime;
            return this.process.ExitCode;
        }

        await this.process.WaitForExitAsync(cancellationToken)
            .ConfigureAwait(false);
        return this.process.ExitCode;
    }

    /// <summary>
    /// Asynchronously waits for the process to exit and returns the captured output.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Output}"/> containing the process results.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var startInfo = new ProcessStartInfo("dotnet", "--version")
    /// {
    ///     RedirectStandardOutput = true,
    ///     RedirectStandardError = true
    /// };
    /// using var process = new ChildProcess(startInfo);
    /// var output = await process.WaitForOutputAsync(CancellationToken.None);
    /// Console.WriteLine(output.Text());
    /// </code>
    /// </example>
    /// </remarks>
    public async ValueTask<Output> WaitForOutputAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (this.IsStdoutRedirected && !this.IsStdoutPiped)
            {
                using var ms = new MemoryStream();
                await this.process.StandardOutput.PipeToAsync(ms, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await ms.FlushAsync(cancellationToken)
                    .ConfigureAwait(false);
                this.stdout = ms.ToArray();
            }

            if (this.IsStderrRedirected && !this.IsStderrPiped)
            {
                using var ms = new MemoryStream();
                await this.process.StandardError
                    .PipeToAsync(ms, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await ms.FlushAsync(cancellationToken)
                    .ConfigureAwait(false);
                this.stderr = ms.ToArray();
            }

            if (this.process.HasExited)
            {
                this.exitTime = this.process.ExitTime;
            }
            else
            {
                await this.process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                this.exitTime = this.process.ExitTime;
            }

            return new Output(
                this.process.StartInfo.FileName,
                this.process.ExitCode,
                this.stdout,
                this.stderr,
                this.StartTime,
                this.exitTime);
        }
        catch (Exception e)
        {
            this.exitTime = DateTime.Now;
            return new Output(
                this.process.StartInfo.FileName,
                -1,
                e,
                this.stdout,
                this.stderr,
                this.StartTime,
                this.exitTime);
        }
    }

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        this.disposed = true;

        this.process?.Dispose();

        if (!disposing || this.disposables.Count == 0)
            return;

        foreach (var disposable in this.disposables)
        {
            disposable.Dispose();
        }
    }

    private void GuardPiped()
    {
        if (this.IsStdoutPiped)
            throw new InvalidOperationException("Cannot pipe stdout. Stdout stream can only be read once.");

        if (!this.IsStdoutRedirected)
            throw new InvalidOperationException("Cannot pipe to stdout when stream is not redirected.");

        this.IsStdoutPiped = true;
    }

    private void GuardErrorPiped()
    {
        if (this.IsStderrPiped)
            throw new InvalidOperationException("Cannot pipe stderr. Stderr stream can only be read once.");

        if (!this.IsStderrRedirected)
            throw new InvalidOperationException("Cannot pipe to stderr when stream is not redirected.");

        this.IsStderrPiped = true;
    }
}