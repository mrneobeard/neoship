using System.Text;

namespace NeoBeard.IO;

/// <summary>
/// Represents the TeeTextWriter class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public class TeeTextWriter : TextWriter
{
    private readonly List<(TextWriter writer, bool leaveOpen)> writers;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TeeTextWriter"/> class with writers and leave open flag.
    /// </summary>
    /// <param name="leaveOpen">Whether to leave the writers open after disposing.</param>
    /// <param name="writers">The text writers to write to.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// var tee = new TeeTextWriter(leaveOpen: true, writer1, writer2);
    /// tee.Write('a');
    /// Assert.Equal("a", writer1.ToString());
    /// Assert.Equal("a", writer2.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public TeeTextWriter(bool leaveOpen, params TextWriter[] writers)
    {
        this.writers = new List<(TextWriter writer, bool leaveOpen)>();
        foreach (var writer in writers)
        {
            this.writers.Add((writer, leaveOpen));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeeTextWriter"/> class with writers.
    /// </summary>
    /// <param name="writers">The text writers to write to.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var writer1 = new StringWriter();
    /// using var writer2 = new StringWriter();
    /// using var tee = new TeeTextWriter(writer1, writer2);
    /// tee.Write('a');
    /// </code>
    /// </example>
    /// </remarks>
    public TeeTextWriter(params TextWriter[] writers)
    {
        this.writers = writers?.Select(writer => (writer, false)).ToList() ?? throw new ArgumentNullException(nameof(writers));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeeTextWriter"/> class from an enumerable of TeeTextWriter.
    /// </summary>
    /// <param name="writers">The tee text writers to write to.</param>
    /// <param name="leaveOpen">Whether to leave the writers open after disposing.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// var tee1 = new TeeTextWriter(writer1);
    /// var tee2 = new TeeTextWriter(writer2);
    /// var combined = new TeeTextWriter(new[] { tee1, tee2 }, leaveOpen: true);
    /// </code>
    /// </example>
    /// </remarks>
    public TeeTextWriter(IEnumerable<TeeTextWriter> writers, bool leaveOpen = false)
    {
        this.writers = new List<(TextWriter writer, bool leaveOpen)>();
        foreach (var writer in writers)
        {
            this.writers.Add((writer, leaveOpen));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TeeTextWriter"/> class from an enumerable of writer tuples.
    /// </summary>
    /// <param name="writers">The writers with their leave open settings.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// var writers = new[] { (writer1, true), (writer2, false) };
    /// var tee = new TeeTextWriter(writers);
    /// tee.Write('a');
    /// </code>
    /// </example>
    /// </remarks>
    public TeeTextWriter(IEnumerable<(TextWriter writer, bool leaveOpen)> writers)
    {
        this.writers = writers?.ToList() ?? throw new ArgumentNullException(nameof(writers));
    }

    public override Encoding Encoding => this.writers.First().writer.Encoding;

    /// <summary>
    /// Writes a character to all underlying writers.
    /// </summary>
    /// <param name="value">The character to write.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// using var tee = new TeeTextWriter(writer1, writer2);
    /// tee.Write('a');
    /// Assert.Equal("a", writer1.ToString());
    /// Assert.Equal("a", writer2.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override void Write(char value)
    {
        foreach (var (writer, _) in this.writers)
        {
            writer.Write(value);
        }
    }

    /// <summary>
    /// Writes a string followed by a line terminator to all underlying writers.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// using var tee = new TeeTextWriter(writer1, writer2);
    /// tee.WriteLine("hello");
    /// Assert.Equal("hello" + Environment.NewLine, writer1.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override void WriteLine(string? value)
    {
        foreach (var (writer, _) in this.writers)
        {
            writer.WriteLine(value);
        }
    }

    /// <summary>
    /// Asynchronously disposes all underlying writers that are not marked as leave open.
    /// </summary>
    /// <returns>A task representing the async dispose operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer1 = new StringWriter();
    /// var writer2 = new StringWriter();
    /// var tee = new TeeTextWriter(writer1, writer2);
    /// await tee.DisposeAsync();
    /// </code>
    /// </example>
    /// </remarks>
    public override async ValueTask DisposeAsync()
    {
        if (this.disposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        this.disposed = true;

        foreach (var (writer, leaveOpen) in this.writers)
        {
            if (!leaveOpen)
            {
                await writer.DisposeAsync().ConfigureAwait(false);
            }
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        this.disposed = true;

        if (disposing)
        {
            foreach (var (writer, leaveOpen) in this.writers)
            {
                if (!leaveOpen)
                {
                    writer.Dispose();
                }
            }
        }

        base.Dispose(disposing);
    }
}