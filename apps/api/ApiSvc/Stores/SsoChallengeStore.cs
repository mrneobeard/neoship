using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Memory;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores short-lived SSO authorization challenges.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var challenge = store.Create(orgId, providerId, redirectUri, nonce);
/// </code>
/// </remarks>
public sealed class SsoChallengeStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache cache;

    /// <summary>
    /// Initializes a new <see cref="SsoChallengeStore"/> instance.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    public SsoChallengeStore(IMemoryCache cache)
    {
        this.cache = cache;
    }

    /// <summary>
    /// Creates and stores a challenge.
    /// </summary>
    /// <param name="orgId">The organization identifier.</param>
    /// <param name="providerId">The identity provider identifier.</param>
    /// <param name="redirectUri">The callback redirect URI.</param>
    /// <param name="nonce">The OIDC nonce.</param>
    /// <returns>The created <see cref="SsoChallenge"/>.</returns>
    public SsoChallenge Create(Guid orgId, long providerId, string redirectUri, string nonce)
    {
        var challenge = new SsoChallenge(CreateToken(), orgId, providerId, redirectUri, nonce, DateTime.UtcNow.Add(Ttl));
        this.cache.Set(GetKey(challenge.State), challenge, Ttl);
        return challenge;
    }

    /// <summary>
    /// Takes and removes an SSO challenge by state.
    /// </summary>
    /// <param name="state">The state token.</param>
    /// <returns>The matching challenge, or <see langword="null"/>.</returns>
    public SsoChallenge? Take(string state)
    {
        var key = GetKey(state);
        if (!this.cache.TryGetValue<SsoChallenge>(key, out var challenge) || challenge is null)
        {
            return null;
        }

        this.cache.Remove(key);
        return challenge;
    }

    /// <summary>
    /// Creates a URL-safe random token.
    /// </summary>
    /// <returns>The random token.</returns>
    public static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    private static string GetKey(string state) => $"sso:state:{state}";
}

/// <summary>
/// Represents a cached SSO challenge.
/// </summary>
/// <param name="State">The state token.</param>
/// <param name="OrgId">The organization identifier.</param>
/// <param name="ProviderId">The identity provider identifier.</param>
/// <param name="RedirectUri">The callback redirect URI.</param>
/// <param name="Nonce">The OIDC nonce.</param>
/// <param name="ExpiresAt">The challenge expiry time.</param>
public sealed record SsoChallenge(string State, Guid OrgId, long ProviderId, string RedirectUri, string Nonce, DateTime ExpiresAt);