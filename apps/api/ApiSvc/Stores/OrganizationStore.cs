using Microsoft.EntityFrameworkCore;

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
    /// Updates organization authentication policy switches.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="allowPasswordAuth">The optional password sign-in allowance.</param>
    /// <param name="allowPasskeyAuth">The optional passkey sign-in allowance.</param>
    /// <param name="allowOidcSso">The optional OIDC SSO sign-in allowance.</param>
    /// <param name="allowSamlSso">The optional SAML SSO sign-in allowance.</param>
    /// <param name="requireSso">The optional SSO requirement.</param>
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
    /// Normalizes an organization slug.
    /// </summary>
    /// <param name="slug">The source slug.</param>
    /// <returns>The normalized slug.</returns>
    public static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }
}
