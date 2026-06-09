using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Manages org-scoped roles and their permission grants.
/// </summary>
public sealed class RoleStore
{
    private readonly ShipDb db;
    private readonly PermissionClaimCodec codec;
    private readonly ILogger<RoleStore> logger;

    /// <summary>
    /// Initializes a new <see cref="RoleStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="codec">The permission claim codec.</param>
    /// <param name="logger">The logger.</param>
    public RoleStore(ShipDb db, PermissionClaimCodec codec, ILogger<RoleStore> logger)
    {
        this.db = db;
        this.codec = codec;
        this.logger = logger;
    }

    /// <summary>
    /// Lists roles in an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching roles.</returns>
    public async Task<List<Role>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        return await this.db.Roles
            .Include(x => x.Claims)
            .Where(x => x.OrgId == orgId)
            .OrderBy(x => x.NameUpcase)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Creates a role.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="createdBy">The creator user identifier.</param>
    /// <param name="name">The role name.</param>
    /// <param name="description">The role description.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created role.</returns>
    public async Task<Role> CreateAsync(Guid orgId, Guid createdBy, string name, string? description, CancellationToken ct = default)
    {
        var role = new Role
        {
            Id = Factory.NewGuid(),
            OrgId = orgId,
            CreatedBy = createdBy,
            Name = name,
            NameUpcase = name.ToUpperInvariant(),
            Description = description,
            CreatedAt = DateTime.UtcNow,
        };

        this.db.Roles.Add(role);
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("Role created: {RoleId} org={OrgId}", role.Id, orgId);
        return role;
    }

    /// <summary>
    /// Gets a role by id within an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching role, or <see langword="null"/>.</returns>
    public async Task<Role?> GetAsync(Guid orgId, Guid roleId, CancellationToken ct = default)
    {
        return await this.db.Roles
            .Include(x => x.Claims)
            .FirstOrDefaultAsync(x => x.Id == roleId && x.OrgId == orgId, ct);
    }

    /// <summary>
    /// Adds a permission grant to a role.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="grant">The permission grant.</param>
    /// <param name="createdBy">The creator user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when added; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AddClaimAsync(Guid orgId, Guid roleId, PermissionGrant grant, Guid createdBy, CancellationToken ct = default)
    {
        var role = await this.GetAsync(orgId, roleId, ct);
        if (role is null)
        {
            return false;
        }

        var (type, value) = this.codec.Encode(grant);
        if (!this.codec.TryDecode(type, value, out var normalizedGrant))
        {
            return false;
        }

        var (normalizedType, normalizedValue) = this.codec.Encode(normalizedGrant);
        role.Claims.Add(new RoleClaim
        {
            Id = 0,
            RoleId = roleId,
            Type = normalizedType,
            Value = normalizedValue,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
        });

        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Removes a role claim.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="claimId">The claim identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemoveClaimAsync(Guid orgId, Guid roleId, ulong claimId, CancellationToken ct = default)
    {
        var role = await this.GetAsync(orgId, roleId, ct);
        if (role is null)
        {
            return false;
        }

        var claim = role.Claims.FirstOrDefault(x => x.Id == claimId);
        if (claim is null)
        {
            return false;
        }

        this.db.RoleClaims.Remove(claim);
        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Attaches a role to a user in the same organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when attached; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AttachUserAsync(Guid orgId, Guid roleId, Guid userId, CancellationToken ct = default)
    {
        var role = await this.db.Roles
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == roleId && x.OrgId == orgId, ct);

        var user = await this.db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.OrgId == orgId, ct);
        if (role is null || user is null)
        {
            return false;
        }

        if (role.Users.Any(x => x.Id == userId))
        {
            return true;
        }

        role.Users.Add(user);
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("Role attached to user: role={RoleId} user={UserId} org={OrgId}", roleId, userId, orgId);
        return true;
    }

    /// <summary>
    /// Detaches a role from a user in the same organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when detached; otherwise <see langword="false"/>.</returns>
    public async Task<bool> DetachUserAsync(Guid orgId, Guid roleId, Guid userId, CancellationToken ct = default)
    {
        var role = await this.db.Roles
            .Include(x => x.Users)
            .FirstOrDefaultAsync(x => x.Id == roleId && x.OrgId == orgId, ct);

        if (role is null)
        {
            return false;
        }

        var user = role.Users.FirstOrDefault(x => x.Id == userId && x.OrgId == orgId);
        if (user is null)
        {
            return false;
        }

        role.Users.Remove(user);
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("Role detached from user: role={RoleId} user={UserId} org={OrgId}", roleId, userId, orgId);
        return true;
    }
}