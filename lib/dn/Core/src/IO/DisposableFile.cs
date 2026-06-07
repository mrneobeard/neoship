using System.Diagnostics;

namespace NeoBeard.IO;

/// <summary>
/// Represents the DisposableFile class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public sealed class DisposableFile : IDisposable
{
    private readonly string filePath;

    private readonly Action<DisposableFile>? onDispose;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableFile"/> class.
    /// </summary>
    /// <param name="filePath">The path to the file that will be deleted on dispose.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.GetTempFileName();
    /// using var file = new DisposableFile(path);
    /// Assert.True(File.Exists(path));
    /// </code>
    /// </example>
    /// </remarks>
    public DisposableFile(string filePath)
    {
        this.filePath = filePath;
        this.onDispose = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableFile"/> class with a dispose callback.
    /// </summary>
    /// <param name="filePath">The path to the file that will be deleted on dispose.</param>
    /// <param name="onDispose">An action to invoke after the file is deleted.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.GetTempFileName();
    /// var disposed = false;
    /// using var file = new DisposableFile(path, _ => disposed = true);
    /// </code>
    /// </example>
    /// </remarks>
    public DisposableFile(string filePath, Action<DisposableFile> onDispose)
    {
        this.filePath = filePath;
        this.onDispose = onDispose;
    }

    /// <summary>
    /// Implicitly converts a <see cref="DisposableFile"/> to its file path string.
    /// </summary>
    /// <param name="disposableFile">The disposable file to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var file = new DisposableFile(Path.GetTempFileName());
    /// string path = file;
    /// Assert.Equal(file.FilePath, path);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator string(DisposableFile disposableFile)
    {
        return disposableFile?.filePath ?? throw new ArgumentNullException(nameof(disposableFile));
    }

    /// <summary>
    /// Implicitly converts a file path string to a <see cref="DisposableFile"/>.
    /// </summary>
    /// <param name="filePath">The file path to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string path = Path.GetTempFileName();
    /// DisposableFile file = path;
    /// Assert.Equal(path, file.FilePath);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator DisposableFile(string filePath)
    {
        return new DisposableFile(filePath);
    }

    public string FilePath => this.filePath;

    /// <summary>
    /// Releases resources and deletes the file.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.GetTempFileName();
    /// var file = new DisposableFile(path);
    /// file.Dispose();
    /// Assert.False(File.Exists(path));
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (this.disposed)
            return;

        // Release unmanaged resources here
        if (System.IO.File.Exists(this.filePath))
        {
            System.IO.File.Delete(this.filePath);
        }

        try
        {
            this.onDispose?.Invoke(this);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during disposal: {ex.Message} -- {ex.StackTrace}");
        }

        this.disposed = true;
    }
}