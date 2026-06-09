using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class RoleGroupStoreTests
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
            StatusId = 1,
            TenantModeId = 0,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task RoleStore_CreatesRoleAndAddsClaim()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var creator = new User(Guid.NewGuid(), "creator@example.com", "Creator")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(creator);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "owners", "Owner role", TestContext.Current.CancellationToken);

        var added = await roles.AddClaimAsync(
            Constants.DefaultOrganizationId,
            role.Id,
            new PermissionGrant(PermissionKey.Create("org.roles", "write"), PermissionScopeKind.Organization, "default"),
            creator.Id,
            TestContext.Current.CancellationToken);

        Assert.True(added);

        var fetched = await roles.GetAsync(Constants.DefaultOrganizationId, role.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.Single(fetched!.Claims);
    }

    [Fact]
    public async Task GroupStore_CanAttachRoleAndMembership()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var groups = new GroupStore(db, NullLogger<GroupStore>.Instance);
        var creator = new User(Guid.NewGuid(), "creator2@example.com", "Creator Two")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        var user = new User(Guid.NewGuid(), "member@example.com", "Member")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(creator);
        db.Users.Add(user);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "writers", null, TestContext.Current.CancellationToken);
        var group = await groups.CreateAsync(Constants.DefaultOrganizationId, "platform", null, null, TestContext.Current.CancellationToken);

        Assert.True(await groups.AddUserAsync(Constants.DefaultOrganizationId, group.Id, user.Id, TestContext.Current.CancellationToken));
        Assert.True(await groups.AttachRoleAsync(Constants.DefaultOrganizationId, group.Id, role.Id, TestContext.Current.CancellationToken));

        var fetched = await groups.GetAsync(Constants.DefaultOrganizationId, group.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fetched);
        Assert.Single(fetched!.Members);
        Assert.Single(fetched.Roles);
    }

    /// <summary>
    /// Verifies that roles can be attached to and detached from users directly.
    /// </summary>

    [Fact]
    public async Task RoleStore_CanAttachAndDetachUser()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var creator = new User(Guid.NewGuid(), "creator3@example.com", "Creator Three")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        var user = new User(Guid.NewGuid(), "direct-member@example.com", "Direct Member")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(creator);
        db.Users.Add(user);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "operators", null, TestContext.Current.CancellationToken);

        Assert.True(await roles.AttachUserAsync(Constants.DefaultOrganizationId, role.Id, user.Id, TestContext.Current.CancellationToken));
        Assert.True(await roles.DetachUserAsync(Constants.DefaultOrganizationId, role.Id, user.Id, TestContext.Current.CancellationToken));
    }
}
