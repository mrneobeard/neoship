using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests service account store behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class ServiceAccountStoreTests
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

    private static ServiceAccountStore CreateStore(ShipDb db)
    {
        return new ServiceAccountStore(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)), NullLogger<ServiceAccountStore>.Instance);
    }

    /// <summary>
    /// Verifies direct service account claims can be added, listed, and removed.
    /// </summary>
    [Fact]
    public async Task ServiceAccountClaims_CanBeManaged()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "sa-tests@example.com", "Service Account Tester")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "deploy-bot",
            NameUpcase = "DEPLOY-BOT",
            CreatedBy = user.Id,
        };

        db.Users.Add(user);
        db.ServiceAccounts.Add(serviceAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = CreateStore(db);
        var claim = await store.AddClaimAsync(
            Constants.DefaultOrganizationId,
            serviceAccount.Id,
            new PermissionGrant(PermissionKey.Create("org.service_accounts", "read"), PermissionScopeKind.Organization, "default"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(claim);

        var claims = await store.ListClaimsAsync(Constants.DefaultOrganizationId, serviceAccount.Id, TestContext.Current.CancellationToken);
        Assert.Single(claims!);
        Assert.True(await store.RemoveClaimAsync(Constants.DefaultOrganizationId, serviceAccount.Id, claim!.Id, TestContext.Current.CancellationToken));
        Assert.Empty((await store.ListClaimsAsync(Constants.DefaultOrganizationId, serviceAccount.Id, TestContext.Current.CancellationToken))!);
    }
}
