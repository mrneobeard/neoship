namespace NeoBeard.Extras;

/// <summary>
/// Represents the UnixFileModeMembers class.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var instance = default(object);
/// </code>
/// </example>
/// </remarks>
public static class UnixFileModeMembers
{
    extension(UnixFileMode)
    {
        /// <summary>
        /// Creates a <see cref="UnixFileMode"/> from an octal permission value.
        /// </summary>
        /// <param name="octal">The octal permission value (e.g., 0755).</param>
        /// <returns>A <see cref="UnixFileMode"/> representing the permissions.</returns>
        /// <remarks>
        /// <example>
        /// <code lang="csharp">
        /// var mode = UnixFileMode.FromOctal(0755);
        /// Assert.True(mode.HasFlag(UnixFileMode.UserRead));
        /// Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
        /// </code>
        /// </example>
        /// </remarks>
        public static UnixFileMode FromOctal(int octal)
        {
            if (octal < 0 || octal > 0777)
                throw new ArgumentOutOfRangeException(nameof(octal), "Octal value must be between 0 and 0777.");

            int user = (octal / 100) % 10;
            int group = (octal / 10) % 10;
            int other = octal % 10;

            UnixFileMode mode = UnixFileMode.None;

            // User
            if ((user & 4) != 0) mode |= UnixFileMode.UserRead;
            if ((user & 2) != 0) mode |= UnixFileMode.UserWrite;
            if ((user & 1) != 0) mode |= UnixFileMode.UserExecute;

            // Group
            if ((group & 4) != 0) mode |= UnixFileMode.GroupRead;
            if ((group & 2) != 0) mode |= UnixFileMode.GroupWrite;
            if ((group & 1) != 0) mode |= UnixFileMode.GroupExecute;

            // Other
            if ((other & 4) != 0) mode |= UnixFileMode.OtherRead;
            if ((other & 2) != 0) mode |= UnixFileMode.OtherWrite;
            if ((other & 1) != 0) mode |= UnixFileMode.OtherExecute;

            return mode;
        }
    }
}