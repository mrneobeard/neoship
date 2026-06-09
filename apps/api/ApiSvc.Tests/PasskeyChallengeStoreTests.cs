using Fido2NetLib;

using Microsoft.Extensions.Caching.Memory;

using NeoShip.ApiSvc.Stores;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests passkey challenge cache behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class PasskeyChallengeStoreTests
{
    /// <summary>
    /// Verifies that registration challenges are one-time values.
    /// </summary>
    [Fact]
    public void RegistrationChallenge_CanBeTakenOnce()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new PasskeyChallengeStore(cache);
        var userId = Guid.NewGuid();
        var options = CreateOptions();

        var challengeId = store.StoreRegistration(userId, options);

        Assert.Same(options, store.TakeRegistration(challengeId, userId));
        Assert.Null(store.TakeRegistration(challengeId, userId));
    }

    /// <summary>
    /// Verifies that registration challenges are scoped to a user.
    /// </summary>
    [Fact]
    public void RegistrationChallenge_RejectsWrongUser()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new PasskeyChallengeStore(cache);
        var options = CreateOptions();

        var challengeId = store.StoreRegistration(Guid.NewGuid(), options);

        Assert.Null(store.TakeRegistration(challengeId, Guid.NewGuid()));
    }

    private static CredentialCreateOptions CreateOptions()
    {
        return new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "NeoShip Tests",
            Origins = new HashSet<string> { "https://localhost" },
        }, metadataService: null).RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = [1, 2, 3],
                Name = "passkey@example.com",
                DisplayName = "Passkey User",
            },
            ExcludeCredentials = [],
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = Fido2NetLib.Objects.AttestationConveyancePreference.None,
        });
    }
}
