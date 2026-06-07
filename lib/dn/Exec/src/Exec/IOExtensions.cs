using System.Buffers;
using System.Text;

using NeoBeard.Text;
using NeoBeard.Text.Extras;

namespace NeoBeard.Exec;

internal static class IOExtensions
{
    /// <summary>
    /// Determines whether the given exception is an I/O exception caused by the input stream being closed by the reader, which can occur when a process stops reading from its input before we're done writing to it. This method checks for the specific HResult value associated with this scenario and also recursively checks inner exceptions and aggregate exceptions.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns><c>true</c> if the exception is an input I/O exception; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// try
    /// {
    ///     throw new IOException("Broken pipe", unchecked((int)0x8007006D));
    /// }
    /// catch (Exception ex)
    /// {
    ///     Assert.True(ex.IsInputIOException());
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static bool IsInputIOException(this Exception ex)
    {
        if (ex is AggregateException aggregateException)
            return aggregateException.InnerExceptions.All(IsInputIOException);

        if (ex is IOException ioException)
        {
            // this occurs when a head-like process stops reading from the input before we're done writing to it
            // see http://stackoverflow.com/questions/24876580/how-to-distinguish-programmatically-between-different-ioexceptions/24877149#24877149
            // see http://msdn.microsoft.com/en-us/library/cc231199.aspx
            return unchecked((uint)ioException.HResult) == 0x8007006D;
        }

        return ex.InnerException != null && IsInputIOException(ex.InnerException);
    }

    /// <summary>
    /// Pipes the contents of the <paramref name="reader"/> to the <paramref name="writer"/> using a buffer of the specified size. This method reads from the reader in chunks and writes to the writer until the end of the reader is reached. It also handles exceptions that may occur during reading and writing, specifically checking for input I/O exceptions that can occur when a process stops reading from its input before we're done writing to it.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="bufferSize">The size of the buffer to use for reading and writing. If negative, a default buffer size of 4096 will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown when the reader or writer is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello, world!");
    /// using var writer = new StringWriter();
    /// reader.PipeTo(writer);
    /// Console.WriteLine(writer.ToString()); // Outputs: Hello, world!
    /// </code>
    /// </example>
    /// </remarks>
    public static void PipeTo(
        this TextReader reader,
        TextWriter writer,
        int bufferSize = -1)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (bufferSize < 0)
            bufferSize = 4096;

        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        try
        {
            int read;
            var span = new Span<char>(buffer);

            while ((read = reader.Read(span)) > 0)
            {
                writer.Write(span.Slice(0, read));
            }
        }
        catch (Exception ex)
        {
            if (!ex.IsInputIOException())
                throw;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    /// <summary>
    /// Pipes the contents of the <paramref name="reader"/> to the <paramref name="stream"/> using a <see cref="StreamWriter"/> with the specified encoding and buffer size. This method reads from the reader and writes to the stream until the end of the reader is reached. It also handles exceptions that may occur during reading and writing, specifically checking for input I/O exceptions that can occur when a process stops reading from its input before we're done writing to it. The stream will be closed after writing unless <paramref name="leaveOpen"/> is set to true.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="encoding">The encoding to use when writing to the stream. If null, UTF8 without BOM will be used.</param>
    /// <param name="bufferSize">The size of the buffer to use for reading and writing. If negative, a default buffer size of 4096 will be used.</param>
    /// <param name="leaveOpen">Indicates whether to leave the stream open after writing. If false, the stream will be closed.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the reader or stream is null.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello, world!");
    /// using var ms = new MemoryStream();
    /// reader.PipeTo(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public static void PipeTo(
        this TextReader reader,
        Stream stream,
        Encoding? encoding = null,
        int bufferSize = -1,
        bool leaveOpen = false)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

        using var sw = new StreamWriter(stream, encoding, bufferSize, leaveOpen);
        reader.PipeTo(sw, bufferSize);
    }

    /// <summary>
    /// Asynchronously pipes the contents of the <paramref name="reader"/> to the <paramref name="file"/>. This
    /// method reads from the reader and writes to the specified file until the end of the reader is
    /// reached. It also handles exceptions that may occur during reading and writing, specifically
    /// checking for input I/O exceptions that can occur when a process stops reading from its input before
    /// we're done writing to it. The file will be created if it does not exist, or overwritten if it does.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="file">The file to write to.</param>
    /// <param name="encoding">The encoding to use when writing to the file. If null, UTF8 without BOM will be used.</param>
    /// <param name="bufferSize">The size of the buffer to use when writing to the file. If less than 0, a default size of 4096 will be used.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the reader or file is null.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello, file!");
    /// var file = new FileInfo("output.txt");
    /// reader.PipeTo(file);
    /// </code>
    /// </example>
    /// </remarks>
    public static void PipeTo(
        this TextReader reader,
        FileInfo file,
        Encoding? encoding = null,
        int bufferSize = -1)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (file is null)
            throw new ArgumentNullException(nameof(file));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;
        using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
        reader.PipeTo(stream, encoding, bufferSize, false);
    }

    /// <summary>
    /// Pipes the contents of the <paramref name="reader"/> to the <paramref name="lines"/> collection
    /// by reading lines from the reader and adding them to the collection until the end of the reader
    /// is reached. It also handles exceptions that may occur during reading, specifically checking
    /// for input I/O exceptions that can occur when a process stops reading from its input before
    /// we're done writing to it.
    /// </summary>
    /// <param name="reader"> The text reader to read from.</param>
    /// <param name="lines"> The collection to add the read lines to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the reader or lines collection is null.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello\nWorld");
    /// var lines = new List&lt;string&gt;();
    /// reader.PipeTo(lines);
    /// Assert.Equal(2, lines.Count);
    /// </code>
    /// </example>
    /// </remarks>
    public static void PipeTo(
        this TextReader reader,
        ICollection<string> lines)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        try
        {
            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
            }
        }
        catch (Exception ex)
        {
            if (!ex.IsInputIOException())
                throw;
        }
    }

    /// <summary>
    /// Asynchronously pipes the contents of the <paramref name="reader"/> to the <paramref name="file"/>. This
    /// method reads from the reader and writes to the specified file until the end of the reader is
    /// reached. It also handles exceptions that may occur during reading and writing, specifically
    /// checking for input I/O exceptions that can occur when a process stops reading from its input
    /// before we're done writing to it. The file will be created if it does not exist, or overwritten
    /// if it does.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="file">The file to write to.</param>
    /// <param name="encoding">The encoding to use when writing to the file. If null, UTF8 without BOM will be used.</param>
    /// <param name="bufferSize">The size of the buffer to use when writing to the file. If less than 0, a default size of 4096 will be used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous pipe operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the reader or file is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello, file!");
    /// var file = new FileInfo("output.txt");
    /// await reader.PipeToAsync(file);
    /// </code>
    /// </example>
    /// </remarks>
    public static Task PipeToAsync(
        this TextReader reader,
        FileInfo file,
        Encoding? encoding = null,
        int bufferSize = -1,
        CancellationToken cancellationToken = default)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (file is null)
            throw new ArgumentNullException(nameof(file));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

        return InnerPipeToAsync(reader, file, encoding, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the contents of the <paramref name="reader"/> to the <paramref name="lines"/>
    /// collection by reading lines from the reader and adding them to the collection until the end of the
    /// reader is reached. It also handles exceptions that may occur during reading, specifically checking
    /// for input I/O exceptions that can occur when a process stops reading from its input before we're
    /// done writing to it.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="lines">The collection to add the read lines to.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous pipe operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the reader or lines collection is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello\nWorld");
    /// var lines = new List&lt;string&gt;();
    /// await reader.PipeToAsync(lines);
    /// Console.WriteLine(string.Join(", ", lines)); // Outputs: Hello, World
    /// </code>
    /// </example>
    /// </remarks>
    public static Task PipeToAsync(
        this TextReader reader,
        ICollection<string> lines,
        CancellationToken cancellationToken = default)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        return InnerPipeToAsync(reader, lines, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the contents of the <paramref name="reader"/> to the <paramref name="writer"/>
    /// using a buffer of the specified size. This method reads from the reader in chunks and writes to the
    /// writer until the end of the reader is reached. It also handles exceptions that may occur during
    /// reading and writing, specifically checking for input I/O exceptions that can occur when a
    /// process stops reading from its input before we're done writing to it.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="bufferSize">The size of the buffer to use for reading and writing. If negative, a default buffer size of 4096 will be used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when the reader or writer is null.</exception>
    /// <returns>A task that represents the asynchronous pipe operation.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    ///   using var reader = new StringReader("Hello, world!");
    ///   using var writer = new StringWriter();
    ///   await reader.PipeToAsync(writer);
    ///   Console.WriteLine(writer.ToString()); // Outputs: Hello, world!
    /// </code>
    /// </example>
    /// </remarks>
    public static Task PipeToAsync(
        this TextReader reader,
        TextWriter writer,
        int bufferSize = -1,
        CancellationToken cancellationToken = default)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (bufferSize < 0)
            bufferSize = 4096;

        return InnerPipeToAsync(reader, writer, bufferSize, cancellationToken);
    }

    /// <summary>
    /// Asynchronously pipes the contents of the <paramref name="reader"/> to the <paramref name="stream"/> using a <see cref="StreamWriter"/> with the specified encoding and buffer size. This method reads from the reader and writes to the stream until the end of the reader is reached. It also handles exceptions that may occur during reading and writing, specifically checking for input I/O exceptions that can occur when a process stops reading from its input before we're done writing to it. The stream will be closed after writing unless <paramref name="leaveOpen"/> is set to true.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="stream"> The stream to write to.</param>
    /// <param name="encoding">The encoding to use when writing to the stream. If null, UTF8 without BOM will be used.</param>
    /// <param name="bufferSize">The size of the buffer to use for reading and writing. If negative, a default buffer size of 4096 will be used.</param>
    /// <param name="leaveOpen">Indicates whether to leave the stream open after writing. If false, the stream will be closed.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous pipe operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the reader or stream is null.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello, world!");
    /// using var ms = new MemoryStream();
    /// await reader.PipeToAsync(ms);
    /// </code>
    /// </example>
    /// </remarks>
    public static Task PipeToAsync(
        this TextReader reader,
        Stream stream,
        Encoding? encoding = null,
        int bufferSize = -1,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

        return InnerPipeToAsync(reader, stream, encoding, bufferSize, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// Reads characters from the text reader into the provided span buffer. This method rents a buffer from the shared array pool, reads characters into the buffer, and then copies the read characters to the provided span. It returns the number of characters read. This approach minimizes allocations and can improve performance when reading large amounts of data.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="chars">The span buffer to read characters into.</param>
    /// <returns>The number of characters read into the buffer.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello");
    /// Span&lt;char&gt; buffer = stackalloc char[10];
    /// int read = reader.Read(buffer);
    /// Assert.Equal(5, read);
    /// </code>
    /// </example>
    /// </remarks>
    public static int Read(this TextReader reader, Span<char> chars)
    {
        var buffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            var read = reader.Read(buffer, 0, buffer.Length);
            if (read > 0)
                buffer.AsSpan(0, read).CopyTo(chars);

            return read;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    /// <summary>
    /// Reads characters from the text reader into the provided memory buffer asynchronously. This method rents a buffer from the shared array pool, reads characters into the buffer, and then copies the read characters to the provided memory. It returns the number of characters read. This approach minimizes allocations and can improve performance when reading large amounts of data.
    /// </summary>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="chars">The memory buffer to read characters into.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous read operation.</param>
    /// <returns>A task that represents the asynchronous read operation. The result contains the number of characters read.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello");
    /// var buffer = new Memory&lt;char&gt;(new char[10]);
    /// int read = await reader.ReadAsync(buffer);
    /// Assert.Equal(5, read);
    /// </code>
    /// </example>
    /// </remarks>
    public static Task<int> ReadAsync(this TextReader reader, Memory<char> chars, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            var read = reader.Read(buffer, 0, chars.Length);
            if (read > 0)
                buffer.AsSpan(0, read).CopyTo(chars.Span);

            return Task.FromResult(read);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    /// <summary>
    /// Writes the specified characters to the text writer asynchronously using a buffer. This method rents a buffer from the shared array pool, copies the characters to the buffer, and then writes the buffer to the writer asynchronously. After writing, it returns the buffer to the pool. This approach minimizes allocations and can improve performance when writing large amounts of data.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="chars">The characters to write.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous write operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the writer is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var writer = new StringWriter();
    /// var chars = "Hello".AsMemory();
    /// await writer.WriteAsync(chars);
    /// Assert.Equal("Hello", writer.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public static Task WriteAsync(this TextWriter writer, ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            chars.Span.CopyTo(buffer);
            return writer.WriteAsync(buffer, 0, chars.Length);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    /// <summary>
    /// Writes the contents of the <paramref name="reader"/> to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The text writer.</param>
    /// <param name="reader">The text reader to read from.</param>
    /// <param name="bufferSize">The size of the buffer to use. Defaults to 4096.</param>
    /// <exception cref="ArgumentNullException">Thrown when the writer or reader is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var reader = new StringReader("Hello");
    /// using var writer = new StringWriter();
    /// writer.Write(reader);
    /// Assert.Equal("Hello", writer.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public static void Write(this TextWriter writer, TextReader reader, int bufferSize = -1)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (reader is null)
            throw new ArgumentNullException(nameof(reader));

        if (bufferSize < 0)
            bufferSize = 4096;

        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        try
        {
            var span = new Span<char>(buffer);
            int read;
            while ((read = reader.Read(span)) > 0)
            {
                writer.Write(span.Slice(0, read));
            }
        }
        catch (Exception ex)
        {
            if (!ex.IsInputIOException())
                throw;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    /// <summary>
    ///  Writes the contents of the <paramref name="stream"/> to the <paramref name="writer"/>. The the method will close
    /// the stream unless <paramref name="leaveOpen"/> is true.
    /// </summary>
    /// <param name="writer">The text writer.</param>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF8.</param>
    /// <param name="bufferSize">The size of the buffer to use. Defaults to 4096.</param>
    /// <param name="leaveOpen">Instructs to leave the stream open.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer or stream is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
    /// using var writer = new StringWriter();
    /// writer.Write(ms);
    /// Assert.Equal("Hello", writer.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public static void Write(this TextWriter writer, Stream stream, Encoding? encoding = null, int bufferSize = -1, bool leaveOpen = false)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

        using var reader = new StreamReader(stream, encoding, true, bufferSize, leaveOpen);
        writer.Write(reader, bufferSize);
    }

    /// <summary>
    /// Writes the contents of the file to the <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The text writer.</param>
    /// <param name="file">The file to read from.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF8.</param>
    /// <param name="bufferSize">The size of the buffer to use. Defaults to 4096.</param>
    /// <exception cref="ArgumentNullException">Thrown when writer or file is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var file = new FileInfo("test.txt");
    /// File.WriteAllText(file.FullName, "Hello");
    /// using var writer = new StringWriter();
    /// writer.Write(file);
    /// Assert.Equal("Hello", writer.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public static void Write(this TextWriter writer, FileInfo file, Encoding? encoding = null, int bufferSize = -1)
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));

        if (file is null)
            throw new ArgumentNullException(nameof(file));

        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

        using var stream = file.OpenRead();
        using var reader = new StreamReader(stream, encoding, true, bufferSize, false);
        writer.Write(reader, bufferSize);
    }

    /// <summary>
    /// Converts a string to SCREAMING_SNAKE_CASE by inserting underscores between words and converting all characters to uppercase. It treats hyphens, spaces, and underscores as word separators and ignores non-alphanumeric characters. Consecutive uppercase letters are considered part of the same word unless followed by a lowercase letter, in which case an underscore is inserted before the uppercase letter. This method is useful for converting identifiers to a common constant naming convention.
    /// </summary>
    /// <param name="value">The string to convert to SCREAMING_SNAKE_CASE.</param>
    /// <returns>The input string converted to SCREAMING_SNAKE_CASE.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string input = "HelloWorld";
    /// string output = input.ScreamingSnakeCase();
    /// Console.WriteLine(output); // Outputs: HELLO_WORLD
    /// </code>
    /// </example>
    /// </remarks>
    public static string ScreamingSnakeCase(this string value)
    {
        var builder = StringBuilderCache.Acquire();
        var previous = char.MinValue;
        foreach (var c in value)
        {
            if (char.IsUpper(c) && builder.Length > 0 && previous != '_')
            {
                builder.Append('_');
            }

            if (c is '-' or ' ' or '_')
            {
                builder.Append('_');
                previous = '_';
                continue;
            }

            if (!char.IsLetterOrDigit(c))
                continue;

            builder.Append(char.ToUpperInvariant(c));
            previous = c;
        }

        return StringBuilderCache.GetStringAndRelease(builder);
    }

    private static async Task InnerPipeToAsync(
        TextReader reader,
        TextWriter writer,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        if (bufferSize < 0)
            bufferSize = 4096;

        char[] buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        try
        {
            int read;
            var memory = new Memory<char>(buffer);

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            while ((read = await reader.ReadAsync(memory, cancellationToken)) > 0)
            {
                await writer.WriteAsync(memory.Slice(0, read), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (!ex.IsInputIOException())
                throw;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, true);
        }
    }

    private static async Task InnerPipeToAsync(
        TextReader reader,
        Stream stream,
        Encoding? encoding = null,
        int bufferSize = -1,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

#if NETLEGACY
        using var sw = new StreamWriter(stream, encoding, bufferSize, leaveOpen);
#else
        await using var sw = new StreamWriter(stream, encoding, bufferSize, leaveOpen);
#endif
        await reader.PipeToAsync(sw, bufferSize, cancellationToken);
    }

    private static async Task InnerPipeToAsync(
        TextReader reader,
        FileInfo file,
        Encoding? encoding,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        if (bufferSize < 0)
            bufferSize = 4096;

        encoding ??= Encoding.UTF8NoBom;

#if NETLEGACY
        using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
#else
        await using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
#endif
        await reader.PipeToAsync(stream, encoding, bufferSize, false, cancellationToken);
    }

    private static async Task InnerPipeToAsync(
        TextReader reader,
        ICollection<string> lines,
        CancellationToken cancellationToken)
    {
        try
        {
#if !NET7_0_OR_GREATER
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                lines.Add(line);
            }
#else
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                lines.Add(line);
            }
#endif
        }
        catch (Exception ex)
        {
            if (!ex.IsInputIOException())
                throw;
        }
    }
}