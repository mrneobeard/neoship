using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;

using NeoBeard.Extras;

namespace NeoBeard.IO.Extras;

/// <summary>
/// Represents the FileMembers class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class FileMembers
{
    extension(File)
    {
        /// <summary>
        /// Appends bytes to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="bytes">The bytes to append.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// File.Append(path, new byte[] { 1, 2, 3 });
        /// File.Append(path, new byte[] { 4, 5, 6 });
        /// Assert.Equal(6, File.ReadAllBytes(path).Length);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Append(string path, byte[] bytes)
                => File.AppendAllBytes(path, bytes);

        /// <summary>
        /// Appends a span of bytes to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="bytes">The bytes to append.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// ReadOnlySpan&lt;byte&gt; data = stackalloc byte[] { 1, 2, 3 };
        /// File.Append(path, data);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Append(string path, ReadOnlySpan<byte> bytes)
                => File.AppendAllBytes(path, bytes);

        /// <summary>
        /// Asynchronously appends bytes to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="bytes">The bytes to append.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// await File.AppendAsync(path, new byte[] { 1, 2, 3 });
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task AppendAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
                => File.AppendAllBytesAsync(path, bytes, cancellationToken);

        /// <summary>
        /// Asynchronously appends memory bytes to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="bytes">The bytes to append.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// var data = new Memory&lt;byte&gt;(new byte[] { 1, 2, 3 });
        /// await File.AppendAsync(path, data);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task AppendAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
                => File.AppendAllBytesAsync(path, bytes, cancellationToken);

        /// <summary>
        /// Changes file permissions using Unix file mode.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="mode">The Unix file mode to set.</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chmod(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chmod(string path, UnixFileMode mode, bool recursive = false)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Chmod is not supported on Windows.");

            File.SetUnixFileMode(path, mode);

            if (!recursive)
                return;

            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    File.SetUnixFileMode(file, mode);
                }

                foreach (var directory in Directory.EnumerateDirectories(path))
                {
                    Chmod(directory, mode, true);
                }
            }
        }

        /// <summary>
        /// Changes file permissions using octal notation.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="octal">The octal permission value (e.g., 0755).</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chmod(path, 0644);
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chmod(string path, int octal, bool recursive = false)
                => Chmod(path, UnixFileModeMembers.FromOctal(octal), recursive);

        /// <summary>
        /// Changes file ownership by user and group ID.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="groupId">The group ID.</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chown(path, 1000, 1000);
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chown(string path, int userId, int groupId, bool recursive = false)
        {
            if (OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Chown is not supported on Windows.");

            int result = 0;
            if (!recursive)
            {
                result = FFI.Libc.chown(path, (uint)userId, (uint)groupId);
                if (result != 0)
                {
                    var message = FFI.Sys.StrError(result);
                    throw new IOException($"Failed to change ownership of '{path}': {message}", result);
                }

                return;
            }

            result = FFI.Libc.chown(path, (uint)userId, (uint)groupId);
            if (result != 0)
            {
                var message = FFI.Sys.StrError(result);
                throw new IOException($"Failed to change ownership of '{path}': {message}", result);
            }

            var attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var files = Directory.EnumerateFiles(path);
                foreach (var file in files)
                {
                    result = FFI.Libc.chown(file, (uint)userId, (uint)groupId);
                    if (result != 0)
                    {
                        var message = FFI.Sys.StrError(result);
                        throw new IOException($"Failed to change ownership of '{file}': {message}", result);
                    }
                }

                var directories = Directory.EnumerateDirectories(path);
                foreach (var directory in directories)
                {
                    result = FFI.Libc.chown(directory, (uint)userId, (uint)groupId);
                    if (result != 0)
                    {
                        var message = FFI.Sys.StrError(result);
                        throw new IOException($"Failed to change ownership of '{directory}': {message}", result);
                    }
                }
            }
        }

        /// <summary>
        /// Changes file ownership by user ID.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chown(path, 1000);
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chown(string path, int userId, bool recursive = false)
        {
            if (OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Chown is not supported on Windows.");

            Chown(path, userId, userId, recursive);
        }

        /// <summary>
        /// Changes file ownership by user and group name.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chown(path, "root", "root");
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chown(string path, string userName, string groupName, bool recursive = false)
        {
            if (OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Chown is not supported on Windows.");

            uint? userId = FFI.Sys.GetUserId(userName);
            if (!userId.HasValue)
                throw new ArgumentException($"Invalid user name: {userName}");

            uint? groupId = FFI.Libc.GetGroupId(groupName);

            if (!groupId.HasValue)
                throw new ArgumentException($"Invalid group name: {groupName}");

            Chown(path, (int)userId.Value, (int)groupId.Value, recursive);
        }

        /// <summary>
        /// Changes file ownership by user name.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="recursive">Whether to apply recursively to directories.</param>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// File.Chown(path, "root");
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static void Chown(string path, string userName, bool recursive = false)
        {
            if (OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Chown is not supported on Windows.");

            uint? userId = FFI.Sys.GetUserId(userName);
            if (!userId.HasValue)
                throw new ArgumentException($"Invalid user name: {userName}");

            Chown(path, (int)userId.Value, recursive);
        }

        /// <summary>
        /// Gets file status without following symbolic links.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="UnixFileStatus"/> with file information.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = "/tmp/testfile";
        /// File.WriteAllText(path, "content");
        /// var status = File.LStat(path);
        /// Assert.True(status.IsFile);
        /// </code>
        /// </example>
        /// </remarks>
        [UnsupportedOSPlatform("windows")]
        public static UnixFileStatus LStat(string path)
        {
            if (OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Stat is not supported on Windows.");

            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File not found: {path}");

            var result = FFI.Sys.lstat(path, out var stat);
            if (result != 0)
            {
                var message = FFI.Sys.StrError(result);
                throw new IOException($"Failed to stat file '{path}': {message}", result);
            }

            return new UnixFileStatus(path, stat);
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
        /// var realPath = File.RealPath("/path/to/symlink");
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileSystemInfo? RealPath(string linkPath, bool returnFinalTarget = true)
                => File.ResolveLinkTarget(linkPath, returnFinalTarget);

        /// <summary>
        /// Gets file status following symbolic links.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A <see cref="UnixFileStatus"/> with file information.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// var status = File.Stat(path);
        /// Assert.True(status.IsFile);
        /// </code>
        /// </example>
        /// </remarks>
        public static UnixFileStatus Stat(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"File not found: {path}");

            if (OperatingSystem.IsWindows())
            {
                return new UnixFileStatus(path, fileInfo);
            }

            var result = FFI.Sys.stat(path, out var stat);
            if (result != 0)
            {
                var message = FFI.Sys.StrError(result);
                throw new IOException($"Failed to stat file '{path}': {message}", result);
            }

            return new UnixFileStatus(path, stat);
        }

        /// <summary>
        /// Creates a symbolic link to a file.
        /// </summary>
        /// <param name="path">The path where the symbolic link will be created.</param>
        /// <param name="target">The target file the link points to.</param>
        /// <returns>A <see cref="FileSystemInfo"/> for the created symbolic link.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var target = Path.GetTempFileName();
        /// var link = File.Symlink(Path.Combine(Path.GetTempPath(), "link"), target);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileSystemInfo Symlink(string path, string target)
                => File.CreateSymbolicLink(path, target);

        /// <summary>
        /// Asynchronously writes bytes to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// var data = new Memory&lt;byte&gt;(new byte[] { 1, 2, 3 });
        /// await File.WriteFileAsync(path, data);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteFileAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
                => File.WriteAllBytesAsync(path, bytes, cancellationToken);

        /// <summary>
        /// Asynchronously writes text to a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="contents">The text content to write.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// await File.WriteTextAsync(path, "Hello, World!".AsMemory());
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteTextAsync(string path, ReadOnlyMemory<char> contents, CancellationToken cancellationToken = default)
                => File.WriteAllTextAsync(path, contents, cancellationToken);

        /// <summary>
        /// Asynchronously writes text to a file with a specific encoding.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="contents">The text content to write.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var path = Path.GetTempFileName();
        /// await File.WriteTextAsync(path, "Hello".AsMemory(), Encoding.UTF8);
        /// </code>
        /// </example>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WriteTextAsync(string path, ReadOnlyMemory<char> contents, Encoding encoding, CancellationToken cancellationToken = default)
                => File.WriteAllTextAsync(path, contents, encoding, cancellationToken);

    }
}