namespace NeoBeard.Exec;

/// <summary>
/// Provides hints for locating an executable on the system path.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hint = new PathHint("node")
/// {
///     Windows = { @"C:\Program Files\nodejs\node.exe" },
///     Linux = { "/usr/bin/node" },
///     Darwin = { "/usr/local/bin/node" }
/// };
/// PathFinder.Default.Register("node", hint);
/// </code>
/// </example>
/// </remarks>
public class PathHint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathHint"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the executable to locate.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("python");
    /// Assert.Equal("python", hint.Name);
    /// </code>
    /// </example>
    /// </remarks>
    public PathHint(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets the name of the executable to locate.
    /// </summary>
    /// <value>The name of the executable.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("dotnet");
    /// Assert.Equal("dotnet", hint.Name);
    /// </code>
    /// </example>
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the explicit executable path if known.
    /// </summary>
    /// <value>The path to the executable, or <c>null</c> if not explicitly set.</value>
    public string? Executable { get; set; }

    /// <summary>
    /// Gets or sets the environment variable name to check for the executable path.
    /// </summary>
    /// <value>The environment variable name, or <c>null</c> if not set.</value>
    public string? Variable { get; set; }

    /// <summary>
    /// Gets or sets the cached path to the executable.
    /// </summary>
    /// <value>The cached path, or <c>null</c> if not cached.</value>
    public string? CachedPath { get; set; }

    /// <summary>
    /// Gets or sets the paths to search in for Windows.
    /// </summary>
    /// <value>A set of Windows-specific paths to search.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("test");
    /// hint.Windows.Add("C:\\Tools\\test.exe");
    /// Assert.Single(hint.Windows);
    /// </code>
    /// </example>
    /// </remarks>
    public HashSet<string> Windows { get; set; } = new();

    /// <summary>
    /// Gets or sets the paths to search in for Linux.
    /// </summary>
    /// <value>A set of Linux-specific paths to search.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("test");
    /// hint.Linux.Add("/usr/bin/test");
    /// Assert.Single(hint.Linux);
    /// </code>
    /// </example>
    /// </remarks>
    public HashSet<string> Linux { get; set; } = new();

    /// <summary>
    /// Gets or sets the paths to search in for macOS (Darwin).
    /// </summary>
    /// <value>A set of macOS-specific paths to search.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hint = new PathHint("test");
    /// hint.Darwin.Add("/usr/local/bin/test");
    /// Assert.Single(hint.Darwin);
    /// </code>
    /// </example>
    /// </remarks>
    public HashSet<string> Darwin { get; set; } = new();
}