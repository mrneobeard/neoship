using System.Text;
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
    private readonly ISsoOAuth2ProfileClient? oauth2ProfileClient;

    /// <summary>
    /// Initializes a new <see cref="SsoStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="challenges">The SSO challenge store.</param>
    /// <param name="tokenClient">The OIDC token client.</param>
    /// <param name="tokenValidator">The OIDC token validator.</param>
    /// <param name="oauth2ProfileClient">The optional OAuth2 profile client.</param>
    public SsoStore(ShipDb db, SsoChallengeStore challenges, ISsoTokenClient tokenClient, ISsoTokenValidator tokenValidator, ISsoOAuth2ProfileClient? oauth2ProfileClient = null)
    {
        this.db = db;
        this.challenges = challenges;
        this.tokenClient = tokenClient;
        this.tokenValidator = tokenValidator;
        this.oauth2ProfileClient = oauth2ProfileClient;
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

        if (!org.AllowOidcSso)
        {
            return null;
        }

        var query = this.db.UserIdentityProviders
            .Where(x => x.OrgId == org.Id
                && (x.ProviderTypeId == UserIdentityProviderType.OIDC.Id || x.ProviderTypeId == UserIdentityProviderType.OAUTH2.Id)
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
            && (x.ProviderTypeId == UserIdentityProviderType.OIDC.Id || x.ProviderTypeId == UserIdentityProviderType.OAUTH2.Id)
            && x.StatusId == UserIdentityProviderStatus.Active.Id, ct);
        if (provider is null)
        {
            return null;
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Id == challenge.OrgId, ct);
        if (org is null || !org.AllowOidcSso)
        {
            return null;
        }

        var tokenResponse = await this.tokenClient.ExchangeAsync(provider, code, challenge.RedirectUri, ct);
        if (tokenResponse is null)
        {
            return null;
        }

        var externalIdentity = await this.ResolveExternalIdentityAsync(provider, tokenResponse, challenge.Nonce, ct);
        if (externalIdentity is null || !externalIdentity.EmailVerified)
        {
            return null;
        }

        var subjectDigest = ComputeSubjectDigest(externalIdentity.Subject);
        var link = await this.db.UserExternalIdentities
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.OrgId == challenge.OrgId
                && x.ProviderId == provider.Id
                && x.SubjectDigest == subjectDigest, ct);

        await using var transaction = await this.db.Database.BeginTransactionAsync(ct);

        var emailUpcase = externalIdentity.Email.ToUpperInvariant();
        var user = link?.User is { StatusId: var status } linkedUser && status == UserStatus.Active.Id
            ? linkedUser
            : await this.db.Users.FirstOrDefaultAsync(x => x.OrgId == challenge.OrgId
                && x.EmailUpcase == emailUpcase
                && x.StatusId == UserStatus.Active.Id, ct);

        var createdUser = false;
        if (user is null)
        {
            user = new User(Guid.CreateVersion7(), externalIdentity.Email, string.IsNullOrWhiteSpace(externalIdentity.Name) ? externalIdentity.Email : externalIdentity.Name)
            {
                OrgId = challenge.OrgId,
                StatusId = UserStatus.Active.Id,
            };
            this.db.Users.Add(user);
            this.db.UserEmails.Add(new UserEmail
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Email = externalIdentity.Email,
                EmailUpcase = emailUpcase,
                EmailDigest = TokenStore.ComputeDigestBase64(externalIdentity.Email),
                StatusId = UserEmailStatus.Active.Id,
                CreatedBy = provider.UserId,
                CreatedAt = DateTime.UtcNow,
                VerifiedAt = DateTime.UtcNow,
            });
            this.db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = challenge.OrgId,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                AcceptedAt = DateTime.UtcNow,
            });
            createdUser = true;
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
                SubjectDigest = subjectDigest,
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
        if (createdUser)
        {
            await BuiltInRoleStore.AssignAsync(this.db, org.Id, org.Slug, user.Id, BuiltInRoleStore.MemberRoleName, ct);
        }

        await transaction.CommitAsync(ct);
        return user;
    }

    private async Task<SsoExternalIdentity?> ResolveExternalIdentityAsync(UserIdentityProvider provider, SsoTokenResponse tokenResponse, string nonce, CancellationToken ct)
    {
        if (provider.ProviderTypeId == UserIdentityProviderType.OIDC.Id)
        {
            return string.IsNullOrWhiteSpace(tokenResponse.IdToken)
                ? null
                : await this.tokenValidator.ValidateAsync(provider, tokenResponse.IdToken, nonce, ct);
        }

        if (provider.ProviderTypeId != UserIdentityProviderType.OAUTH2.Id || this.oauth2ProfileClient is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            return null;
        }

        var profile = await this.oauth2ProfileClient.FetchAsync(provider, tokenResponse.AccessToken, ct);
        return profile is null ? null : new SsoExternalIdentity(profile.Subject, profile.Email, profile.EmailVerified, profile.Name);
    }

    /// <summary>
    /// Lists external identities linked to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The linked external identities.</returns>
    public async Task<List<UserExternalIdentity>> ListExternalIdentitiesAsync(Guid userId, CancellationToken ct = default)
    {
        return await this.db.UserExternalIdentities
            .Include(x => x.Provider)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ProviderId)
            .ThenBy(x => x.Email)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Unlinks an external identity if policy and lockout safety allow it.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="externalIdentityId">The external identity identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The unlink result.</returns>
    public async Task<SsoExternalIdentityUnlinkResult> UnlinkExternalIdentityAsync(Guid userId, Guid externalIdentityId, CancellationToken ct = default)
    {
        var link = await this.db.UserExternalIdentities
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == externalIdentityId && x.UserId == userId, ct);
        if (link?.User is null)
        {
            return SsoExternalIdentityUnlinkResult.NotFound;
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Id == link.OrgId, ct);
        if (org is null || !org.AllowSelfServiceExternalIdentityUnlink)
        {
            return SsoExternalIdentityUnlinkResult.PolicyDenied;
        }

        if (!await HasAlternativeSignInMethodAsync(link.User, externalIdentityId, org, ct))
        {
            return SsoExternalIdentityUnlinkResult.LastMethod;
        }

        this.db.UserExternalIdentities.Remove(link);
        await this.db.SaveChangesAsync(ct);
        return SsoExternalIdentityUnlinkResult.Success;
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

    private static string ComputeSubjectDigest(string subject)
    {
        return TokenStore.ComputeDigestBase64(Encoding.UTF8.GetBytes(subject));
    }

    private async Task<bool> HasAlternativeSignInMethodAsync(User user, Guid excludingExternalIdentityId, Organization org, CancellationToken ct)
    {
        if (org.AllowPasswordAuth && await this.db.UserPasswordAuths.AnyAsync(x => x.UserId == user.Id && x.PasswordHash != string.Empty, ct))
        {
            return true;
        }

        if (org.AllowPasskeyAuth && await this.db.UserMfaFactors.AnyAsync(x => x.UserId == user.Id
            && x.Type == MfaFactorType.Passkey.Id
            && x.VerifiedAt != null, ct))
        {
            return true;
        }

        return await this.db.UserExternalIdentities
            .Include(x => x.Provider)
            .AnyAsync(x => x.UserId == user.Id
                && x.Id != excludingExternalIdentityId
                && x.Provider != null
                && x.Provider.StatusId == UserIdentityProviderStatus.Active.Id
                && ((org.AllowOidcSso && x.Provider.ProviderTypeId == UserIdentityProviderType.OIDC.Id)
                    || (org.AllowSamlSso && x.Provider.ProviderTypeId == UserIdentityProviderType.SAML.Id)), ct);
    }
}

/// <summary>
/// Represents an SSO begin result.
/// </summary>
/// <param name="ProviderId">The identity provider identifier.</param>
/// <param name="AuthorizationUrl">The provider authorization URL.</param>
/// <param name="State">The state token.</param>
/// <param name="ExpiresAt">The state expiry time.</param>
public sealed record SsoBeginResult(long ProviderId, string AuthorizationUrl, string State, DateTime ExpiresAt);

/// <summary>
/// Represents the result of unlinking an external identity.
/// </summary>
public enum SsoExternalIdentityUnlinkResult
{
    /// <summary>
    /// The external identity was unlinked.
    /// </summary>
    Success,

    /// <summary>
    /// The external identity was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Organization policy denied self-service unlinking.
    /// </summary>
    PolicyDenied,

    /// <summary>
    /// Unlinking would remove the user's last usable sign-in method.
    /// </summary>
    LastMethod,
}
