using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores organization invite lifecycle operations.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var (invite, token) = await store.CreateAsync(orgId, inviterId, "person@example.com", [], [], ct);
/// </code>
/// </remarks>
public sealed class OrganizationInviteStore
{
    private static readonly TimeSpan DefaultInviteLifetime = TimeSpan.FromDays(7);

    private readonly ShipDb db;
    private readonly TokenStore tokens;
    private readonly ILogger<OrganizationInviteStore> logger;

    /// <summary>
    /// Initializes a new <see cref="OrganizationInviteStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="tokens">The token helper.</param>
    /// <param name="logger">The logger.</param>
    public OrganizationInviteStore(ShipDb db, TokenStore tokens, ILogger<OrganizationInviteStore> logger)
    {
        this.db = db;
        this.tokens = tokens;
        this.logger = logger;
    }

    /// <summary>
    /// Lists organization invites.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The organization invites.</returns>
    public async Task<List<OrganizationInvite>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        return await this.db.OrganizationInvites
            .Where(x => x.OrgId == orgId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Creates an organization invite.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="invitedByUserId">The inviter user identifier.</param>
    /// <param name="email">The invited email address.</param>
    /// <param name="roleIds">The pending role identifiers.</param>
    /// <param name="groupIds">The pending group identifiers.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created invite and plaintext token.</returns>
    public async Task<(OrganizationInvite Invite, string Token)> CreateAsync(
        Guid orgId,
        Guid invitedByUserId,
        string email,
        IReadOnlyCollection<Guid> roleIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim();
        var rawToken = this.tokens.GenerateResetToken();
        var invite = new OrganizationInvite
        {
            Id = Guid.CreateVersion7(),
            OrgId = orgId,
            InvitedByUserId = invitedByUserId,
            Email = normalizedEmail,
            EmailUpcase = normalizedEmail.ToUpperInvariant(),
            TokenDigest = TokenStore.ComputeDigestBase64(rawToken),
            PendingRoleIdsJson = JsonSerializer.Serialize(roleIds),
            PendingGroupIdsJson = JsonSerializer.Serialize(groupIds),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(DefaultInviteLifetime),
        };

        this.db.OrganizationInvites.Add(invite);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Organization invite created: {InviteId} org={OrgId}", invite.Id, orgId);
        return (invite, rawToken);
    }

    /// <summary>
    /// Revokes an active invite.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="inviteId">The invite identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when revoked; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RevokeAsync(Guid orgId, Guid inviteId, CancellationToken ct = default)
    {
        var invite = await this.db.OrganizationInvites.FirstOrDefaultAsync(x => x.Id == inviteId && x.OrgId == orgId, ct);
        if (invite is null || invite.AcceptedAt is not null || invite.RevokedAt is not null)
        {
            return false;
        }

        invite.RevokedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Organization invite revoked: {InviteId} org={OrgId}", inviteId, orgId);
        return true;
    }

    /// <summary>
    /// Accepts an invite for a user and applies pending role/group assignments.
    /// </summary>
    /// <param name="rawToken">The plaintext invite token.</param>
    /// <param name="userId">The accepting user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The accepted invite, or <see langword="null"/>.</returns>
    public async Task<OrganizationInvite?> AcceptAsync(string rawToken, Guid userId, CancellationToken ct = default)
    {
        var tokenDigest = TokenStore.ComputeDigestBase64(rawToken);
        var invite = await this.db.OrganizationInvites.FirstOrDefaultAsync(x => x.TokenDigest == tokenDigest, ct);
        if (invite is null || invite.AcceptedAt is not null || invite.RevokedAt is not null || invite.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var user = await this.db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || !string.Equals(user.EmailUpcase, invite.EmailUpcase, StringComparison.Ordinal))
        {
            return null;
        }

        var membership = await this.db.OrganizationMemberships.FirstOrDefaultAsync(x => x.OrgId == invite.OrgId && x.UserId == userId, ct);
        if (membership is null)
        {
            this.db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = invite.OrgId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow,
            });
        }
        else
        {
            membership.DeletedAt = null;
            membership.AcceptedAt = DateTime.UtcNow;
        }

        await this.ApplyPendingAssignmentsAsync(invite, user, ct);

        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = userId;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Organization invite accepted: {InviteId} org={OrgId} user={UserId}", invite.Id, invite.OrgId, userId);
        return invite;
    }

    private async Task ApplyPendingAssignmentsAsync(OrganizationInvite invite, User user, CancellationToken ct)
    {
        var roleIds = ParseIds(invite.PendingRoleIdsJson);
        if (roleIds.Count > 0)
        {
            var roles = await this.db.Roles.Where(x => x.OrgId == invite.OrgId && roleIds.Contains(x.Id)).ToListAsync(ct);
            foreach (var role in roles)
            {
                role.Users.Add(user);
            }
        }

        var groupIds = ParseIds(invite.PendingGroupIdsJson);
        if (groupIds.Count > 0)
        {
            var groups = await this.db.Groups.Include(x => x.Members).Where(x => x.OrgId == invite.OrgId && groupIds.Contains(x.Id)).ToListAsync(ct);
            foreach (var group in groups)
            {
                group.Members.Add(user);
            }
        }
    }

    private static List<Guid> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}