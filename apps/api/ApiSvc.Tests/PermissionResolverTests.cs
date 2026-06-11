using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class PermissionResolverTests
{
    private static (ShipDb db, PermissionResolver resolver) CreateDatabase()
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

        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        return (db, new PermissionResolver(db, codec));
    }

    [Fact]
    public async Task ResolveUserAsync_IncludesDirectRoleAndGroupClaims()
    {
        var (db, resolver) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "reader@example.com", "Reader")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "role-admin",
            NameUpcase = "ROLE-ADMIN",
            CreatedBy = user.Id,
        };

        var group = new Group
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "ops",
            NameUpcase = "OPS",
        };

        db.Users.Add(user);
        db.Roles.Add(role);
        db.Groups.Add(group);
        db.SaveChanges();

        db.UserClaims.Add(new UserClaim
        {
            UserId = user.Id,
            Type = "org.roles.read",
            Value = "organization:default",
        });

        db.RoleClaims.Add(new RoleClaim
        {
            RoleId = role.Id,
            Type = "org.groups.write",
            Value = "organization:default",
            CreatedBy = user.Id,
        });

        db.SaveChanges();

        user.Roles.Add(role);
        group.Members.Add(user);
        group.Roles.Add(role);
        db.SaveChanges();

        var permissions = await resolver.ResolveUserAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.True(permissions.Allows(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization, "default"));
        Assert.True(permissions.Allows(PermissionKey.Create("org.groups", "write"), PermissionScopeKind.Organization, "default"));
    }

    [Fact]
    public async Task BuiltInRoleStore_AssignsOwnerPermissions()
    {
        var (db, resolver) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "builtin-owner@example.com", "Built In Owner")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await BuiltInRoleStore.AssignAsync(db, Constants.DefaultOrganizationId, "default", user.Id, BuiltInRoleStore.OwnerRoleName, TestContext.Current.CancellationToken);

        var permissions = await resolver.ResolveUserAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.True(permissions.Allows(PermissionKey.Create("org.settings", "write"), PermissionScopeKind.Organization, "default"));
        Assert.True(permissions.Allows(PermissionKey.Create("org.identity_providers", "write"), PermissionScopeKind.Organization, "default"));
        Assert.True(await db.RoleAssignments.AnyAsync(x => x.OrgId == Constants.DefaultOrganizationId && x.UserId == user.Id && x.RoleKey == BuiltInRoleStore.OwnerRoleName, TestContext.Current.CancellationToken));
        Assert.False(await db.Roles.AnyAsync(x => x.OrgId == Constants.DefaultOrganizationId && x.Name == BuiltInRoleStore.OwnerRoleName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ServiceAccountResolver_IncludesDirectClaims()
    {
        var (db, resolver) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "owner@example.com", "Owner")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        db.SaveChanges();

        var serviceAccount = new ServiceAccount
        {
            Id = Guid.NewGuid(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "deploy-bot",
            NameUpcase = "DEPLOY-BOT",
            CreatedBy = user.Id,
        };

        db.ServiceAccounts.Add(serviceAccount);
        db.SaveChanges();

        db.ServiceAccountClaims.Add(new ServiceAccountClaim
        {
            ServiceAccountId = serviceAccount.Id,
            Type = "org.service_accounts.write",
            Value = "organization:default",
        });
        db.SaveChanges();

        var permissions = await resolver.ResolveServiceAccountAsync(serviceAccount.Id, TestContext.Current.CancellationToken);

        Assert.True(permissions.Allows(PermissionKey.Create("org.service_accounts", "write"), PermissionScopeKind.Organization, "default"));
    }

    [Fact]
    public async Task ResolveUserApiKeyAsync_IncludesKeySpecificClaims()
    {
        var (db, resolver) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "apikey@example.com", "Api Key User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        db.SaveChanges();

        var apiKeys = new ApiKeyStore(db, NullLogger<ApiKeyStore>.Instance);
        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(
            user.Id, "ci", null, "[]", DateTime.UtcNow.AddHours(1));

        db.UserApiKeys.Add(apiKey);
        db.SaveChanges();

        db.UserApiKeyClaims.Add(new UserApiKeyClaim
        {
            UserApiKeyId = apiKey.Id,
            Type = "auth.sessions.revoke",
            Value = "organization:default",
        });
        db.SaveChanges();

        var permissions = await resolver.ResolveUserApiKeyAsync(apiKey.Id, TestContext.Current.CancellationToken);

        Assert.True(permissions.Allows(PermissionKey.Create("auth.sessions", "revoke"), PermissionScopeKind.Organization, "default"));
        Assert.NotNull(plaintextKey);
    }

    [Fact]
    public async Task ResolveServiceAccountApiKeyAsync_IncludesKeySpecificClaims()
    {
        var (db, resolver) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "sa-owner@example.com", "Service Account Owner")
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
        db.SaveChanges();

        var serviceAccounts = new ServiceAccountStore(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)), NullLogger<ServiceAccountStore>.Instance);
        var (_, apiKey) = serviceAccounts.GenerateApiKey(serviceAccount.Id, "ci", null, "[]", DateTime.UtcNow.AddHours(1));

        db.ServiceAccountApiKeys.Add(apiKey);
        db.ServiceAccountApiKeyClaims.Add(new ServiceAccountApiKeyClaim
        {
            ServiceAccountApiKeyId = apiKey.Id,
            Type = "org.service_accounts.read",
            Value = "organization:default",
        });
        db.SaveChanges();

        var permissions = await resolver.ResolveServiceAccountApiKeyAsync(apiKey.Id, TestContext.Current.CancellationToken);

        Assert.True(permissions.Allows(PermissionKey.Create("org.service_accounts", "read"), PermissionScopeKind.Organization, "default"));
    }
}