namespace NeoShip.Data.Model;

/// <summary>
/// Represents a normalized permission key.
/// </summary>
/// <example>
/// <code>
/// var key = PermissionKey.Create("org.roles", "read");
/// var text = key.ToString();
/// </code>
/// </example>
public readonly record struct PermissionKey
{
    private const char Separator = '.';

    /// <summary>
    /// Initializes a new <see cref="PermissionKey"/> instance.
    /// </summary>
    /// <param name="resource">The permission resource segment.</param>
    /// <param name="action">The permission action segment.</param>
    /// <exception cref="ArgumentException">Thrown when either segment is blank.</exception>
    public PermissionKey(string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Permission resource cannot be blank.", nameof(resource));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Permission action cannot be blank.", nameof(action));
        }

        this.Resource = resource.Trim().ToLowerInvariant();
        this.Action = action.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the resource segment.
    /// </summary>
    public string Resource { get; init; }

    /// <summary>
    /// Gets the action segment.
    /// </summary>
    public string Action { get; init; }

    /// <summary>
    /// Creates a new permission key from resource and action segments.
    /// </summary>
    /// <param name="resource">The permission resource segment.</param>
    /// <param name="action">The permission action segment.</param>
    /// <returns>A normalized permission key.</returns>
    public static PermissionKey Create(string resource, string action) => new(resource, action);

    /// <summary>
    /// Parses a permission key from text.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <returns>The parsed permission key.</returns>
    /// <exception cref="FormatException">Thrown when the value is not in `resource.action` form.</exception>
    public static PermissionKey Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Permission key cannot be blank.");
        }

        var index = value.LastIndexOf(Separator);
        if (index <= 0 || index >= value.Length - 1)
        {
            throw new FormatException($"Invalid permission key: {value}");
        }

        return new PermissionKey(value[..index], value[(index + 1)..]);
    }

    /// <summary>
    /// Returns the canonical `resource.action` text form.
    /// </summary>
    /// <returns>The canonical permission key text.</returns>
    public override string ToString() => $"{this.Resource}{Separator}{this.Action}";
}
