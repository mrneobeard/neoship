using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Manages org-scoped groups and membership.
/// </summary>
public sealed class GroupStore
{
    private readonly ShipDb db;
    private readonly ILogger<GroupStore> logger;

    /// <summary>
    /// Initializes a new <see cref="GroupStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">The logger.</param>
    public GroupStore(ShipDb db, ILogger<GroupStore> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Lists groups in an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching groups.</returns>
    public async Task<List<Group>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        return await this.db.Groups
            .Include(x => x.Roles)
            .Where(x => x.OrgId == orgId)
            .OrderBy(x => x.NameUpcase)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Creates a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="name">The group name.</param>
    /// <param name="email">The optional group email.</param>
    /// <param name="description">The optional group description.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created group.</returns>
    public async Task<Group> CreateAsync(Guid orgId, string name, string? email, string? description, CancellationToken ct = default)
    {
        var group = new Group
        {
            Id = Factory.NewGuid(),
            OrgId = orgId,
            Name = name,
            NameUpcase = name.ToUpperInvariant(),
            Email = email,
            EmailUpcase = email?.ToUpperInvariant(),
            Description = description,
        };

        this.db.Groups.Add(group);
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("Group created: {GroupId} org={OrgId}", group.Id, orgId);
        return group;
    }

    /// <summary>
    /// Gets a group by id within an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching group, or <see langword="null"/>.</returns>
    public async Task<Group?> GetAsync(Guid orgId, Guid groupId, CancellationToken ct = default)
    {
        return await this.db.Groups
            .Include(x => x.Roles)
            .Include(x => x.Members)
            .Include(x => x.Owners)
            .Include(x => x.ServiceAccountMembers)
            .Include(x => x.ServiceAccountOwners)
            .FirstOrDefaultAsync(x => x.Id == groupId && x.OrgId == orgId, ct);
    }

    /// <summary>
    /// Adds a user to a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when added; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AddUserAsync(Guid orgId, Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var user = await this.db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.OrgId == orgId, ct);
        if (group is null || user is null)
        {
            return false;
        }

        group.Members.Add(user);
        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemoveUserAsync(Guid orgId, Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var user = await this.db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.OrgId == orgId, ct);
        if (group is null || user is null)
        {
            return false;
        }

        if (!group.Members.Remove(user))
        {
            return false;
        }

        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Attaches a role to a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when attached; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AttachRoleAsync(Guid orgId, Guid groupId, Guid roleId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var role = await this.db.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.OrgId == orgId, ct);
        if (group is null || role is null)
        {
            return false;
        }

        group.Roles.Add(role);
        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Adds a service account to a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when added; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AddServiceAccountAsync(Guid orgId, Guid groupId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var serviceAccount = await this.db.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == serviceAccountId && x.OrgId == orgId, ct);
        if (group is null || serviceAccount is null)
        {
            return false;
        }

        group.ServiceAccountMembers.Add(serviceAccount);
        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Removes a service account from a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="serviceAccountId">The service account identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when removed; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemoveServiceAccountAsync(Guid orgId, Guid groupId, Guid serviceAccountId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var serviceAccount = await this.db.ServiceAccounts.FirstOrDefaultAsync(x => x.Id == serviceAccountId && x.OrgId == orgId, ct);
        if (group is null || serviceAccount is null)
        {
            return false;
        }

        if (!group.ServiceAccountMembers.Remove(serviceAccount))
        {
            return false;
        }

        await this.db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Detaches a role from a group.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when detached; otherwise <see langword="false"/>.</returns>
    public async Task<bool> DetachRoleAsync(Guid orgId, Guid groupId, Guid roleId, CancellationToken ct = default)
    {
        var group = await this.GetAsync(orgId, groupId, ct);
        var role = await this.db.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.OrgId == orgId, ct);
        if (group is null || role is null)
        {
            return false;
        }

        if (!group.Roles.Remove(role))
        {
            return false;
        }

        await this.db.SaveChangesAsync(ct);
        return true;
    }
}
