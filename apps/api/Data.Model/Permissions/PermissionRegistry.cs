namespace NeoShip.Data.Model;

/// <summary>
/// Stores permission definitions contributed by the core app and modules.
/// </summary>
/// <example>
/// <code>
/// var registry = new PermissionRegistry(CorePermissions.All);
/// var permission = registry[PermissionKey.Create("org.roles", "read")];
/// </code>
/// </example>
public sealed class PermissionRegistry
{
    private readonly Dictionary<PermissionKey, PermissionDefinition> definitions = new();

    /// <summary>
    /// Initializes a new <see cref="PermissionRegistry"/> instance.
    /// </summary>
    /// <param name="definitions">The initial permission definitions.</param>
    public PermissionRegistry(IEnumerable<PermissionDefinition>? definitions = null)
    {
        if (definitions is not null)
        {
            this.RegisterRange(definitions);
        }
    }

    /// <summary>
    /// Gets the registered permission definitions.
    /// </summary>
    public IEnumerable<PermissionDefinition> All => this.definitions.Values;

    /// <summary>
    /// Gets a permission definition by key.
    /// </summary>
    /// <param name="key">The permission key.</param>
    /// <returns>The matching permission definition.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not registered.</exception>
    public PermissionDefinition this[PermissionKey key] => this.definitions[key];

    /// <summary>
    /// Registers a permission definition.
    /// </summary>
    /// <param name="definition">The definition to register.</param>
    /// <exception cref="InvalidOperationException">Thrown when the key is already registered.</exception>
    public void Register(PermissionDefinition definition)
    {
        if (!this.definitions.TryAdd(definition.Key, definition))
        {
            throw new InvalidOperationException($"Permission already registered: {definition.Key}");
        }
    }

    /// <summary>
    /// Registers multiple permission definitions.
    /// </summary>
    /// <param name="definitions">The definitions to register.</param>
    public void RegisterRange(IEnumerable<PermissionDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            this.Register(definition);
        }
    }

    /// <summary>
    /// Tries to get a permission definition by key.
    /// </summary>
    /// <param name="key">The permission key.</param>
    /// <param name="definition">The matching definition when found.</param>
    /// <returns><see langword="true"/> when a definition exists; otherwise <see langword="false"/>.</returns>
    public bool TryGet(PermissionKey key, out PermissionDefinition? definition) => this.definitions.TryGetValue(key, out definition);
}
