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

    /// <summary>
    /// Verifies direct service account API key claims can be added, listed, and removed.
    /// </summary>
    [Fact]
    public async Task ServiceAccountApiKeyClaims_CanBeManaged()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "sa-key-tests@example.com", "Service Account Key Tester")
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
        var (_, apiKey) = store.GenerateApiKey(serviceAccount.Id, "ci", null, "[]", DateTime.UtcNow.AddHours(1));
        db.ServiceAccountApiKeys.Add(apiKey);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var claim = await store.AddApiKeyClaimAsync(
            Constants.DefaultOrganizationId,
            serviceAccount.Id,
            apiKey.Id,
            new PermissionGrant(PermissionKey.Create("org.service_accounts", "read"), PermissionScopeKind.Organization, "default"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(claim);

        var claims = await store.ListApiKeyClaimsAsync(Constants.DefaultOrganizationId, serviceAccount.Id, apiKey.Id, TestContext.Current.CancellationToken);
        Assert.Single(claims!);
        Assert.True(await store.RemoveApiKeyClaimAsync(Constants.DefaultOrganizationId, serviceAccount.Id, apiKey.Id, claim!.Id, TestContext.Current.CancellationToken));
        Assert.Empty((await store.ListApiKeyClaimsAsync(Constants.DefaultOrganizationId, serviceAccount.Id, apiKey.Id, TestContext.Current.CancellationToken))!);
    }

    /// <summary>
    /// Verifies disabled service accounts can be re-enabled.
    /// </summary>
    [Fact]
    public async Task EnableAsync_RestoresDisabledServiceAccount()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "sa-enable-tests@example.com", "Service Account Enable Tester")
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
            DeletedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.ServiceAccounts.Add(serviceAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = CreateStore(db);
        Assert.Empty(await store.ListAsync(Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));

        Assert.True(await store.EnableAsync(Constants.DefaultOrganizationId, serviceAccount.Id, TestContext.Current.CancellationToken));

        var accounts = await store.ListAsync(Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);
        Assert.Single(accounts);
        Assert.Null(accounts[0].DeletedAt);
        Assert.NotNull(accounts[0].UpdatedAt);
    }

    /// <summary>
    /// Verifies single service account reads exclude disabled accounts.
    /// </summary>
    [Fact]
    public async Task GetAsync_ExcludesDisabledServiceAccounts()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "sa-get-tests@example.com", "Service Account Get Tester")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "disabled-bot",
            NameUpcase = "DISABLED-BOT",
            CreatedBy = user.Id,
            DeletedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.ServiceAccounts.Add(serviceAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = CreateStore(db);
        var found = await store.GetAsync(Constants.DefaultOrganizationId, serviceAccount.Id, TestContext.Current.CancellationToken);

        Assert.Null(found);
    }
}
