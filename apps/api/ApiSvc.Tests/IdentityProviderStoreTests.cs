using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Verifies that identity providers can be created, updated, listed, and enabled.
    /// </summary>
    [Fact]
    public async Task IdentityProviderStore_CanManageProviderLifecycle()
    {
        await using var db = CreateDatabase();
        var store = new IdentityProviderStore(db, NullLogger<IdentityProviderStore>.Instance);
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
            "{}",
            TestContext.Current.CancellationToken);

        Assert.Equal(UserIdentityProviderStatus.Inactive.Id, provider.StatusId);

        var updated = await store.UpdateAsync(
            Constants.DefaultOrganizationId,
            provider.Id,
            "Acme Login",
            null,
            null,
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("Acme Login", updated!.Name);

        var enabled = await store.SetActiveAsync(Constants.DefaultOrganizationId, provider.Id, active: true, TestContext.Current.CancellationToken);
        var providers = await store.ListAsync(Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.NotNull(enabled);
        Assert.Equal(UserIdentityProviderStatus.Active.Id, enabled!.StatusId);
        Assert.Single(providers);
    }
}
