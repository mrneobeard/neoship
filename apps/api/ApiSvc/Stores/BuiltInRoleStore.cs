using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Assigns and resolves code-owned organization roles.
/// </summary>
/// <example>
/// <code>
/// await BuiltInRoleStore.AssignAsync(db, org.Id, org.Slug, user.Id, BuiltInRoleStore.OwnerRoleName, ct);
/// </code>
/// </example>
public static class BuiltInRoleStore
{
    /// <summary>
    /// The built-in owner role key.
    /// </summary>
    public const string OwnerRoleName = "owner";

    /// <summary>
    /// The built-in admin role key.
    /// </summary>
    public const string AdminRoleName = "admin";

    /// <summary>
    /// The built-in editor role key.
    /// </summary>
    public const string EditorRoleName = "editor";

    /// <summary>
    /// The built-in reader role key.
    /// </summary>
    public const string ReaderRoleName = "reader";

    /// <summary>
    /// The built-in auditor role key.
    /// </summary>
    public const string AuditorRoleName = "auditor";

    /// <summary>
    /// The legacy built-in member role key.
    /// </summary>
    public const string MemberRoleName = ReaderRoleName;

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<PermissionKey>> Roles = new Dictionary<string, IReadOnlyCollection<PermissionKey>>(StringComparer.OrdinalIgnoreCase)
    {
        [OwnerRoleName] = OwnerPermissions().ToArray(),
        [AdminRoleName] = AdminPermissions().ToArray(),
        [EditorRoleName] = EditorPermissions().ToArray(),
        [ReaderRoleName] = ReaderPermissions().ToArray(),
        [AuditorRoleName] = AuditorPermissions().ToArray(),
    };

    /// <summary>
    /// Assigns a built-in role to a user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task AssignAsync(ShipDb db, Guid orgId, string orgSlug, Guid userId, string roleName, CancellationToken ct = default)
    {
        var roleKey = NormalizeRoleKey(roleName);
        if (!Roles.ContainsKey(roleKey))
        {
            throw new ArgumentException("Unknown built-in role.", nameof(roleName));
        }

        var scopeId = NormalizeScopeId(orgSlug);
        var exists = await db.RoleAssignments.AnyAsync(
            x => x.OrgId == orgId
                && x.UserId == userId
                && x.RoleKey == roleKey
                && x.ScopeKind == PermissionScopeKind.Organization
                && x.ScopeId == scopeId,
            ct);
        if (exists)
        {
            return;
        }

        var userExists = db.Users.Local.Any(x => x.Id == userId) || await db.Users.AnyAsync(x => x.Id == userId, ct);
        if (!userExists)
        {
            return;
        }

        db.RoleAssignments.Add(new RoleAssignment
        {
            Id = Factory.NewGuid(),
            OrgId = orgId,
            UserId = userId,
            RoleKey = roleKey,
            ScopeKind = PermissionScopeKind.Organization,
            ScopeId = scopeId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Gets grants for a built-in role at a scope.
    /// </summary>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="scopeKind">The scope kind.</param>
    /// <param name="scopeId">The optional scope identifier.</param>
    /// <returns>The role's permission grants, or an empty sequence for unknown roles.</returns>
    public static IEnumerable<PermissionGrant> GrantsFor(string roleName, PermissionScopeKind scopeKind, string? scopeId)
    {
        var roleKey = NormalizeRoleKey(roleName);
        if (!Roles.TryGetValue(roleKey, out var permissions))
        {
            return [];
        }

        return permissions.Select(x => new PermissionGrant(x, scopeKind, scopeId));
    }

    private static string NormalizeRoleKey(string roleName) => roleName.Trim().ToLowerInvariant() is "member" ? ReaderRoleName : roleName.Trim().ToLowerInvariant();

    private static string NormalizeScopeId(string orgSlug) => orgSlug.Trim().ToLowerInvariant();

    private static IEnumerable<PermissionKey> OwnerPermissions()
        => CorePermissions.All.Where(x => x.AllowedScopes.Contains(PermissionScopeKind.Organization)).Select(x => x.Key);

    private static IEnumerable<PermissionKey> AdminPermissions()
        => OwnerPermissions().Where(x => x != PermissionKey.Create("org.settings", "write"));

    private static IEnumerable<PermissionKey> EditorPermissions()
        => OwnerPermissions().Where(x => x.Action is "read" or "write" && x.Resource != "org.settings");

    private static IEnumerable<PermissionKey> ReaderPermissions()
        => OwnerPermissions().Where(x => x.Action == "read" && x.Resource is "org.settings" or "org.members");

    private static IEnumerable<PermissionKey> AuditorPermissions()
        => OwnerPermissions().Where(x => x.Action == "read");
}
