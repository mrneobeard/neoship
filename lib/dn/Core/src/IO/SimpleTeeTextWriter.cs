using System.Text;

namespace NeoBeard.IO;

/// <summary>
/// Writes text to two underlying <see cref="TextWriter"/> instances.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var first = new StringWriter();
/// using var second = new StringWriter();
/// await using var tee = new SimpleTeeTextWriter(first, second);
/// tee.WriteLine("hello");
/// </code>
/// </example>
/// </remarks>
public class SimpleTeeTextWriter : TextWriter
{
    private readonly TextWriter writer1;

    private readonly TextWriter writer2;

    private readonly bool leaveWriterOpen1;

    private readonly bool leaveWriterOpen2;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTeeTextWriter"/> class.
    /// </summary>
    /// <param name="writer1">The first writer that receives output.</param>
    /// <param name="writer2">The second writer that receives output.</param>
    /// <param name="leaveWriterOpen1"><see langword="true"/> to leave <paramref name="writer1"/> open when this writer is disposed.</param>
    /// <param name="leaveWriterOpen2"><see langword="true"/> to leave <paramref name="writer2"/> open when this writer is disposed.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var writer = new SimpleTeeTextWriter(Console.Out, TextWriter.Null, leaveWriterOpen1: true, leaveWriterOpen2: true);
    /// </code>
    /// </example>
    /// </remarks>
    public SimpleTeeTextWriter(TextWriter writer1, TextWriter writer2, bool leaveWriterOpen1 = false, bool leaveWriterOpen2 = false)
    {
        this.writer1 = writer1 ?? throw new ArgumentNullException(nameof(writer1));
        this.writer2 = writer2 ?? throw new ArgumentNullException(nameof(writer2));
        this.leaveWriterOpen1 = leaveWriterOpen1;
        this.leaveWriterOpen2 = leaveWriterOpen2;
    }

    public override Encoding Encoding => this.writer1.Encoding;

    /// <summary>
    /// Writes a character to both writers.
    /// </summary>
    /// <param name="value">The character to write.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// tee.Write('A');
    /// </code>
    /// </example>
    /// </remarks>
    public override void Write(char value)
    {
        this.writer1.Write(value);
        this.writer2.Write(value);
    }

    /// <summary>
    /// Writes a string followed by a line terminator to both writers.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// tee.WriteLine("ready");
    /// </code>
    /// </example>
    /// </remarks>
    public override void WriteLine(string? value)
    {
        this.writer1.WriteLine(value);
        this.writer2.WriteLine(value);
    }

    /// <summary>
    /// Asynchronously disposes this writer and, when configured, the underlying writers.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
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

        if (!this.leaveWriterOpen1)
        {
            await this.writer1.DisposeAsync().ConfigureAwait(false);
        }

        if (!this.leaveWriterOpen2)
        {
            await this.writer2.DisposeAsync().ConfigureAwait(false);
        }

        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        if (disposing)
        {
            if (!this.leaveWriterOpen1)
            {
                this.writer1.Dispose();
            }

            if (!this.leaveWriterOpen2)
            {
                this.writer2.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}