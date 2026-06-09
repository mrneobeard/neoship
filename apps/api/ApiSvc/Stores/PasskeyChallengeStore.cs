using Fido2NetLib;

using Microsoft.Extensions.Caching.Memory;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores short-lived passkey ceremony challenges.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var id = store.StoreRegistration(userId, options);
/// </code>
/// </remarks>
public sealed class PasskeyChallengeStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasskeyChallengeStore"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    public PasskeyChallengeStore(IMemoryCache cache)
    {
        this.cache = cache;
    }

    /// <summary>
    /// Stores registration options and returns a challenge identifier.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="options">The credential creation options.</param>
    /// <returns>The challenge identifier.</returns>
    public Guid StoreRegistration(Guid userId, CredentialCreateOptions options)
    {
        var challengeId = Guid.CreateVersion7();
        cache.Set(GetRegistrationKey(challengeId), new PasskeyRegistrationChallenge(userId, options), Ttl);
        return challengeId;
    }

    /// <summary>
    /// Takes and removes registration options for a challenge.
    /// </summary>
    /// <param name="challengeId">The challenge identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The credential creation options, or <see langword="null"/>.</returns>
    public CredentialCreateOptions? TakeRegistration(Guid challengeId, Guid userId)
    {
        var key = GetRegistrationKey(challengeId);
        if (!cache.TryGetValue<PasskeyRegistrationChallenge>(key, out var challenge) || challenge is null || challenge.UserId != userId)
        {
            return null;
        }

        cache.Remove(key);
        return challenge.Options;
    }

    private static string GetRegistrationKey(Guid challengeId)
    {
        return $"passkey:registration:{challengeId:N}";
    }

    private sealed record PasskeyRegistrationChallenge(Guid UserId, CredentialCreateOptions Options);
}