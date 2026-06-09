using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests identity provider store behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class IdentityProviderStoreTests
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

    private static IdentityProviderStore CreateStore(ShipDb db)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:IdentityProviders:EncryptionKey"] = "test-identity-provider-secret-key",
            })
            .Build();
        var protector = new IdentityProviderSecretProtector(configuration);
        return new IdentityProviderStore(db, protector, NullLogger<IdentityProviderStore>.Instance);
    }

    /// <summary>
    /// Verifies that identity providers can be created, updated, listed, and enabled.
    /// </summary>
    [Fact]
    public async Task IdentityProviderStore_CanManageProviderLifecycle()
    {
        await using var db = CreateDatabase();
        var store = CreateStore(db);
        var createdBy = Guid.NewGuid();
        db.Users.Add(new User(createdBy, "idp-admin@example.com", "IDP Admin")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var provider = await store.CreateAsync(
            Constants.DefaultOrganizationId,
            createdBy,
            "Acme OIDC",
            UserIdentityProviderType.OIDC,
            "https://idp.example.com",
            "client-id",
            "client-secret",
            "{}",
            TestContext.Current.CancellationToken);

        Assert.Equal(UserIdentityProviderStatus.Inactive.Id, provider.StatusId);
        Assert.NotEmpty(provider.ClientSecretEncrypted);
        Assert.DoesNotContain("client-secret", Convert.ToBase64String(provider.ClientSecretEncrypted));
        var initialSecretEncrypted = provider.ClientSecretEncrypted.ToArray();

        var updated = await store.UpdateAsync(
            Constants.DefaultOrganizationId,
            provider.Id,
            "Acme Login",
            null,
            null,
            "replacement-secret",
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("Acme Login", updated!.Name);
        Assert.NotEqual(initialSecretEncrypted, updated.ClientSecretEncrypted);

        var enabled = await store.SetActiveAsync(Constants.DefaultOrganizationId, provider.Id, active: true, TestContext.Current.CancellationToken);
        var providers = await store.ListAsync(Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.NotNull(enabled);
        Assert.Equal(UserIdentityProviderStatus.Active.Id, enabled!.StatusId);
        Assert.Single(providers);
    }

    /// <summary>
    /// Verifies single identity provider reads are scoped to the owning organization.
    /// </summary>
    [Fact]
    public async Task IdentityProviderStore_GetAsync_RejectsWrongOrganization()
    {
        await using var db = CreateDatabase();
        var otherOrgId = Guid.NewGuid();
        var store = CreateStore(db);
        var createdBy = Guid.NewGuid();
        db.Orgs.Add(new Organization
        {
            Id = otherOrgId,
            Name = "Other",
            NameUpcase = "OTHER",
            Slug = "other",
            StatusId = OrganizationStatus.Active.Id,
            TenantModeId = TenantMode.None.Id,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.Users.Add(new User(createdBy, "idp-read@example.com", "IDP Read")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var provider = await store.CreateAsync(
            Constants.DefaultOrganizationId,
            createdBy,
            "Acme OIDC",
            UserIdentityProviderType.OIDC,
            "https://idp.example.com",
            "client-id",
            null,
            "{}",
            TestContext.Current.CancellationToken);

        Assert.Null(await store.GetAsync(otherOrgId, provider.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(await store.GetAsync(Constants.DefaultOrganizationId, provider.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies identity provider client secrets require configured encryption.
    /// </summary>
    [Fact]
    public void IdentityProviderSecretProtector_RequiresConfiguredKey()
    {
        var protector = new IdentityProviderSecretProtector(new ConfigurationBuilder().Build());

        Assert.Throws<InvalidOperationException>(() => protector.Encrypt("client-secret"));
        Assert.Empty(protector.Encrypt(null));
    }
}
