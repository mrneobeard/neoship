using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Resolves effective permissions for users, API keys, and service accounts.
/// </summary>
/// <example>
/// <code>
/// var resolver = new PermissionResolver(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)));
/// var permissions = await resolver.ResolveUserAsync(userId, token);
/// </code>
/// </example>
public sealed class PermissionResolver
{
    private readonly record struct ClaimPair(string Type, string Value);

    private readonly ShipDb db;
    private readonly PermissionClaimCodec codec;

    /// <summary>
    /// Initializes a new <see cref="PermissionResolver"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="codec">The permission claim codec.</param>
    public PermissionResolver(ShipDb db, PermissionClaimCodec codec)
    {
        this.db = db;
        this.codec = codec;
    }

    /// <summary>
    /// Resolves the effective permissions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The condensed permission set.</returns>
    public async Task<PermissionSet> ResolveUserAsync(Guid userId, CancellationToken ct = default)
    {
        var grants = new List<PermissionGrant>();

        var userOrgId = await this.db.Users
            .Where(x => x.Id == userId)
            .Select(x => x.OrgId)
            .FirstOrDefaultAsync(ct);

        var userClaims = await this.db.UserClaims
            .Where(x => x.UserId == userId)
            .Select(x => new ClaimPair(x.Type, x.Value ?? string.Empty))
            .ToListAsync(ct);

        this.AddClaims(grants, userClaims);

        var builtInAssignments = await this.db.RoleAssignments
            .Where(x => x.UserId == userId && x.OrgId == userOrgId)
            .Select(x => new { x.RoleKey, x.ScopeKind, x.ScopeId })
            .ToListAsync(ct);

        foreach (var assignment in builtInAssignments)
        {
            grants.AddRange(BuiltInRoleStore.GrantsFor(assignment.RoleKey, assignment.ScopeKind, assignment.ScopeId));
        }

        var userRoles = await this.db.Users
            .Where(x => x.Id == userId)
            .SelectMany(x => x.Roles.Where(r => r.OrgId == userOrgId))
            .SelectMany(x => x.Claims)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, userRoles);

        var userGroupIds = await this.db.Groups
            .Where(g => g.OrgId == userOrgId && (g.Members.Any(u => u.Id == userId) || g.Owners.Any(u => u.Id == userId)))
            .Select(g => g.Id)
            .ToListAsync(ct);

        var userGroups = await this.db.Groups
            .Where(g => userGroupIds.Contains(g.Id))
            .SelectMany(g => g.Roles)
            .SelectMany(r => r.Claims)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, userGroups);

        var builtInGroupAssignments = await this.db.RoleAssignments
            .Where(x => x.GroupId != null && userGroupIds.Contains(x.GroupId.Value))
            .Select(x => new { x.RoleKey, x.ScopeKind, x.ScopeId })
            .ToListAsync(ct);

        foreach (var assignment in builtInGroupAssignments)
        {
            grants.AddRange(BuiltInRoleStore.GrantsFor(assignment.RoleKey, assignment.ScopeKind, assignment.ScopeId));
        }

        return new PermissionSet(grants);
    }

    /// <summary>
    /// Resolves the effective permissions for a user API key.
    /// </summary>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The condensed permission set.</returns>
    public async Task<PermissionSet> ResolveUserApiKeyAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var grants = new List<PermissionGrant>();

        var keyClaims = await this.db.UserApiKeyClaims
            .Where(x => x.UserApiKeyId == apiKeyId)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, keyClaims);

        var userApiKey = await this.db.UserApiKeys
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, ct);

        if (userApiKey?.User is not null)
        {
            var userPermissions = await this.ResolveUserAsync(userApiKey.UserId, ct);
            grants.AddRange(userPermissions.All);
        }

        return new PermissionSet(grants);
    }

    /// <summary>
    /// Resolves the effective permissions for a service account.
    /// </summary>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The condensed permission set.</returns>
    public async Task<PermissionSet> ResolveServiceAccountAsync(Guid serviceAccountId, CancellationToken ct = default)
    {
        var grants = new List<PermissionGrant>();

        var orgId = await this.db.ServiceAccounts
            .Where(x => x.Id == serviceAccountId)
            .Select(x => x.OrgId)
            .FirstOrDefaultAsync(ct);

        var directClaims = await this.db.ServiceAccountClaims
            .Where(x => x.ServiceAccountId == serviceAccountId)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, directClaims);

        var serviceAccountGroupIds = await this.db.Groups
            .Where(g => g.OrgId == orgId && (g.ServiceAccountMembers.Any(sa => sa.Id == serviceAccountId) || g.ServiceAccountOwners.Any(sa => sa.Id == serviceAccountId)))
            .Select(g => g.Id)
            .ToListAsync(ct);

        var roleClaims = await this.db.Groups
            .Where(g => serviceAccountGroupIds.Contains(g.Id))
            .SelectMany(g => g.Roles)
            .SelectMany(r => r.Claims)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, roleClaims);

        var builtInGroupAssignments = await this.db.RoleAssignments
            .Where(x => x.GroupId != null && serviceAccountGroupIds.Contains(x.GroupId.Value))
            .Select(x => new { x.RoleKey, x.ScopeKind, x.ScopeId })
            .ToListAsync(ct);

        foreach (var assignment in builtInGroupAssignments)
        {
            grants.AddRange(BuiltInRoleStore.GrantsFor(assignment.RoleKey, assignment.ScopeKind, assignment.ScopeId));
        }

        return new PermissionSet(grants);
    }

    /// <summary>
    /// Resolves the effective permissions for a service account API key.
    /// </summary>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The condensed permission set.</returns>
    public async Task<PermissionSet> ResolveServiceAccountApiKeyAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        var grants = new List<PermissionGrant>();

        var keyClaims = await this.db.ServiceAccountApiKeyClaims
            .Where(x => x.ServiceAccountApiKeyId == apiKeyId)
            .Select(x => new ClaimPair(x.Type, x.Value))
            .ToListAsync(ct);

        this.AddClaims(grants, keyClaims);

        var apiKey = await this.db.ServiceAccountApiKeys
            .Include(x => x.ServiceAccount)
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, ct);

        if (apiKey?.ServiceAccount is not null)
        {
            var serviceAccountPermissions = await this.ResolveServiceAccountAsync(apiKey.ServiceAccountId, ct);
            grants.AddRange(serviceAccountPermissions.All);
        }

        return new PermissionSet(grants);
    }

    /// <summary>
    /// Checks whether a user has a permission at a requested scope.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="key">The permission key.</param>
    /// <param name="scopeKind">The requested scope kind.</param>
    /// <param name="scopeId">The optional scope identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when the permission is present; otherwise <see langword="false"/>.</returns>
    public async Task<bool> UserHasAsync(Guid userId, PermissionKey key, PermissionScopeKind scopeKind, string? scopeId = null, CancellationToken ct = default)
        => (await this.ResolveUserAsync(userId, ct)).Allows(key, scopeKind, scopeId);

    /// <summary>
    /// Checks whether a service account has a permission at a requested scope.
    /// </summary>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="key">The permission key.</param>
    /// <param name="scopeKind">The requested scope kind.</param>
    /// <param name="scopeId">The optional scope identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when the permission is present; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ServiceAccountHasAsync(Guid serviceAccountId, PermissionKey key, PermissionScopeKind scopeKind, string? scopeId = null, CancellationToken ct = default)
        => (await this.ResolveServiceAccountAsync(serviceAccountId, ct)).Allows(key, scopeKind, scopeId);

    private void AddClaims(List<PermissionGrant> grants, IEnumerable<ClaimPair> claims)
    {
        foreach (var claim in claims)
        {
            if (string.IsNullOrWhiteSpace(claim.Type) || string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            if (this.codec.TryDecode(claim.Type, claim.Value, out var grant))
            {
                grants.Add(grant);
            }
        }
    }
}
