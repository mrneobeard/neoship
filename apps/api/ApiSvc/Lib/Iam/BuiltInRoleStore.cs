using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Lib.Iam;

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
    /// The built-in owner role identifier.
    /// </summary>
    public static readonly Guid OwnerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    /// <summary>
    /// The built-in admin role identifier.
    /// </summary>
    public static readonly Guid AdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000102");

    /// <summary>
    /// The built-in editor role identifier.
    /// </summary>
    public static readonly Guid EditorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000103");

    /// <summary>
    /// The built-in reader role identifier.
    /// </summary>
    public static readonly Guid ReaderRoleId = Guid.Parse("00000000-0000-0000-0000-000000000104");

    /// <summary>
    /// The built-in auditor role identifier.
    /// </summary>
    public static readonly Guid AuditorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000105");

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

    private static readonly IReadOnlyList<BuiltInRoleDefinition> RoleDefinitions =
    [
        new(OwnerRoleId, OwnerRoleName, "Owner", "Full organization ownership."),
        new(AdminRoleId, AdminRoleName, "Admin", "Organization administration without ownership transfer."),
        new(EditorRoleId, EditorRoleName, "Editor", "Read and write access without organization settings changes."),
        new(ReaderRoleId, ReaderRoleName, "Reader", "Default organization read access."),
        new(AuditorRoleId, AuditorRoleName, "Auditor", "Read-only audit and inspection access."),
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<PermissionKey>> Roles = new Dictionary<string, IReadOnlyCollection<PermissionKey>>(StringComparer.OrdinalIgnoreCase)
    {
        [OwnerRoleName] = OwnerPermissions().ToArray(),
        [AdminRoleName] = AdminPermissions().ToArray(),
        [EditorRoleName] = EditorPermissions().ToArray(),
        [ReaderRoleName] = ReaderPermissions().ToArray(),
        [AuditorRoleName] = AuditorPermissions().ToArray(),
    };

    /// <summary>
    /// Gets the code-owned built-in role definitions.
    /// </summary>
    /// <returns>The built-in role definitions.</returns>
    public static IReadOnlyList<BuiltInRoleDefinition> Definitions() => RoleDefinitions;

    /// <summary>
    /// Gets a code-owned built-in role by identifier.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <returns>The matching built-in role definition, or <see langword="null"/>.</returns>
    public static BuiltInRoleDefinition? FindById(Guid roleId) => RoleDefinitions.FirstOrDefault(x => x.Id == roleId);

    /// <summary>
    /// Checks whether a role name is reserved for a built-in role.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <returns><see langword="true"/> when the name is built-in; otherwise <see langword="false"/>.</returns>
    public static bool IsBuiltInRoleName(string roleName) => Roles.ContainsKey(NormalizeRoleKey(roleName));

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
        => await AssignAsync(db, orgId, orgSlug, userId, roleName, userId, ct);

    /// <summary>
    /// Assigns a built-in role to a user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="createdBy">The creator user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task AssignAsync(ShipDb db, Guid orgId, string orgSlug, Guid userId, string roleName, Guid createdBy, CancellationToken ct = default)
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
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Removes a built-in role assignment from a user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when an assignment was removed; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> UnassignAsync(ShipDb db, Guid orgId, string orgSlug, Guid userId, string roleName, CancellationToken ct = default)
    {
        var roleKey = NormalizeRoleKey(roleName);
        if (!Roles.ContainsKey(roleKey))
        {
            throw new ArgumentException("Unknown built-in role.", nameof(roleName));
        }

        var scopeId = NormalizeScopeId(orgSlug);
        var assignment = await db.RoleAssignments.FirstOrDefaultAsync(
            x => x.OrgId == orgId
                && x.UserId == userId
                && x.RoleKey == roleKey
                && x.ScopeKind == PermissionScopeKind.Organization
                && x.ScopeId == scopeId,
            ct);
        if (assignment is null)
        {
            return false;
        }

        db.RoleAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Assigns a built-in role to a group.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="createdBy">The creator user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when the group exists; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> AssignGroupAsync(ShipDb db, Guid orgId, string orgSlug, Guid groupId, string roleName, Guid createdBy, CancellationToken ct = default)
    {
        var roleKey = NormalizeRoleKey(roleName);
        if (!Roles.ContainsKey(roleKey))
        {
            throw new ArgumentException("Unknown built-in role.", nameof(roleName));
        }

        var groupExists = await db.Groups.AnyAsync(x => x.Id == groupId && x.OrgId == orgId, ct);
        if (!groupExists)
        {
            return false;
        }

        var scopeId = NormalizeScopeId(orgSlug);
        var exists = await db.RoleAssignments.AnyAsync(
            x => x.OrgId == orgId
                && x.GroupId == groupId
                && x.RoleKey == roleKey
                && x.ScopeKind == PermissionScopeKind.Organization
                && x.ScopeId == scopeId,
            ct);
        if (exists)
        {
            return true;
        }

        db.RoleAssignments.Add(new RoleAssignment
        {
            Id = Factory.NewGuid(),
            OrgId = orgId,
            GroupId = groupId,
            RoleKey = roleKey,
            ScopeKind = PermissionScopeKind.Organization,
            ScopeId = scopeId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Removes a built-in role assignment from a group.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="roleName">The built-in role key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when an assignment was removed; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> UnassignGroupAsync(ShipDb db, Guid orgId, string orgSlug, Guid groupId, string roleName, CancellationToken ct = default)
    {
        var roleKey = NormalizeRoleKey(roleName);
        if (!Roles.ContainsKey(roleKey))
        {
            throw new ArgumentException("Unknown built-in role.", nameof(roleName));
        }

        var scopeId = NormalizeScopeId(orgSlug);
        var assignment = await db.RoleAssignments.FirstOrDefaultAsync(
            x => x.OrgId == orgId
                && x.GroupId == groupId
                && x.RoleKey == roleKey
                && x.ScopeKind == PermissionScopeKind.Organization
                && x.ScopeId == scopeId,
            ct);
        if (assignment is null)
        {
            return false;
        }

        db.RoleAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return true;
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

/// <summary>
/// Describes a code-owned built-in role.
/// </summary>
/// <param name="Id">The stable built-in role identifier.</param>
/// <param name="Key">The built-in role key.</param>
/// <param name="Name">The display name.</param>
/// <param name="Description">The role description.</param>
/// <example>
/// <code>
/// var owner = BuiltInRoleStore.Definitions().First(x =&gt; x.Key == BuiltInRoleStore.OwnerRoleName);
/// </code>
/// </example>
public sealed record BuiltInRoleDefinition(Guid Id, string Key, string Name, string Description);
