using System.Collections.ObjectModel;

namespace NeoShip.Data.Model;

/// <summary>
/// Describes a permission available to the system.
/// </summary>
/// <example>
/// <code>
/// var def = new PermissionDefinition(
///     PermissionKey.Create("org.roles", "read"),
///     "Read roles",
///     PermissionScopeKind.Organization);
/// </code>
/// </example>
public sealed class PermissionDefinition
{
    /// <summary>
    /// Initializes a new <see cref="PermissionDefinition"/> instance.
    /// </summary>
    /// <param name="key">The canonical permission key.</param>
    /// <param name="description">The human-readable description.</param>
    /// <param name="allowedScopes">The scope kinds the permission supports.</param>
    /// <exception cref="ArgumentException">Thrown when no scope kinds are supplied.</exception>
    public PermissionDefinition(PermissionKey key, string description, params PermissionScopeKind[] allowedScopes)
    {
        if (allowedScopes is null || allowedScopes.Length == 0)
        {
            throw new ArgumentException("At least one allowed scope is required.", nameof(allowedScopes));
        }

        this.Key = key;
        this.Description = description.Trim();
        this.AllowedScopes = Array.AsReadOnly(allowedScopes.Distinct().ToArray());
    }

    /// <summary>
    /// Gets the canonical permission key.
    /// </summary>
    public PermissionKey Key { get; }

    /// <summary>
    /// Gets the human-readable description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the allowed scope kinds for this permission.
    /// </summary>
    public ReadOnlyCollection<PermissionScopeKind> AllowedScopes { get; }

    /// <summary>
    /// Determines whether the permission supports the provided scope kind.
    /// </summary>
    /// <param name="scopeKind">The scope kind to test.</param>
    /// <returns><see langword="true"/> when the permission allows the scope kind; otherwise <see langword="false"/>.</returns>
    public bool Allows(PermissionScopeKind scopeKind) => this.AllowedScopes.Contains(scopeKind);
}