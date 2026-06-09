using NeoBeard.Extras;

namespace NeoBeard;

/// <summary>
/// Provides methods for working with environment variables and the environment PATH variable.
/// </summary>
/// <remarks>
/// <para>
/// This <c>Environ</c> class is designd to be imported with the <c>using static NeoBeard.Environ;</c>
/// so that the method names do not clash with other methods and still be shorter than
/// the methods on the <see cref="Environment"/> class.
/// </para>
/// <example>
/// <code lang="csharp">
/// var home = Env.Get(Env.Keys.Home);
/// </code>
/// </example>
/// </remarks>
public static partial class Env
{
    /// <summary>
    /// Appends a path to the environment variable PATH.
    /// If the path already exists in the PATH variable, it will not be added again.
    /// </summary>
    /// <param name="path">The path to append.</param>
    /// <param name="target">
    /// The target for the environment variable (e.g., process, user, machine).
    /// Default is <see cref="EnvironmentVariableTarget.Process"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///    Thrown when the <paramref name="path"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.AppendPath("/usr/local/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static void AppendPath(string path, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var paths = SplitPath(target);
        if (paths.Length > 0)
        {
            if (OperatingSystem.IsWindows() && paths[^1].EqualFold(path))
                return;

            if (paths[^1].Equals(path))
                return;
        }

        Array.Resize(ref paths, paths.Length + 1);
        paths[^1] = path;
        Set(OperatingSystem.IsWindows() ? "Path" : "PATH", JoinPath(paths), target);
    }

    /// <summary>
    /// Expands environment variables in a template string.
    /// </summary>
    /// <param name="template">
    /// The template string containing environment variables to expand.
    /// </param>
    /// <param name="options">
    /// The options to customize the expansion behavior.
    /// </param>
    /// <returns>
    /// A <see cref="System.ReadOnlySpan{T}"/> representing the expanded template.
    /// </returns>
    /// <exception cref="EnvironmentException">
    /// Thrown when the template contains invalid syntax.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var expanded = Env.Expand("${HOME}/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static string Expand(string template, EnvExpandOptions? options = null)
        => Expand(template.AsSpan(), options).ToString();

    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="target">The target for the environment variable.</param>
    /// <returns>The value of the environment variable, or <c>null</c> if not set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="variable"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the target is not valid on the os.</exception>
    /// <exception cref="System.Security.SecurityException">Thrown when the caller does not have permission to access the environment variable.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var home = Env.Get(Env.Keys.Home);
    /// Assert.NotNull(home);
    /// </code>
    /// </example>
    /// </remarks>
    public static string? Get(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        => Environment.GetEnvironmentVariable(variable, target);

    public static string GetOrDefault(string variable, string defaultValue, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        => Environment.GetEnvironmentVariable(variable, target) ?? defaultValue;

    /// <summary>
    /// Determines whether an environment variable is set.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="target">The target for the environment variable.</param>
    /// <returns><c>true</c> if the variable is set; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="variable"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the target is not valid on the os.</exception>
    /// <exception cref="System.Security.SecurityException">Thrown when the caller does not have permission to access the environment variable.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hasHome = Env.Has(Env.Keys.Home);
    /// Assert.True(hasHome);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool Has(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        => Environment.GetEnvironmentVariable(variable, target) is not null;

    /// <summary>
    /// Indicates whether the specified path exists in the environment PATH variable.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>
    /// <c>true</c> if the path exists in the environment PATH variable; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exists = Env.HasPath("/usr/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static bool HasPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var paths = SplitPath();
        return HasPath(path, paths);
    }

    /// <summary>
    /// Indicates whether the specified path exists in the environment PATH variable for a specific target.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="target">The target for the environment variable.</param>
    /// <returns><c>true</c> if the path exists; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exists = Env.HasPath("/usr/bin", EnvironmentVariableTarget.Process);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool HasPath(string path, EnvironmentVariableTarget target)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var paths = SplitPath(target);
        return HasPath(path, paths);
    }

    /// <summary>
    /// Indicates whether the specified path exists in the provided array of paths.
    /// </summary>
    /// <param name="path">
    /// The path to check against the array of paths.
    /// </param>
    /// <param name="paths">
    /// The array of paths to check against.
    /// </param>
    /// <returns><c>true</c> if the path exists in the array; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var exists = Env.HasPath("/usr/bin", new[] { "/usr/bin", "/usr/local/bin" });
    /// </code>
    /// </example>
    /// </remarks>
    public static bool HasPath(string path, string[] paths)
    {
        if (string.IsNullOrWhiteSpace(path) || paths is null || paths.Length == 0)
            return false;

        if (OperatingSystem.IsWindows())
        {
            foreach (var p in paths)
            {
                if (p.EqualsFold(path))
                    return true;
            }
        }
        else
        {
            foreach (var p in paths)
            {
                if (p.Equals(path))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Joins the specified paths into a single string, using the system's path separator.
    /// </summary>
    /// <param name="paths">
    /// The paths to join.
    /// </param>
    /// <returns>
    /// The joined string containing all the paths, separated by the system's path separator.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var joined = Env.JoinPath("/usr/bin", "/usr/local/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static string JoinPath(params string[] paths)
       => string.Join(System.IO.Path.PathSeparator.ToString(), paths);

    /// <summary>
    /// Prepends the specified path to the environment PATH variable.
    /// </summary>
    /// <param name="path">
    /// The path to prepend to the environment PATH variable.
    /// </param>
    /// <param name="target">
    /// The target for the environment variable (e.g., process, user, machine).
    /// Default is <see cref="EnvironmentVariableTarget.Process"/>.
    /// </param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.PrependPath("/opt/tools/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static void PrependPath(string path, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        var paths = SplitPath(target);
        if (paths.Length > 0)
        {
            if (OperatingSystem.IsWindows() && paths[0].EqualFold(path))
                return;

            if (paths[0].Equals(path))
                return;
        }

        var copy = new string[paths.Length + 1];
        Array.Copy(paths, 0, copy, 1, paths.Length);
        copy[0] = path;
        Set(OperatingSystem.IsWindows() ? "Path" : "PATH", JoinPath(copy), target);
    }

    /// <summary>
    /// Removes the specified path from the environment PATH variable.
    /// </summary>
    /// <param name="path">
    /// The path to remove from the environment PATH variable.
    /// </param>
    /// <param name="target">
    /// The target for the environment variable (e.g., process, user, machine).
    /// Default is <see cref="EnvironmentVariableTarget.Process"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="path"/> is null or empty.
    /// </exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.RemovePath("/opt/tools/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static void RemovePath(string path, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        var paths = SplitPath(target);
        var newPaths = new List<string>();

        foreach (var p in paths)
        {
            if (OperatingSystem.IsWindows() && p.EqualsFold(path))
                continue;

            if (p.Equals(path))
                continue;

            newPaths.Add(p);
        }

        if (newPaths.Count == paths.Length)
            return; // No change, path was not found.

        Set(OperatingSystem.IsWindows() ? "Path" : "PATH", JoinPath(newPaths.ToArray()), target);
    }

    /// <summary>
    /// Sets the value of an environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="target">The target for the environment variable.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.Set("MY_VAR", "my_value");
    /// Assert.Equal("my_value", Env.Get("MY_VAR"));
    /// </code>
    /// </example>
    /// </remarks>
    public static void Set(string variable, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        => Environment.SetEnvironmentVariable(variable, value, target);

    /// <summary>
    /// Splits the path as if it were an environment variable PATH.
    /// </summary>
    /// <param name="path">
    /// The path to split into an array of paths.
    /// </param>
    /// <returns>
    /// An array of strings representing the individual paths in the environment variable PATH.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parts = Env.SplitPath("/usr/bin:/usr/local/bin");
    /// </code>
    /// </example>
    /// </remarks>
    public static string[] SplitPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        return path.Split(new[] { System.IO.Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Splits the environment variable PATH into an array of paths.
    /// </summary>
    /// <param name="target">
    /// The target for the environment variable (e.g., process, user, machine).
    /// Default is <see cref="EnvironmentVariableTarget.Process"/>.
    /// </param>
    /// <returns>
    /// An array of strings representing the individual paths in the environment variable PATH.
    /// </returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parts = Env.SplitPath();
    /// </code>
    /// </example>
    /// </remarks>
    public static string[] SplitPath(EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        => SplitPath(Get(OperatingSystem.IsWindows() ? "Path" : "PATH", target) ?? string.Empty);

    public static bool TryGet(string variable, out string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        var result = Environment.GetEnvironmentVariable(variable, target);
        if (result is null)
        {
            value = string.Empty;
            return false;
        }

        value = result;
        return true;
    }

    /// <summary>
    /// Unsets the specified environment variable.
    /// </summary>
    /// <summary>
    /// Unsets an environment variable for the process target.
    /// </summary>
    /// <param name="variable">The name of the environment variable to unset.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.Set("MY_VAR", "value");
    /// Env.Unset("MY_VAR");
    /// Assert.Null(Env.Get("MY_VAR"));
    /// </code>
    /// </example>
    /// </remarks>
    public static void Unset(string variable)
        => Environment.SetEnvironmentVariable(variable, null);

    /// <summary>
    /// Unsets the specified environment variable.
    /// </summary>
    /// <param name="variable">
    /// The name of the environment variable to unset.
    /// </param>
    /// <param name="target">
    /// The target for the environment variable (e.g., process, user, machine).
    /// </param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Env.Unset("APP_MODE", EnvironmentVariableTarget.Process);
    /// </code>
    /// </example>
    /// </remarks>
    public static void Unset(string variable, EnvironmentVariableTarget target)
        => Environment.SetEnvironmentVariable(variable, null, target);

    /// <summary>
    /// Represents the Keys class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var instance = default(object);
    /// </code>
    /// </example>
    /// </remarks>
    public static class Keys
    {
        public static string Path => OperatingSystem.IsWindows() ? "Path" : "PATH";

        public static string Home => OperatingSystem.IsWindows() ? "USERPROFILE" : "HOME";

        public static string Temp => OperatingSystem.IsWindows() ? "TEMP" : "TMPDIR";

        public static string User => OperatingSystem.IsWindows() ? "USERNAME" : "USER";

        public static string Shell => OperatingSystem.IsWindows() ? "ComSpec" : "SHELL";

        public static string HomeConfig => OperatingSystem.IsWindows() ? "APPDATA" : "XDG_CONFIG_HOME";

        public static string HomeData => OperatingSystem.IsWindows() ? "LOCALAPPDATA" : "XDG_DATA_HOME";

        public static string HomeCache => OperatingSystem.IsWindows() ? "LOCALAPPDATA" : "XDG_CACHE_HOME";
    }
}