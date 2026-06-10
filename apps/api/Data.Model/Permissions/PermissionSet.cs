namespace NeoShip.Data.Model;

/// <summary>
/// Represents a condensed set of scoped permission grants.
/// </summary>
/// <example>
/// <code>
/// var permissions = new PermissionSet(CorePermissions.All.Select(x =&gt; new PermissionGrant(x.Key, PermissionScopeKind.Global)));
/// var canRead = permissions.Allows(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization);
/// </code>
/// </example>
public sealed class PermissionSet
{
    private readonly HashSet<PermissionGrant> grants = new();

    /// <summary>
    /// Initializes a new <see cref="PermissionSet"/> instance.
    /// </summary>
    /// <param name="grants">The initial grants.</param>
    public PermissionSet(IEnumerable<PermissionGrant>? grants = null)
    {
        if (grants is not null)
        {
            this.AddRange(grants);
        }
    }

    /// <summary>
    /// Gets the current grants.
    /// </summary>
    public IEnumerable<PermissionGrant> All => this.grants;

    /// <summary>
    /// Adds a grant.
    /// </summary>
    /// <param name="grant">The grant to add.</param>
    public void Add(PermissionGrant grant) => this.grants.Add(grant);

    /// <summary>
    /// Adds multiple grants.
    /// </summary>
    /// <param name="grants">The grants to add.</param>
    public void AddRange(IEnumerable<PermissionGrant> grants)
    {
        foreach (var grant in grants)
        {
            this.Add(grant);
        }
    }

    /// <summary>
    /// Checks whether the set allows a permission at the requested scope.
    /// </summary>
    /// <param name="key">The permission key.</param>
    /// <param name="scopeKind">The requested scope kind.</param>
    /// <param name="scopeId">The optional requested scope identifier.</param>
    /// <returns><see langword="true"/> when any grant applies; otherwise <see langword="false"/>.</returns>
    public bool Allows(PermissionKey key, PermissionScopeKind scopeKind, string? scopeId = null)
        => this.grants.Any(grant => grant.Matches(key, scopeKind, scopeId));

    /// <summary>
    /// Returns grants for a permission key.
    /// </summary>
    /// <param name="key">The permission key.</param>
    /// <returns>The matching grants.</returns>
    public IEnumerable<PermissionGrant> ForKey(PermissionKey key) => this.grants.Where(grant => grant.Key == key);

    /// <summary>
    /// Returns grants for a resource prefix.
    /// </summary>
    /// <param name="resource">The resource prefix.</param>
    /// <returns>The matching grants.</returns>
    public IEnumerable<PermissionGrant> ForResource(string resource)
    {
        var normalizedResource = new PermissionKey(resource, "read").Resource;
        return this.grants.Where(grant => grant.Key.Resource == normalizedResource);
    }

    /// <summary>
    /// Returns grants for a scope.
    /// </summary>
    /// <param name="scopeKind">The scope kind.</param>
    /// <param name="scopeId">The optional scope identifier.</param>
    /// <returns>The matching grants.</returns>
    public IEnumerable<PermissionGrant> ForScope(PermissionScopeKind scopeKind, string? scopeId = null)
        => this.grants.Where(grant => grant.MatchesScope(scopeKind, scopeId));
}