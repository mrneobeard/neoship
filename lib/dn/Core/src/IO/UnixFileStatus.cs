namespace NeoBeard.IO;

public readonly struct UnixFileStatus
{
    internal UnixFileStatus(string path, FFI.Sys.FileStatus status)
    {
        this.Path = path;
        this.Mode = status.Mode;
        this.UserId = (int)status.Uid;
        this.GroupId = (int)status.Gid;
        this.Size = status.Size;
        this.AccessTime = status.ATime;
        this.ModifiedTime = status.MTime;
        this.ChangedTime = status.CTime;
        this.BirthTime = status.BirthTime;
        this.Dev = status.Dev;
        this.Ino = status.Ino;
    }

    internal UnixFileStatus(string path, FileInfo info)
    {
        this.Path = path;
        if (info.LinkTarget is not null)
        {
            this.Mode = 0xA000; // S_IFLNK bit for symbolic links
        }
        else if (info.Attributes.HasFlag(FileAttributes.Directory))
        {
            this.Mode = 0x4000; // S_IFDIR bit for directories
        }
        else
        {
            this.Mode = 0x8000; // S_IFREG bit for regular files
        }

        this.UserId = -1; // User ID is not available in FileInfo
        this.GroupId = -1; // Group ID is not available in FileInfo
        this.Size = info.Length;
        this.AccessTime = new DateTimeOffset(info.LastAccessTime).ToUnixTimeSeconds();
        this.ModifiedTime = new DateTimeOffset(info.LastWriteTime).ToUnixTimeSeconds();
        this.ChangedTime = new DateTimeOffset(info.LastWriteTime).ToUnixTimeSeconds(); // Changed time is the last write time
        this.BirthTime = new DateTimeOffset(info.CreationTime).ToUnixTimeSeconds(); // Birth time is the creation time
        this.Dev = -1; // Device ID is not available in FileInfo
        this.Ino = -1; // Inode number is not available in FileInfo
    }

    public string Path { get; }

    public int Mode { get; }

    public int UserId { get; }

    public int GroupId { get; }

    public long Size { get; }

    public long AccessTime { get; }

    public long ModifiedTime { get; }

    public long ChangedTime { get; }

    public long BirthTime { get; }

    public long Dev { get; }

    public long Ino { get; }

    public bool IsDirectory => (this.Mode & 0x4000) != 0; // S_IFDIR bit

    public bool IsFile => (this.Mode & 0x8000) != 0; // S_IFREG bit

    public bool IsSymbolicLink => (this.Mode & 0xA000) != 0; // S_IFLNK bit

    /// <summary>
    /// Gets the user name associated with this file's user ID.
    /// </summary>
    /// <returns>The user name, or <c>null</c> if unavailable.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = File.Stat("/etc/passwd");
    /// var userName = status.GetUserName();
    /// Assert.NotNull(userName);
    /// </code>
    /// </example>
    /// </remarks>
    public string? GetUserName()
    {
        if (this.UserId < 0)
            return null;

        var uid = (uint)this.UserId;
        return FFI.Sys.GetUserName(uid);
    }

    /// <summary>
    /// Gets the group name associated with this file's group ID.
    /// </summary>
    /// <returns>The group name, or <c>null</c> if unavailable.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = File.Stat("/etc/passwd");
    /// var groupName = status.GetGroupName();
    /// Assert.NotNull(groupName);
    /// </code>
    /// </example>
    /// </remarks>
    public string? GetGroupName()
    {
        if (this.GroupId < 0)
            return null;

        var gid = (uint)this.GroupId;
        return FFI.Libc.GetGroupName(gid);
    }
}