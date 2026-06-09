using System.Diagnostics;

namespace NeoBeard.IO;

/// <summary>
/// Represents the DisposableDirectory class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public sealed class DisposableDirectory : IDisposable
{
    private readonly string directoryPath;

    private readonly Action<DisposableDirectory>? onDispose;

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableDirectory"/> class.
    /// </summary>
    /// <param name="directoryPath">The path to the directory that will be deleted on dispose.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    /// Directory.CreateDirectory(path);
    /// using var dir = new DisposableDirectory(path);
    /// Assert.True(Directory.Exists(path));
    /// </code>
    /// </example>
    /// </remarks>
    public DisposableDirectory(string directoryPath)
    {
        this.directoryPath = directoryPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableDirectory"/> class with a dispose callback.
    /// </summary>
    /// <param name="directoryPath">The path to the directory that will be deleted on dispose.</param>
    /// <param name="onDispose">An action to invoke after the directory is deleted.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    /// Directory.CreateDirectory(path);
    /// var disposed = false;
    /// using var dir = new DisposableDirectory(path, _ => disposed = true);
    /// </code>
    /// </example>
    /// </remarks>
    public DisposableDirectory(string directoryPath, Action<DisposableDirectory> onDispose)
    {
        this.directoryPath = directoryPath;
        this.onDispose = onDispose;
    }

    /// <summary>
    /// Implicitly converts a <see cref="DisposableDirectory"/> to its directory path string.
    /// </summary>
    /// <param name="disposableDirectory">The disposable directory to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var dir = new DisposableDirectory(Path.GetTempPath());
    /// string path = dir;
    /// Assert.Equal(dir.DirectoryPath, path);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator string(DisposableDirectory disposableDirectory)
    {
        return disposableDirectory?.directoryPath ?? throw new ArgumentNullException(nameof(disposableDirectory));
    }

    /// <summary>
    /// Implicitly converts a directory path string to a <see cref="DisposableDirectory"/>.
    /// </summary>
    /// <param name="directoryPath">The directory path to convert.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// string path = Path.GetTempPath();
    /// DisposableDirectory dir = path;
    /// Assert.Equal(path, dir.DirectoryPath);
    /// </code>
    /// </example>
    /// </remarks>
    public static implicit operator DisposableDirectory(string directoryPath)
    {
        return new DisposableDirectory(directoryPath);
    }

    public string DirectoryPath => this.directoryPath;

    /// <summary>
    /// Releases resources and deletes the directory recursively.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    /// Directory.CreateDirectory(path);
    /// var dir = new DisposableDirectory(path);
    /// dir.Dispose();
    /// Assert.False(Directory.Exists(path));
    /// </code>
    /// </example>
    /// </remarks>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (this.disposed)
            return;

        // Release unmanaged resources here
        if (System.IO.Directory.Exists(this.directoryPath))
        {
            System.IO.Directory.Delete(this.directoryPath, true);
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