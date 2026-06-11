using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores organization lifecycle and lookup operations.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var org = await store.GetBySlugAsync("default", ct);
/// </code>
/// </remarks>
public sealed class OrganizationStore
{
    private static readonly TimeSpan DefaultInviteLifetime = TimeSpan.FromDays(7);

    private readonly ShipDb db;
    private readonly ILogger<OrganizationStore> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationStore"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">The organization store logger.</param>
    public OrganizationStore(ShipDb db, ILogger<OrganizationStore> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Lists organizations visible to the user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of visible <see cref="Organization"/> values.</returns>
    public async Task<List<Organization>> ListForUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return [];
        }

        return await db.OrganizationMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.DeletedAt == null)
            .Select(m => m.Org!)
            .OrderBy(o => o.NameUpcase)
            .ThenBy(o => o.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets an organization by slug.
    /// </summary>
    /// <param name="slug">The organization slug.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching <see cref="Organization"/>, or <see langword="null"/>.</returns>
    public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        return await db.Orgs.FirstOrDefaultAsync(o => o.Slug == slug, ct);
    }

    /// <summary>
    /// Creates an organization and assigns it as the user's current organization.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The organization name.</param>
    /// <param name="slug">The organization slug.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="Organization"/>, or <see langword="null"/> when the user does not exist or slug is unavailable.</returns>
    public async Task<Organization?> CreateAsync(Guid userId, string name, string slug, CancellationToken ct)
    {
        var normalizedSlug = NormalizeSlug(slug);
        var slugExists = await db.Orgs.AnyAsync(o => o.Slug == normalizedSlug, ct);
        if (slugExists)
        {
            return null;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        var org = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NameUpcase = name.Trim().ToUpperInvariant(),
            Slug = normalizedSlug,
            StatusId = OrganizationStatus.Active.Id,
            TenantModeId = TenantMode.Multi.Id,
            OrganizationPlanId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        db.Orgs.Add(org);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = org.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            AcceptedAt = DateTime.UtcNow,
        });
        user.OrgId = org.Id;

        await BuiltInRoleStore.AssignAsync(db, org.Id, org.Slug, userId, BuiltInRoleStore.OwnerRoleName, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Organization created: {OrgId} slug={Slug} userId={UserId}", org.Id, org.Slug, userId);

        return org;
    }

    /// <summary>
    /// Updates an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="name">The organization name, when changing.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated <see cref="Organization"/>, or <see langword="null"/>.</returns>
    public async Task<Organization?> UpdateAsync(Guid orgId, string? name, CancellationToken ct)
    {
        var org = await db.Orgs.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            org.Name = name.Trim();
            org.NameUpcase = org.Name.ToUpperInvariant();
        }

        org.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Organization updated: {OrgId}", org.Id);
        return org;
    }

    /// <summary>
    /// Marks an organization pending deletion and revokes active memberships.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="retentionDays">The number of days before the organization becomes hard-delete eligible.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The deleted <see cref="Organization"/>, or <see langword="null"/>.</returns>
    public async Task<Organization?> DeleteAsync(Guid orgId, int retentionDays, CancellationToken ct)
    {
        var org = await db.Orgs.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        org.StatusId = OrganizationStatus.PendingDeleted.Id;
        org.DeletedAt = now;
        org.HardDeleteAt = now.AddDays(Math.Max(0, retentionDays));
        org.UpdatedAt = now;

        await db.OrganizationMemberships
            .Where(x => x.OrgId == orgId && x.DeletedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.DeletedAt, now), ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Organization marked pending deletion: {OrgId}", org.Id);
        return org;
    }

    /// <summary>
    /// Updates organization authentication policy switches.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="allowPasswordAuth">The optional password sign-in allowance.</param>
    /// <param name="allowPasskeyAuth">The optional passkey sign-in allowance.</param>
    /// <param name="allowOidcSso">The optional OIDC SSO sign-in allowance.</param>
    /// <param name="allowSamlSso">The optional SAML SSO sign-in allowance.</param>
    /// <param name="requireSso">The optional SSO requirement.</param>
    /// <param name="mfaPolicy">The optional MFA policy.</param>
    /// <param name="allowSelfServiceExternalIdentityUnlink">The optional self-service external identity unlink allowance.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated <see cref="Organization"/>, or <see langword="null"/>.</returns>
    public async Task<Organization?> UpdateAuthPolicyAsync(
        Guid orgId,
        bool? allowPasswordAuth,
        bool? allowPasskeyAuth,
        bool? allowOidcSso,
        bool? allowSamlSso,
        bool? requireSso,
        OrganizationMfaPolicy? mfaPolicy,
        bool? allowSelfServiceExternalIdentityUnlink,
        CancellationToken ct)
    {
        var org = await db.Orgs.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org is null)
        {
            return null;
        }

        org.AllowPasswordAuth = allowPasswordAuth ?? org.AllowPasswordAuth;
        org.AllowPasskeyAuth = allowPasskeyAuth ?? org.AllowPasskeyAuth;
        org.AllowOidcSso = allowOidcSso ?? org.AllowOidcSso;
        org.AllowSamlSso = allowSamlSso ?? org.AllowSamlSso;
        org.RequireSso = requireSso ?? org.RequireSso;
        org.MfaPolicyId = mfaPolicy?.Id ?? org.MfaPolicyId;
        org.AllowSelfServiceExternalIdentityUnlink = allowSelfServiceExternalIdentityUnlink ?? org.AllowSelfServiceExternalIdentityUnlink;
        org.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Organization auth policy updated: {OrgId}", org.Id);
        return org;
    }

    /// <summary>
    /// Determines whether the user can access the organization.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when the user can access the organization; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> UserCanAccessAsync(Guid userId, Guid orgId, CancellationToken ct)
    {
        return await db.OrganizationMemberships.AnyAsync(m => m.UserId == userId
            && m.OrgId == orgId
            && m.DeletedAt == null, ct);
    }

    /// <summary>
    /// Switches a user's current organization when they have active membership.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The switched organization, or <see langword="null"/>.</returns>
    public async Task<Organization?> SwitchCurrentAsync(Guid userId, Guid orgId, CancellationToken ct)
    {
        var membership = await db.OrganizationMemberships
            .Include(x => x.Org)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.OrgId == orgId && x.DeletedAt == null, ct);
        if (membership?.Org is null)
        {
            return null;
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
        {
            return null;
        }

        user.OrgId = orgId;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User switched current organization: user={UserId} org={OrgId}", userId, orgId);
        return membership.Org;
    }

    /// <summary>
    /// Lists organization invites.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The organization invites.</returns>
    public async Task<List<OrganizationInvite>> ListInvitesAsync(Guid orgId, CancellationToken ct = default)
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
    public async Task<(OrganizationInvite Invite, string Token)> CreateInviteAsync(Guid orgId, Guid invitedByUserId, string email, IReadOnlyCollection<Guid> roleIds, IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim();
        var rawToken = TokenGenerator.GenerateResetToken();
        var invite = new OrganizationInvite
        {
            Id = Guid.CreateVersion7(),
            OrgId = orgId,
            InvitedByUserId = invitedByUserId,
            Email = normalizedEmail,
            EmailUpcase = normalizedEmail.ToUpperInvariant(),
            TokenDigest = TokenGenerator.ComputeDigestBase64(rawToken),
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
    public async Task<bool> RevokeInviteAsync(Guid orgId, Guid inviteId, CancellationToken ct = default)
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
    public async Task<OrganizationInvite?> AcceptInviteAsync(string rawToken, Guid userId, CancellationToken ct = default)
    {
        var tokenDigest = TokenGenerator.ComputeDigestBase64(rawToken);
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

        await this.ApplyPendingInviteAssignmentsAsync(invite, user, ct);

        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = userId;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Organization invite accepted: {InviteId} org={OrgId} user={UserId}", invite.Id, invite.OrgId, userId);
        return invite;
    }

    /// <summary>
    /// Normalizes an organization slug.
    /// </summary>
    /// <param name="slug">The source slug.</param>
    /// <returns>The normalized slug.</returns>
    public static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }

    private async Task ApplyPendingInviteAssignmentsAsync(OrganizationInvite invite, User user, CancellationToken ct)
    {
        var roleIds = ParseInviteIds(invite.PendingRoleIdsJson);
        if (roleIds.Count > 0)
        {
            var roles = await this.db.Roles.Where(x => x.OrgId == invite.OrgId && roleIds.Contains(x.Id)).ToListAsync(ct);
            foreach (var role in roles)
            {
                role.Users.Add(user);
            }
        }

        var groupIds = ParseInviteIds(invite.PendingGroupIdsJson);
        if (groupIds.Count > 0)
        {
            var groups = await this.db.Groups.Include(x => x.Members).Where(x => x.OrgId == invite.OrgId && groupIds.Contains(x.Id)).ToListAsync(ct);
            foreach (var group in groups)
            {
                group.Members.Add(user);
            }
        }
    }

    private static List<Guid> ParseInviteIds(string? json)
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
