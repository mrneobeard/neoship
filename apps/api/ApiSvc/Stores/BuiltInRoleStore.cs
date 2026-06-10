using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Seeds and assigns built-in organization roles.
/// </summary>
/// <example>
/// <code>
/// var roles = await BuiltInRoleStore.EnsureAsync(db, org.Id, org.Slug, user.Id, ct);
/// </code>
/// </example>
public static class BuiltInRoleStore
{
    /// <summary>
    /// The built-in owner role name.
    /// </summary>
    public const string OwnerRoleName = "Owner";

    /// <summary>
    /// The built-in admin role name.
    /// </summary>
    public const string AdminRoleName = "Admin";

    /// <summary>
    /// The built-in member role name.
    /// </summary>
    public const string MemberRoleName = "Member";

    /// <summary>
    /// Ensures the built-in organization roles and claims exist.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="createdBy">The creator user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The built-in <see cref="Role"/> values keyed by role name.</returns>
    public static async Task<IReadOnlyDictionary<string, Role>> EnsureAsync(ShipDb db, Guid orgId, string orgSlug, Guid createdBy, CancellationToken ct = default)
    {
        var roles = await db.Roles
            .Include(x => x.Claims)
            .Where(x => x.OrgId == orgId && (x.NameUpcase == "OWNER" || x.NameUpcase == "ADMIN" || x.NameUpcase == "MEMBER"))
            .ToDictionaryAsync(x => x.NameUpcase, ct);

        var owner = EnsureRole(db, roles, orgId, OwnerRoleName, "Full organization ownership.", createdBy);
        var admin = EnsureRole(db, roles, orgId, AdminRoleName, "Organization administration without ownership transfer.", createdBy);
        var member = EnsureRole(db, roles, orgId, MemberRoleName, "Default organization member access.", createdBy);

        EnsureClaims(owner, OwnerPermissions(), orgSlug, createdBy);
        EnsureClaims(admin, AdminPermissions(), orgSlug, createdBy);
        EnsureClaims(member, MemberPermissions(), orgSlug, createdBy);

        await db.SaveChangesAsync(ct);
        return new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase)
        {
            [OwnerRoleName] = owner,
            [AdminRoleName] = admin,
            [MemberRoleName] = member,
        };
    }

    /// <summary>
    /// Assigns a built-in role to a user.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roleName">The built-in role name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task AssignAsync(ShipDb db, Guid orgId, string orgSlug, Guid userId, string roleName, CancellationToken ct = default)
    {
        var roles = await EnsureAsync(db, orgId, orgSlug, userId, ct);
        if (!roles.TryGetValue(roleName, out var role))
        {
            throw new ArgumentException("Unknown built-in role.", nameof(roleName));
        }

        var user = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || user.Roles.Any(x => x.Id == role.Id))
        {
            return;
        }

        user.Roles.Add(role);
        await db.SaveChangesAsync(ct);
    }

    private static Role EnsureRole(ShipDb db, Dictionary<string, Role> roles, Guid orgId, string name, string description, Guid createdBy)
    {
        var nameUpcase = name.ToUpperInvariant();
        if (roles.TryGetValue(nameUpcase, out var role))
        {
            role.Description = description;
            return role;
        }

        role = new Role
        {
            Id = Guid.CreateVersion7(),
            OrgId = orgId,
            Name = name,
            NameUpcase = nameUpcase,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        };
        db.Roles.Add(role);
        roles[nameUpcase] = role;
        return role;
    }

    private static void EnsureClaims(Role role, IEnumerable<PermissionKey> permissions, string orgSlug, Guid createdBy)
    {
        var scope = $"organization:{orgSlug}";
        foreach (var permission in permissions)
        {
            var type = permission.ToString();
            if (role.Claims.Any(x => x.Type == type && x.Value == scope))
            {
                continue;
            }

            role.Claims.Add(new RoleClaim
            {
                RoleId = role.Id,
                Type = type,
                Value = scope,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private static IEnumerable<PermissionKey> OwnerPermissions()
        => CorePermissions.All.Where(x => x.AllowedScopes.Contains(PermissionScopeKind.Organization)).Select(x => x.Key);

    private static IEnumerable<PermissionKey> AdminPermissions()
        => OwnerPermissions().Where(x => x != PermissionKey.Create("org.settings", "write"));

    private static IEnumerable<PermissionKey> MemberPermissions()
        => OwnerPermissions().Where(x => x.Action == "read" && x.Resource is "org.settings" or "org.members");
}