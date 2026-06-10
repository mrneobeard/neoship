using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests organization store behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class OrganizationStoreTests
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
    /// Verifies that creating an organization normalizes slug and assigns it to the user.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NormalizesSlugAndAssignsUser()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "owner@example.com", "Owner")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);
        var org = await store.CreateAsync(user.Id, "Acme Inc", " Acme-Team ", TestContext.Current.CancellationToken);

        Assert.NotNull(org);
        Assert.Equal("acme-team", org!.Slug);
        Assert.Equal(org.Id, db.Users.Single(u => u.Id == user.Id).OrgId);
    }

    /// <summary>
    /// Verifies that creating an organization rejects duplicate slugs.
    /// </summary>
    [Fact]
    public async Task CreateAsync_RejectsDuplicateSlug()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "owner2@example.com", "Owner Two")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);
        var org = await store.CreateAsync(user.Id, "Duplicate", "default", TestContext.Current.CancellationToken);

        Assert.Null(org);
    }

    /// <summary>
    /// Verifies that organization access follows active memberships.
    /// </summary>
    [Fact]
    public async Task UserCanAccessAsync_UsesActiveMemberships()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "member@example.com", "Member")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        var otherOrg = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Other",
            NameUpcase = "OTHER",
            Slug = "other",
            StatusId = OrganizationStatus.Active.Id,
            TenantModeId = TenantMode.Multi.Id,
            OrganizationPlanId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.Orgs.Add(otherOrg);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = Constants.DefaultOrganizationId,
            UserId = user.Id,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);

        Assert.True(await store.UserCanAccessAsync(user.Id, Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
        Assert.False(await store.UserCanAccessAsync(user.Id, otherOrg.Id, TestContext.Current.CancellationToken));

        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = otherOrg.Id,
            UserId = user.Id,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(await store.UserCanAccessAsync(user.Id, otherOrg.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that organization lists include all active memberships.
    /// </summary>
    [Fact]
    public async Task ListForUserAsync_ReturnsActiveMembershipOrganizations()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "member-list@example.com", "Member List")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var otherOrg = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = "Other",
            NameUpcase = "OTHER",
            Slug = "other",
            StatusId = OrganizationStatus.Active.Id,
            TenantModeId = TenantMode.Multi.Id,
            OrganizationPlanId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(user);
        db.Orgs.Add(otherOrg);
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = Constants.DefaultOrganizationId,
            UserId = user.Id,
        });
        db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = otherOrg.Id,
            UserId = user.Id,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);
        var orgs = await store.ListForUserAsync(user.Id, TestContext.Current.CancellationToken);

        Assert.Equal(2, orgs.Count);
        Assert.Contains(orgs, org => org.Id == Constants.DefaultOrganizationId);
        Assert.Contains(orgs, org => org.Id == otherOrg.Id);
    }

    /// <summary>
    /// Verifies that updating an organization changes its name fields.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ChangesNameFields()
    {
        await using var db = CreateDatabase();
        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);

        var org = await store.UpdateAsync(Constants.DefaultOrganizationId, "Updated", TestContext.Current.CancellationToken);

        Assert.NotNull(org);
        Assert.Equal("Updated", org!.Name);
        Assert.Equal("UPDATED", org.NameUpcase);
        Assert.NotNull(org.UpdatedAt);
    }

    /// <summary>
    /// Verifies that updating organization auth policy changes only supplied switches.
    /// </summary>
    [Fact]
    public async Task UpdateAuthPolicyAsync_ChangesSuppliedSwitches()
    {
        await using var db = CreateDatabase();
        var store = new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);

        var org = await store.UpdateAuthPolicyAsync(
            Constants.DefaultOrganizationId,
            allowPasswordAuth: false,
            allowPasskeyAuth: null,
            allowOidcSso: false,
            allowSamlSso: null,
            requireSso: true,
            allowSelfServiceExternalIdentityUnlink: false,
            TestContext.Current.CancellationToken);

        Assert.NotNull(org);
        Assert.False(org!.AllowPasswordAuth);
        Assert.True(org.AllowPasskeyAuth);
        Assert.False(org.AllowOidcSso);
        Assert.True(org.AllowSamlSso);
        Assert.True(org.RequireSso);
        Assert.False(org.AllowSelfServiceExternalIdentityUnlink);
        Assert.NotNull(org.UpdatedAt);
    }
}
