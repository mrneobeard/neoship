using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NeoBeard.Exec;

/// <summary>
/// Provides functionality for locating and caching executable paths on the system.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// // Find an executable on the PATH
/// var path = PathFinder.Which("dotnet");
/// Console.WriteLine($"Found dotnet at: {path}");
///
/// // Use the default PathFinder instance
/// var nodePath = PathFinder.Default.Find("node");
/// if (nodePath != null)
/// {
///     Console.WriteLine($"Node.js found at: {nodePath}");
/// }
/// </code>
/// </example>
/// </remarks>
public class PathFinder
{
    private static readonly ConcurrentDictionary<string, string> ExecutableLocationCache = new();

    private readonly ConcurrentDictionary<string, PathHint> entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the default <see cref="PathFinder"/> instance for the application.
    /// </summary>
    /// <value>The default shared instance of <see cref="PathFinder"/>.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var pythonPath = PathFinder.Default.Find("python");
    /// if (pythonPath != null)
    /// {
    ///     Console.WriteLine($"Python found at: {pythonPath}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static PathFinder Default { get; } = new();

    /// <summary>
    /// Provides the equivalent functionality of which/where on Windows and Linux to
    /// determine the full path of an executable found on the PATH.
    /// </summary>
    /// <param name="command">The command to search for.</param>
    /// <param name="prependPaths">Additional paths that will be used to find the executable.</param>
    /// <param name="useCache">Should the lookup use the cached values.</param>
    /// <returns>Null or the full path of the command.</returns>
    /// <exception cref="ArgumentNullException">Throws when command is null.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var path = PathFinder.Which("dotnet");
    /// Console.WriteLine($"Found at: {path}");
    ///
    /// // Search with custom paths prepended
    /// path = PathFinder.Which("node", prependPaths: ["/custom/path"]);
    /// </code>
    /// </example>
    /// </remarks>
    public static string? Which(
        string command,
        IEnumerable<string>? prependPaths = null,
        bool useCache = true)
    {
        // https://github.com/actions/runner/blob/592ce1b230985aea359acdf6ed4ee84307bbedc1/src/Runner.Sdk/Util/WhichUtil.cs
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentNullException(nameof(command));

        var rootName = Path.GetFileNameWithoutExtension(command);
        if (useCache && ExecutableLocationCache.TryGetValue(rootName, out var location))
            return location;

#if NETLEGACY
        if (Path.IsPathRooted(command) && File.Exists(command))
        {
            ExecutableLocationCache[command] = command;
            ExecutableLocationCache[rootName] = command;

            return command;
        }
#else
        if (Path.IsPathFullyQualified(command) && File.Exists(command))
        {
            ExecutableLocationCache[command] = command;
            ExecutableLocationCache[rootName] = command;

            return command;
        }
#endif

        var pathSegments = new List<string>();
        if (prependPaths is not null)
            pathSegments.AddRange(prependPaths);

        pathSegments.AddRange(Env.SplitPath());

        for (var i = 0; i < pathSegments.Count; i++)
        {
            pathSegments[i] = Env.Expand(pathSegments[i]);
        }

        foreach (var pathSegment in pathSegments)
        {
            if (string.IsNullOrEmpty(pathSegment) || !System.IO.Directory.Exists(pathSegment))
                continue;

            IEnumerable<string> matches = Array.Empty<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var pathExt = Env.GetOrDefault("PATHEXT", string.Empty);
                if (pathExt.IsNullOrWhiteSpace())
                {
                    // XP's system default value for PATHEXT system variable
                    pathExt = ".com;.exe;.bat;.cmd;.vbs;.vbe;.js;.jse;.wsf;.wsh";
                }

                var pathExtSegments = pathExt.ToLowerInvariant()
                                                .Split(
                                                    [";"],
                                                    StringSplitOptions.RemoveEmptyEntries);

                // if command already has an extension.
                if (pathExtSegments.Any(x => command.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        matches = System.IO.Directory.EnumerateFiles(pathSegment, command);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }

                    var result = matches.FirstOrDefault();
                    if (result is null)
                        continue;

                    ExecutableLocationCache[rootName] = result;
                    return result;
                }
                else
                {
                    string searchPattern = $"{command}.*";
                    try
                    {
                        matches = System.IO.Directory.EnumerateFiles(pathSegment, searchPattern);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }

                    var expandedPath = Path.Combine(pathSegment, command);

                    foreach (var match in matches)
                    {
                        foreach (var ext in pathExtSegments)
                        {
                            var fullPath = expandedPath + ext;
                            if (!match.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            ExecutableLocationCache[rootName] = fullPath;
                            return fullPath;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    matches = System.IO.Directory.EnumerateFiles(pathSegment, command);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                var result = matches.FirstOrDefault();
                if (result is null)
                    continue;

                ExecutableLocationCache[rootName] = result;
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets or sets the <see cref="PathHint"/> for the specified executable name.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <value>The <see cref="PathHint"/> for the executable, or <c>null</c> if not registered.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("node");
    /// PathFinder.Default["node"] = hint;
    ///
    /// var retrieved = PathFinder.Default["node"];
    /// Assert.Same(hint, retrieved);
    /// </code>
    /// </example>
    /// </remarks>
    public PathHint? this[string name]
    {
        get => this.entries.TryGetValue(name, out var entry) ? entry : null;
        set
        {
            if (value is null)
                this.entries.TryRemove(name, out _);
            else
                this.entries[name] = value;
        }
    }

    /// <summary>
    /// Registers a <see cref="PathHint"/> for the specified executable name.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <param name="entry">The <see cref="PathHint"/> containing path information.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("python")
    /// {
    ///     Variable = "PYTHON_PATH"
    /// };
    /// PathFinder.Default.Register("python", hint);
    /// Assert.True(PathFinder.Default.Has("python"));
    /// </code>
    /// </example>
    /// </remarks>
    public void Register(string name, PathHint entry)
    {
        this.entries[name] = entry;
        if (entry.Variable.IsNullOrWhiteSpace())
        {
            entry.Variable = name.ScreamingSnakeCase() + "_PATH";
        }
    }

    /// <summary>
    /// Registers a <see cref="PathHint"/> using a factory function if not already registered.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <param name="factory">A factory function that creates the <see cref="PathHint"/>.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// PathFinder.Default.Register("node", () => new PathHint("node")
    /// {
    ///     Windows = { @"C:\Program Files\nodejs\node.exe" }
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public void Register(string name, Func<PathHint> factory)
    {
        if (!this.entries.TryGetValue(name, out _))
        {
            this.entries[name] = factory();
        }
    }

    /// <summary>
    /// Registers a new <see cref="PathHint"/> or updates an existing one.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <param name="update">An action that updates the <see cref="PathHint"/>.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// PathFinder.Default.RegisterOrUpdate("python", hint =>
    /// {
    ///     hint.Windows.Add(@"C:\Python39\python.exe");
    ///     hint.Linux.Add("/usr/bin/python3");
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public void RegisterOrUpdate(string name, Action<PathHint> update)
    {
        if (!this.entries.TryGetValue(name, out var entry))
        {
            entry = new PathHint(name);
            this.Register(name, entry);
        }

        update(entry);
    }

    /// <summary>
    /// Updates an existing <see cref="PathHint"/> for the specified executable name.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <param name="update">An action that updates the <see cref="PathHint"/>.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// PathFinder.Default.Register("node", new PathHint("node"));
    /// PathFinder.Default.Update("node", hint => hint.Executable = "node.exe");
    /// </code>
    /// </example>
    /// </remarks>
    public void Update(string name, Action<PathHint> update)
    {
        if (this.entries.TryGetValue(name, out var entry))
        {
            update(entry);
        }
    }

    /// <summary>
    /// Determines whether a <see cref="PathHint"/> is registered for the specified executable name.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns><c>true</c> if a <see cref="PathHint"/> is registered; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.False(PathFinder.Default.Has("nonexistent-tool"));
    /// PathFinder.Default.Register("nonexistent-tool", new PathHint("nonexistent-tool"));
    /// Assert.True(PathFinder.Default.Has("nonexistent-tool"));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Has(string name)
    {
        return this.entries.ContainsKey(name);
    }

    /// <summary>
    /// Finds the full path to the specified executable, throwing if not found.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>The full path to the executable.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the executable cannot be found on the PATH.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var dotnetPath = PathFinder.Default.FindOrThrow("dotnet");
    /// Console.WriteLine($"Dotnet is at: {dotnetPath}");
    /// </code>
    /// </example>
    /// </remarks>
    public string FindOrThrow(string name)
    {
        var path = this.Find(name);
        if (path is null)
            throw new FileNotFoundException($"Could not find {name} on the PATH.");

        return path;
    }

    /// <summary>
    /// Finds the full path to the specified executable.
    /// </summary>
    /// <param name="name">The name or path of the executable.</param>
    /// <returns>The full path to the executable, or <c>null</c> if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var nodePath = PathFinder.Default.Find("node");
    /// if (nodePath != null)
    /// {
    ///     Console.WriteLine($"Found node at: {nodePath}");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public string? Find(string name)
    {
#if NET5_0_OR_GREATER
        if (Path.IsPathFullyQualified(name))
            return name;
#else
        if (Path.IsPathRooted(name))
            return name;
#endif
        var entry = this[name];
        if (entry is null)
        {
            entry = new PathHint(name);
            this.Register(name, entry);
        }

        if (!entry.Variable.IsNullOrWhiteSpace())
        {
            if (Env.TryGet(entry.Variable, out var envPath))
            {
                if (!entry.CachedPath.IsNullOrWhiteSpace() && envPath == entry.CachedPath)
                    return envPath;

                var path = Env.Expand(envPath);
                if (path.Length > 0)
                {
                    path = Path.GetFullPath(path);
                    if (!entry.CachedPath.IsNullOrWhiteSpace() && entry.CachedPath == envPath)
                        return envPath;

                    var tmp = Which(path);
                    if (tmp is not null)
                    {
                        entry.CachedPath = tmp;
                        return tmp;
                    }
                }
            }
        }

        if (!entry.CachedPath.IsNullOrWhiteSpace())
            return entry.CachedPath;

        var exe = entry.Executable ?? name;
        exe = Which(exe);
        if (exe is not null)
        {
            entry.Executable = Path.GetFileName(exe);
            entry.CachedPath = exe;
            return exe;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var attempt in entry.Windows)
            {
                exe = attempt;
                exe = Env.Expand(exe);
                exe = Which(exe);
                if (exe is null)
                {
                    continue;
                }

                entry.Executable = Path.GetFileName(exe);
                entry.CachedPath = exe;
                return exe;
            }

            return null;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var attempt in entry.Darwin)
            {
                exe = attempt;
                exe = Env.Expand(exe);
                exe = Which(exe);
                if (exe is null)
                {
                    continue;
                }

                entry.Executable = Path.GetFileName(exe);
                entry.CachedPath = exe;
                return exe;
            }
        }

        foreach (var attempt in entry.Linux)
        {
            exe = attempt;
            exe = Env.Expand(exe);
            exe = Which(exe);
            if (exe is null)
            {
                continue;
            }

            entry.Executable = Path.GetFileName(exe);
            entry.CachedPath = exe;
            return exe;
        }

        return null;
    }
}