using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores organization identity provider configuration.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var providers = await store.ListAsync(orgId, ct);
/// </code>
/// </remarks>
public sealed class IdentityProviderStore
{
    private readonly ShipDb db;
    private readonly IdentityProviderSecretProtector secrets;
    private readonly ILogger<IdentityProviderStore> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityProviderStore"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="secrets">The client secret protector.</param>
    /// <param name="logger">The identity provider store logger.</param>
    public IdentityProviderStore(ShipDb db, IdentityProviderSecretProtector secrets, ILogger<IdentityProviderStore> logger)
    {
        this.db = db;
        this.secrets = secrets;
        this.logger = logger;
    }

    /// <summary>
    /// Lists identity providers for an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching provider configurations.</returns>
    public async Task<List<UserIdentityProvider>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        return await db.UserIdentityProviders
            .AsNoTracking()
            .Where(x => x.OrgId == orgId)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Gets an identity provider by id in an organization.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching provider, or <see langword="null"/>.</returns>
    public async Task<UserIdentityProvider?> GetAsync(Guid orgId, long providerId, CancellationToken ct = default)
    {
        return await db.UserIdentityProviders
            .FirstOrDefaultAsync(x => x.OrgId == orgId && x.Id == providerId, ct);
    }

    /// <summary>
    /// Creates an identity provider configuration.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="createdBy">The creating user identifier.</param>
    /// <param name="name">The provider name.</param>
    /// <param name="providerType">The provider type.</param>
    /// <param name="issuerUrl">The issuer URL.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="clientSecret">The optional plaintext client secret.</param>
    /// <param name="metadataJson">The provider metadata JSON.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created provider configuration.</returns>
    public async Task<UserIdentityProvider> CreateAsync(
        Guid orgId,
        Guid createdBy,
        string name,
        UserIdentityProviderType providerType,
        string? issuerUrl,
        string? clientId,
        string? clientSecret,
        string? metadataJson,
        CancellationToken ct = default)
    {
        var provider = new UserIdentityProvider
        {
            OrgId = orgId,
            UserId = createdBy,
            Name = name.Trim(),
            ProviderTypeId = providerType.Id,
            StatusId = UserIdentityProviderStatus.Inactive.Id,
            IssuerUrl = issuerUrl,
            ClientId = clientId,
            ClientSecretEncrypted = this.secrets.Encrypt(clientSecret),
            MetadataJson = metadataJson,
            CreatedAt = DateTime.UtcNow,
        };

        db.UserIdentityProviders.Add(provider);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Identity provider created: {ProviderId} org={OrgId}", provider.Id, orgId);
        return provider;
    }

    /// <summary>
    /// Updates an identity provider configuration.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="name">The provider name.</param>
    /// <param name="issuerUrl">The issuer URL.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="clientSecret">The optional plaintext client secret.</param>
    /// <param name="metadataJson">The provider metadata JSON.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated provider configuration, or <see langword="null"/>.</returns>
    public async Task<UserIdentityProvider?> UpdateAsync(
        Guid orgId,
        long providerId,
        string? name,
        string? issuerUrl,
        string? clientId,
        string? clientSecret,
        string? metadataJson,
        CancellationToken ct = default)
    {
        var provider = await GetAsync(orgId, providerId, ct);
        if (provider is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            provider.Name = name.Trim();
        }

        if (issuerUrl is not null)
        {
            provider.IssuerUrl = issuerUrl;
        }

        if (clientId is not null)
        {
            provider.ClientId = clientId;
        }

        if (clientSecret is not null)
        {
            provider.ClientSecretEncrypted = this.secrets.Encrypt(clientSecret);
        }

        if (metadataJson is not null)
        {
            provider.MetadataJson = metadataJson;
        }

        provider.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Identity provider updated: {ProviderId} org={OrgId}", provider.Id, orgId);
        return provider;
    }

    /// <summary>
    /// Sets identity provider active state.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="active">A value indicating whether the provider is active.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated provider configuration, or <see langword="null"/>.</returns>
    public async Task<UserIdentityProvider?> SetActiveAsync(Guid orgId, long providerId, bool active, CancellationToken ct = default)
    {
        var provider = await GetAsync(orgId, providerId, ct);
        if (provider is null)
        {
            return null;
        }

        provider.StatusId = active ? UserIdentityProviderStatus.Active.Id : UserIdentityProviderStatus.Inactive.Id;
        provider.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Identity provider status changed: {ProviderId} org={OrgId} active={Active}", provider.Id, orgId, active);
        return provider;
    }
}