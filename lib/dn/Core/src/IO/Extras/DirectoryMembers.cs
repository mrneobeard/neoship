using System.Runtime.CompilerServices;

using NeoBeard.Extras;

namespace NeoBeard.IO.Extras;

/// <summary>
/// Represents the DirectoryMembers class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class DirectoryMembers
{
    extension(Directory)
    {
        /// <summary>
        /// Copies a directory and optionally its contents recursively.
        /// </summary>
        /// <param name="sourceDirName">The source directory path.</param>
        /// <param name="destDirName">The destination directory path.</param>
        /// <param name="recursive">Whether to copy subdirectories recursively.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var source = Path.Combine(Path.GetTempPath(), "source");
        /// var dest = Path.Combine(Path.GetTempPath(), "dest");
        /// Directory.CreateDirectory(source);
        /// File.WriteAllText(Path.Combine(source, "file.txt"), "content");
        /// Directory.Copy(source, dest, recursive: true);
        /// Assert.True(Directory.Exists(dest));
        /// </code>
        /// </example>
        /// </remarks>
        public static void Copy(string sourceDirName, string destDirName, bool recursive = false)
        {
            if (sourceDirName.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(sourceDirName), "Source directory name cannot be null or empty.");

            if (destDirName.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(destDirName), "Destination directory name cannot be null or empty.");

            var src = new DirectoryInfo(sourceDirName);
            if (!src.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {sourceDirName}");

            if (!(src.Attributes & FileAttributes.Directory).HasFlag(FileAttributes.Directory))
                throw new IOException($"Source path is not a directory: {sourceDirName}");

            var dest = new DirectoryInfo(destDirName);
            if (!dest.Exists)
            {
                dest.Create();
            }
            else if (dest.Exists && !dest.Attributes.HasFlag(FileAttributes.Directory))
            {
                throw new IOException($"Destination path already exists and is not a directory: {destDirName}");
            }

            foreach (var file in src.GetFiles())
            {
                var destFile = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(destFile);
            }

            if (recursive)
            {
                foreach (var dir in src.GetDirectories())
                {
                    var destSubDir = System.IO.Path.Combine(destDirName, dir.Name);
                    Copy(dir.FullName, destSubDir, true);
                }
            }
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <returns>A <see cref="DirectoryInfo"/> for the directory.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.Combine(Path.GetTempPath(), "new_dir");
        /// var info = Directory.Ensure(path);
        /// Assert.True(info.Exists);
        /// </code>
        /// </example>
        /// </remarks>
        public static DirectoryInfo Ensure(string path)
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists)
                info.Create();

            return info;
        }

        /// <summary>
        /// Creates a directory at the specified path.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        /// <returns>A <see cref="DirectoryInfo"/> for the created directory.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.Combine(Path.GetTempPath(), "new_dir");
        /// var info = Directory.Make(path);
        /// Assert.True(info.Exists);
        /// </code>
        /// </example>
        /// </remarks>
        public static unsafe DirectoryInfo Make(string path)
                => Directory.CreateDirectory(path);

        /// <summary>
        /// Creates a temporary directory with an optional prefix.
        /// </summary>
        /// <param name="prefix">An optional prefix for the directory name.</param>
        /// <returns>A <see cref="DirectoryInfo"/> for the created temporary directory.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var tempDir = Directory.MakeTemp("myapp_");
        /// Assert.True(tempDir.Exists);
        /// Assert.StartsWith("myapp_", tempDir.Name);
        /// </code>
        /// </example>
        /// </remarks>
        public static unsafe DirectoryInfo MakeTemp(string? prefix = default)
                => Directory.CreateTempSubdirectory(prefix);

        /// <summary>
        /// Enumerates all file system entries in a directory.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <returns>An enumerable of file system entries.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var entries = Directory.Read(Path.GetTempPath()).ToList();
        /// Assert.NotEmpty(entries);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FileSystemInfo> Read(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return dirInfo.EnumerateFileSystemInfos();
        }

        /// <summary>
        /// Enumerates file system entries matching a search pattern.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <returns>An enumerable of matching file system entries.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var tempFile = Path.Combine(Path.GetTempPath(), "test.txt");
        /// File.WriteAllText(tempFile, "content");
        /// var entries = Directory.Read(Path.GetTempPath(), "*.txt").ToList();
        /// Assert.NotEmpty(entries);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FileSystemInfo> Read(string path, string searchPattern)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return dirInfo.EnumerateFileSystemInfos(searchPattern);
        }

        /// <summary>
        /// Enumerates file system entries matching a search pattern with search option.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="searchOption">Whether to search top directory only or all subdirectories.</param>
        /// <returns>An enumerable of matching file system entries.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var entries = Directory.Read(Path.GetTempPath(), "*.txt", SearchOption.TopDirectoryOnly).ToList();
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<FileSystemInfo> Read(string path, string searchPattern, SearchOption searchOption)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return dirInfo.EnumerateFileSystemInfos(searchPattern, searchOption);
        }

        /// <summary>
        /// Resolves the real path of a symbolic link.
        /// </summary>
        /// <param name="linkPath">The path to the symbolic link.</param>
        /// <param name="returnFinalTarget">Whether to return the final target.</param>
        /// <returns>The resolved file system entry, or <c>null</c> if resolution fails.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var realPath = Directory.RealPath("/path/to/symlink");
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileSystemInfo? RealPath(string linkPath, bool returnFinalTarget = true)
                => Directory.ResolveLinkTarget(linkPath, returnFinalTarget);

        /// <summary>
        /// Creates a symbolic link to a directory.
        /// </summary>
        /// <param name="path">The path where the symbolic link will be created.</param>
        /// <param name="target">The target directory the link points to.</param>
        /// <returns>A <see cref="FileSystemInfo"/> for the created symbolic link.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var target = Path.Combine(Path.GetTempPath(), "target");
        /// Directory.CreateDirectory(target);
        /// var link = Directory.SymlinkDirectory(Path.Combine(Path.GetTempPath(), "link"), target);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileSystemInfo SymlinkDirectory(string path, string target)
                => Directory.CreateSymbolicLink(path, target);

    }
}