using Fido2NetLib;

using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

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
public class UserPasskeyChallengeTests
{
    private static UserStore CreateStore()
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlite("Data Source=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;
        var db = new ShipDb(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return TestUserStore.Create(db);
    }

    /// <summary>
    /// Verifies that registration challenges are one-time values.
    /// </summary>
    [Fact]
    public void RegistrationChallenge_CanBeTakenOnce()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var options = CreateOptions();

        var challengeId = store.StorePasskeyRegistrationChallenge(userId, options);

        Assert.Same(options, store.TakePasskeyRegistrationChallenge(challengeId, userId));
        Assert.Null(store.TakePasskeyRegistrationChallenge(challengeId, userId));
    }

    /// <summary>
    /// Verifies that registration challenges are scoped to a user.
    /// </summary>
    [Fact]
    public void RegistrationChallenge_RejectsWrongUser()
    {
        var store = CreateStore();
        var options = CreateOptions();

        var challengeId = store.StorePasskeyRegistrationChallenge(Guid.NewGuid(), options);

        Assert.Null(store.TakePasskeyRegistrationChallenge(challengeId, Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that login challenges are one-time values.
    /// </summary>
    [Fact]
    public void LoginChallenge_CanBeTakenOnce()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var options = CreateAssertionOptions();

        var challengeId = store.StorePasskeyLoginChallenge(userId, options);
        var challenge = store.TakePasskeyLoginChallenge(challengeId);

        Assert.NotNull(challenge);
        Assert.Equal(userId, challenge.Value.UserId);
        Assert.Same(options, challenge.Value.Options);
        Assert.Null(store.TakePasskeyLoginChallenge(challengeId));
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

    private static AssertionOptions CreateAssertionOptions()
    {
        return new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "NeoShip Tests",
            Origins = new HashSet<string> { "https://localhost" },
        }, metadataService: null).GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = Fido2NetLib.Objects.UserVerificationRequirement.Preferred,
        });
    }
}