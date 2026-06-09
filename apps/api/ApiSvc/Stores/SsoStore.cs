using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Builds SSO authorization requests.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var begin = await store.BeginOidcAsync("default", null, redirectUri, ct);
/// </code>
/// </remarks>
public sealed class SsoStore
{
    private readonly ShipDb db;
    private readonly SsoChallengeStore challenges;
    private readonly ISsoTokenClient tokenClient;
    private readonly ISsoTokenValidator tokenValidator;

    /// <summary>
    /// Initializes a new <see cref="SsoStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="challenges">The SSO challenge store.</param>
    /// <param name="tokenClient">The OIDC token client.</param>
    /// <param name="tokenValidator">The OIDC token validator.</param>
    public SsoStore(ShipDb db, SsoChallengeStore challenges, ISsoTokenClient tokenClient, ISsoTokenValidator tokenValidator)
    {
        this.db = db;
        this.challenges = challenges;
        this.tokenClient = tokenClient;
        this.tokenValidator = tokenValidator;
    }

    /// <summary>
    /// Begins an OIDC authorization flow.
    /// </summary>
    /// <param name="orgSlug">The organization slug.</param>
    /// <param name="providerId">The optional provider identifier.</param>
    /// <param name="redirectUri">The callback redirect URI.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The SSO begin result, or <see langword="null"/>.</returns>
    public async Task<SsoBeginResult?> BeginOidcAsync(string orgSlug, long? providerId, string redirectUri, CancellationToken ct = default)
    {
        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Slug == orgSlug, ct);
        if (org is null)
        {
            return null;
        }

        var query = this.db.UserIdentityProviders
            .Where(x => x.OrgId == org.Id
                && x.ProviderTypeId == UserIdentityProviderType.OIDC.Id
                && x.StatusId == UserIdentityProviderStatus.Active.Id);

        if (providerId.HasValue)
        {
            query = query.Where(x => x.Id == providerId.Value);
        }

        var provider = await query.OrderBy(x => x.Name).FirstOrDefaultAsync(ct);
        if (provider is null || string.IsNullOrWhiteSpace(provider.ClientId))
        {
            return null;
        }

        var authorizationEndpoint = TryReadAuthorizationEndpoint(provider.MetadataJson);
        if (authorizationEndpoint is null)
        {
            return null;
        }

        var nonce = SsoChallengeStore.CreateToken();
        var challenge = this.challenges.Create(org.Id, provider.Id, redirectUri, nonce);
        var authorizationUrl = BuildAuthorizationUrl(authorizationEndpoint, provider.ClientId, redirectUri, challenge.State, nonce);

        return new SsoBeginResult(provider.Id, authorizationUrl, challenge.State, challenge.ExpiresAt);
    }

    /// <summary>
    /// Finishes an OIDC authorization flow.
    /// </summary>
    /// <param name="state">The state token.</param>
    /// <param name="code">The authorization code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated user, or <see langword="null"/>.</returns>
    public async Task<User?> FinishOidcAsync(string state, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var challenge = this.challenges.Take(state);
        if (challenge is null)
        {
            return null;
        }

        var provider = await this.db.UserIdentityProviders.FirstOrDefaultAsync(x => x.Id == challenge.ProviderId
            && x.OrgId == challenge.OrgId
            && x.ProviderTypeId == UserIdentityProviderType.OIDC.Id
            && x.StatusId == UserIdentityProviderStatus.Active.Id, ct);
        if (provider is null)
        {
            return null;
        }

        var tokenResponse = await this.tokenClient.ExchangeAsync(provider, code, challenge.RedirectUri, ct);
        if (tokenResponse is null)
        {
            return null;
        }

        var externalIdentity = await this.tokenValidator.ValidateAsync(provider, tokenResponse.IdToken, challenge.Nonce, ct);
        if (externalIdentity is null || !externalIdentity.EmailVerified)
        {
            return null;
        }

        var subjectUpcase = externalIdentity.Subject.ToUpperInvariant();
        var link = await this.db.UserExternalIdentities
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.OrgId == challenge.OrgId
                && x.ProviderId == provider.Id
                && x.SubjectUpcase == subjectUpcase, ct);

        var emailUpcase = externalIdentity.Email.ToUpperInvariant();
        var user = link?.User is { StatusId: var status } linkedUser && status == UserStatus.Active.Id
            ? linkedUser
            : await this.db.Users.FirstOrDefaultAsync(x => x.OrgId == challenge.OrgId
                && x.EmailUpcase == emailUpcase
                && x.StatusId == UserStatus.Active.Id, ct);
        if (user is null)
        {
            return null;
        }

        if (link is null)
        {
            this.db.UserExternalIdentities.Add(new UserExternalIdentity
            {
                Id = Guid.CreateVersion7(),
                OrgId = challenge.OrgId,
                UserId = user.Id,
                ProviderId = provider.Id,
                Subject = externalIdentity.Subject,
                SubjectUpcase = subjectUpcase,
                Email = externalIdentity.Email,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
            });
        }
        else
        {
            link.Email = externalIdentity.Email;
            link.LastUsedAt = DateTime.UtcNow;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);
        return user;
    }

    private static string? TryReadAuthorizationEndpoint(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty("authorization_endpoint", out var element))
            {
                return null;
            }

            var value = element.GetString();
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.ToString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildAuthorizationUrl(string authorizationEndpoint, string clientId, string redirectUri, string state, string nonce)
    {
        var separator = authorizationEndpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return authorizationEndpoint + separator + string.Join('&', new[]
        {
            Pair("client_id", clientId),
            Pair("redirect_uri", redirectUri),
            Pair("response_type", "code"),
            Pair("scope", "openid profile email"),
            Pair("state", state),
            Pair("nonce", nonce),
        });
    }

    private static string Pair(string key, string value) => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
}

/// <summary>
/// Represents an SSO begin result.
/// </summary>
/// <param name="ProviderId">The identity provider identifier.</param>
/// <param name="AuthorizationUrl">The provider authorization URL.</param>
/// <param name="State">The state token.</param>
/// <param name="ExpiresAt">The state expiry time.</param>
public sealed record SsoBeginResult(long ProviderId, string AuthorizationUrl, string State, DateTime ExpiresAt);