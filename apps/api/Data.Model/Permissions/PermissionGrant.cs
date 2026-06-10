namespace NeoShip.Data.Model;

/// <summary>
/// Represents a scoped permission grant.
/// </summary>
/// <example>
/// <code>
/// var grant = new PermissionGrant(
///     PermissionKey.Create("org.roles", "write"),
///     PermissionScopeKind.Organization,
///     "org_123");
/// </code>
/// </example>
public readonly record struct PermissionGrant
{
    /// <summary>
    /// Initializes a new <see cref="PermissionGrant"/> instance.
    /// </summary>
    /// <param name="key">The permission key.</param>
    /// <param name="scopeKind">The grant scope kind.</param>
    /// <param name="scopeId">The optional scope identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the scope kind is not set.</exception>
    public PermissionGrant(PermissionKey key, PermissionScopeKind scopeKind, string? scopeId = null)
    {
        if (scopeKind == PermissionScopeKind.Unknown)
        {
            throw new ArgumentException("Permission scope kind must be set.", nameof(scopeKind));
        }

        this.Key = key;
        this.ScopeKind = scopeKind;
        this.ScopeId = string.IsNullOrWhiteSpace(scopeId) ? null : scopeId.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the permission key.
    /// </summary>
    public PermissionKey Key { get; init; }

    /// <summary>
    /// Gets the grant scope kind.
    /// </summary>
    public PermissionScopeKind ScopeKind { get; init; }

    /// <summary>
    /// Gets the optional scope identifier.
    /// </summary>
    public string? ScopeId { get; init; }

    /// <summary>
    /// Checks whether the grant matches a permission and scope.
    /// </summary>
    /// <param name="key">The permission key to test.</param>
    /// <param name="scopeKind">The scope kind to test.</param>
    /// <param name="scopeId">The optional scope identifier to test.</param>
    /// <returns><see langword="true"/> when the grant applies; otherwise <see langword="false"/>.</returns>
    public bool Matches(PermissionKey key, PermissionScopeKind scopeKind, string? scopeId = null)
    {
        if (this.Key != key)
        {
            return false;
        }

        if (this.ScopeKind == PermissionScopeKind.Global)
        {
            return true;
        }

        if (this.ScopeKind != scopeKind)
        {
            return false;
        }

        if (this.ScopeId is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return false;
        }

        return string.Equals(this.ScopeId, scopeId.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks whether the grant matches only a scope.
    /// </summary>
    /// <param name="scopeKind">The scope kind to test.</param>
    /// <param name="scopeId">The optional scope identifier to test.</param>
    /// <returns><see langword="true"/> when the scope applies; otherwise <see langword="false"/>.</returns>
    public bool MatchesScope(PermissionScopeKind scopeKind, string? scopeId = null)
    {
        if (this.ScopeKind == PermissionScopeKind.Global)
        {
            return true;
        }

        if (this.ScopeKind != scopeKind)
        {
            return false;
        }

        if (this.ScopeId is null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(scopeId))
        {
            return false;
        }

        return string.Equals(this.ScopeId, scopeId.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }
}