using Fido2NetLib;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests passkey store behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class PasskeyStoreTests
{
    private static ShipDb CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlite("Data Source=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;

        var db = new ShipDb(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        db.Orgs.Add(new Organization
        {
            Id = Constants.DefaultOrganizationId,
            Name = "Default",
            NameUpcase = "DEFAULT",
            Slug = "default",
            StatusId = OrganizationStatus.Active.Id,
            TenantModeId = TenantMode.None.Id,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        db.SaveChanges();
        return db;
    }

    private static Fido2 CreateFido2()
    {
        return new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "NeoShip Tests",
            Origins = new HashSet<string> { "https://localhost" },
        }, metadataService: null);
    }

    /// <summary>
    /// Verifies that registration options include the current user.
    /// </summary>
    [Fact]
    public async Task BeginRegistrationAsync_ReturnsUserBoundOptions()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "passkey@example.com", "Passkey User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new PasskeyStore(db, CreateFido2(), NullLogger<PasskeyStore>.Instance);
        var options = await store.BeginRegistrationAsync(user, TestContext.Current.CancellationToken);

        Assert.Equal(user.Email, options.User.Name);
        Assert.Equal(user.Name, options.User.DisplayName);
        Assert.NotEmpty(options.Challenge);
    }

    /// <summary>
    /// Verifies that passkeys can be listed and revoked.
    /// </summary>
    [Fact]
    public async Task Passkeys_CanBeListedAndRevoked()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "passkey2@example.com", "Passkey User Two")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Name = "Laptop",
            Type = MfaFactorType.Passkey.Id,
            WebAuthnCredentialId = [1, 2, 3],
            WebAuthnCredentialIdDigest = "digest",
            WebAuthnPublicKeyCredentialData = [4, 5, 6],
            WebAuthnSignCount = 7,
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            VerifiedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };

        db.Users.Add(user);
        db.UserMfaFactors.Add(factor);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new PasskeyStore(db, CreateFido2(), NullLogger<PasskeyStore>.Instance);

        Assert.Single(await store.ListAsync(user.Id, TestContext.Current.CancellationToken));
        Assert.True(await store.RevokeAsync(user.Id, factor.Id, TestContext.Current.CancellationToken));
        Assert.Empty(await store.ListAsync(user.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that passkey login options include registered credentials.
    /// </summary>
    [Fact]
    public async Task BeginLoginAsync_ReturnsAssertionOptionsForRegisteredPasskey()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "login-passkey@example.com", "Login Passkey User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Name = "Laptop",
            Type = MfaFactorType.Passkey.Id,
            WebAuthnCredentialId = [1, 2, 3],
            WebAuthnCredentialIdDigest = PasskeyStore.ComputeCredentialIdDigest([1, 2, 3]),
            WebAuthnPublicKeyCredentialData = [4, 5, 6],
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            VerifiedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        };

        db.Users.Add(user);
        db.UserMfaFactors.Add(factor);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new PasskeyStore(db, CreateFido2(), NullLogger<PasskeyStore>.Instance);
        var result = await store.BeginLoginAsync(user.Email, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Value.User.Id);
        Assert.Single(result.Value.Options.AllowCredentials);
    }

    /// <summary>
    /// Verifies organization policy can block passkey login.
    /// </summary>
    [Fact]
    public async Task BeginLoginAsync_WhenSsoIsRequired_ReturnsNull()
    {
        await using var db = CreateDatabase();
        var org = await db.Orgs.SingleAsync(o => o.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);
        org.RequireSso = true;
        var user = new User(Guid.NewGuid(), "blocked-passkey@example.com", "Blocked Passkey User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        db.UserMfaFactors.Add(new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Name = "Laptop",
            Type = MfaFactorType.Passkey.Id,
            WebAuthnCredentialId = [1, 2, 3],
            WebAuthnCredentialIdDigest = PasskeyStore.ComputeCredentialIdDigest([1, 2, 3]),
            WebAuthnPublicKeyCredentialData = [4, 5, 6],
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            VerifiedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
        });
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new PasskeyStore(db, CreateFido2(), NullLogger<PasskeyStore>.Instance);
        var result = await store.BeginLoginAsync(user.Email, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
