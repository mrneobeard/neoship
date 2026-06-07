namespace NeoBeard.IO;

/// <summary>
/// Represents the type of a file system entry.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var type = FileSystemEntryType.File;
/// var includesFile = (FileSystemEntryType.All &amp; type) == type;
/// </code>
/// </example>
/// </remarks>
public enum FileSystemEntryType
{
    /// <summary>
    /// Represents a file system entry that is not recognized.
    /// </summary>
    All = File | Directory | SymbolicLink,

    /// <summary>
    /// Represents a file system entry that is a file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Represents a file system entry that is a directory.
    /// </summary>
    Directory = 2,

    /// <summary>
    /// Represents a file system entry that is a symbolic link.
    /// </summary>
    SymbolicLink = 3,
}