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

    /// <summary>
    /// Verifies roles can be updated within an organization.
    /// </summary>
    [Fact]
    public async Task RoleStore_CanUpdateRole()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var creator = new User(Guid.NewGuid(), "role-update@example.com", "Role Update")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(creator);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "operators", null, TestContext.Current.CancellationToken);
        var updated = await roles.UpdateAsync(Constants.DefaultOrganizationId, role.Id, "maintainers", "Maintainers", TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("maintainers", updated!.Name);
        Assert.Equal("MAINTAINERS", updated.NameUpcase);
        Assert.Equal("Maintainers", updated.Description);
    }

    /// <summary>
    /// Verifies legacy persisted built-in roles are hidden from custom role operations.
    /// </summary>
    [Fact]
    public async Task RoleStore_HidesLegacyBuiltInRows()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var creator = new User(Guid.NewGuid(), "legacy-role@example.com", "Legacy Role")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "Owner",
            NameUpcase = "OWNER",
            CreatedBy = creator.Id,
        };

        db.Users.Add(creator);
        db.Roles.Add(ownerRole);
        db.SaveChanges();

        var listed = await roles.ListAsync(Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.Empty(listed);
        Assert.Null(await roles.GetAsync(Constants.DefaultOrganizationId, ownerRole.Id, TestContext.Current.CancellationToken));
        Assert.False(await roles.DeleteAsync(Constants.DefaultOrganizationId, ownerRole.Id, TestContext.Current.CancellationToken));
        Assert.True(await db.Roles.AnyAsync(x => x.Id == ownerRole.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies single role reads are scoped to the owning organization.
    /// </summary>
    [Fact]
    public async Task RoleStore_GetAsync_RejectsWrongOrganization()
    {
        var db = CreateDatabase();
        var otherOrgId = Guid.NewGuid();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var creator = new User(Guid.NewGuid(), "role-get@example.com", "Role Get")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Orgs.Add(new Organization
        {
            Id = otherOrgId,
            Name = "Other",
            NameUpcase = "OTHER",
            Slug = "other",
            StatusId = 1,
            TenantModeId = 0,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.Users.Add(creator);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "readers", null, TestContext.Current.CancellationToken);

        Assert.Null(await roles.GetAsync(otherOrgId, role.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(await roles.GetAsync(Constants.DefaultOrganizationId, role.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies deleting a role removes claims and direct assignments.
    /// </summary>
    [Fact]
    public async Task RoleStore_CanDeleteRoleWithAssignments()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var groups = new GroupStore(db, NullLogger<GroupStore>.Instance);
        var creator = new User(Guid.NewGuid(), "role-delete@example.com", "Role Delete")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var user = new User(Guid.NewGuid(), "role-delete-member@example.com", "Role Delete Member")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(creator);
        db.Users.Add(user);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "temporary", null, TestContext.Current.CancellationToken);
        var group = await groups.CreateAsync(Constants.DefaultOrganizationId, "ops", null, null, TestContext.Current.CancellationToken);

        Assert.True(await roles.AddClaimAsync(
            Constants.DefaultOrganizationId,
            role.Id,
            new PermissionGrant(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization, "default"),
            creator.Id,
            TestContext.Current.CancellationToken));
        Assert.True(await roles.AttachUserAsync(Constants.DefaultOrganizationId, role.Id, user.Id, TestContext.Current.CancellationToken));
        Assert.True(await groups.AttachRoleAsync(Constants.DefaultOrganizationId, group.Id, role.Id, TestContext.Current.CancellationToken));

        Assert.True(await roles.DeleteAsync(Constants.DefaultOrganizationId, role.Id, TestContext.Current.CancellationToken));
        Assert.Null(await roles.GetAsync(Constants.DefaultOrganizationId, role.Id, TestContext.Current.CancellationToken));
        Assert.Empty(db.RoleClaims);
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
    /// Verifies groups can be updated within an organization.
    /// </summary>
    [Fact]
    public async Task GroupStore_CanUpdateGroup()
    {
        var db = CreateDatabase();
        var groups = new GroupStore(db, NullLogger<GroupStore>.Instance);

        var group = await groups.CreateAsync(Constants.DefaultOrganizationId, "platform", null, null, TestContext.Current.CancellationToken);
        var updated = await groups.UpdateAsync(Constants.DefaultOrganizationId, group.Id, "ops", "ops@example.com", "Operations", TestContext.Current.CancellationToken);

        Assert.NotNull(updated);
        Assert.Equal("ops", updated!.Name);
        Assert.Equal("OPS", updated.NameUpcase);
        Assert.Equal("ops@example.com", updated.Email);
        Assert.Equal("OPS@EXAMPLE.COM", updated.EmailUpcase);
        Assert.Equal("Operations", updated.Description);
    }

    /// <summary>
    /// Verifies single group reads are scoped to the owning organization.
    /// </summary>
    [Fact]
    public async Task GroupStore_GetAsync_RejectsWrongOrganization()
    {
        var db = CreateDatabase();
        var otherOrgId = Guid.NewGuid();
        var groups = new GroupStore(db, NullLogger<GroupStore>.Instance);

        db.Orgs.Add(new Organization
        {
            Id = otherOrgId,
            Name = "Other",
            NameUpcase = "OTHER",
            Slug = "other",
            StatusId = 1,
            TenantModeId = 0,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();

        var group = await groups.CreateAsync(Constants.DefaultOrganizationId, "readers", null, null, TestContext.Current.CancellationToken);

        Assert.Null(await groups.GetAsync(otherOrgId, group.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(await groups.GetAsync(Constants.DefaultOrganizationId, group.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies deleting a group removes memberships and role assignments.
    /// </summary>
    [Fact]
    public async Task GroupStore_CanDeleteGroupWithAssignments()
    {
        var db = CreateDatabase();
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var roles = new RoleStore(db, codec, NullLogger<RoleStore>.Instance);
        var groups = new GroupStore(db, NullLogger<GroupStore>.Instance);
        var creator = new User(Guid.NewGuid(), "group-delete-creator@example.com", "Group Delete Creator")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var user = new User(Guid.NewGuid(), "group-delete-user@example.com", "Group Delete User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "group-delete-bot",
            NameUpcase = "GROUP-DELETE-BOT",
            CreatedBy = creator.Id,
        };

        db.Users.Add(creator);
        db.Users.Add(user);
        db.ServiceAccounts.Add(serviceAccount);
        db.SaveChanges();

        var role = await roles.CreateAsync(Constants.DefaultOrganizationId, creator.Id, "group-delete-role", null, TestContext.Current.CancellationToken);
        var group = await groups.CreateAsync(Constants.DefaultOrganizationId, "delete-me", null, null, TestContext.Current.CancellationToken);

        Assert.True(await groups.AddUserAsync(Constants.DefaultOrganizationId, group.Id, user.Id, TestContext.Current.CancellationToken));
        Assert.True(await groups.AddServiceAccountAsync(Constants.DefaultOrganizationId, group.Id, serviceAccount.Id, TestContext.Current.CancellationToken));
        Assert.True(await groups.AttachRoleAsync(Constants.DefaultOrganizationId, group.Id, role.Id, TestContext.Current.CancellationToken));

        Assert.True(await groups.DeleteAsync(Constants.DefaultOrganizationId, group.Id, TestContext.Current.CancellationToken));
        Assert.Null(await groups.GetAsync(Constants.DefaultOrganizationId, group.Id, TestContext.Current.CancellationToken));
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