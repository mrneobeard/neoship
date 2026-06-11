using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests organization invite behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public sealed class OrganizationInviteTests
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

    private static OrganizationStore CreateStore(ShipDb db)
    {
        return new OrganizationStore(db, NullLogger<OrganizationStore>.Instance);
    }

    /// <summary>
    /// Verifies invites are created with a seven-day default expiry and revocable token digest.
    /// </summary>
    [Fact]
    public async Task CreateAsync_CreatesSevenDayInviteAndTokenDigest()
    {
        await using var db = CreateDatabase();
        var inviter = new User(Guid.NewGuid(), "inviter@example.com", "Inviter")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        db.Users.Add(inviter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = CreateStore(db);
        var before = DateTime.UtcNow;
        var (invite, token) = await store.CreateInviteAsync(Constants.DefaultOrganizationId, inviter.Id, " person@example.com ", [], [], TestContext.Current.CancellationToken);

        Assert.NotEmpty(token);
        Assert.NotEqual(token, invite.TokenDigest);
        Assert.Equal("person@example.com", invite.Email);
        Assert.Equal("PERSON@EXAMPLE.COM", invite.EmailUpcase);
        Assert.InRange(invite.ExpiresAt, before.AddDays(7).AddMinutes(-1), before.AddDays(7).AddMinutes(1));
    }

    /// <summary>
    /// Verifies active invites can be revoked.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_RevokesActiveInvite()
    {
        await using var db = CreateDatabase();
        var inviter = new User(Guid.NewGuid(), "revoker@example.com", "Revoker")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        db.Users.Add(inviter);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var store = CreateStore(db);
        var (invite, _) = await store.CreateInviteAsync(Constants.DefaultOrganizationId, inviter.Id, "person@example.com", [], [], TestContext.Current.CancellationToken);

        Assert.True(await store.RevokeInviteAsync(Constants.DefaultOrganizationId, invite.Id, TestContext.Current.CancellationToken));
        Assert.False(await store.RevokeInviteAsync(Constants.DefaultOrganizationId, invite.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(invite.RevokedAt);
    }

    /// <summary>
    /// Verifies accepting an invite creates membership and applies pending assignments.
    /// </summary>
    [Fact]
    public async Task AcceptAsync_CreatesMembershipAndAppliesPendingAssignments()
    {
        await using var db = CreateDatabase();
        var inviter = new User(Guid.NewGuid(), "assigner@example.com", "Assigner")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var invited = new User(Guid.NewGuid(), "person@example.com", "Person")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "Member",
            NameUpcase = "MEMBER",
            CreatedBy = inviter.Id,
        };
        var group = new Group
        {
            Id = Guid.CreateVersion7(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "Operators",
            NameUpcase = "OPERATORS",
        };
        db.Users.AddRange(inviter, invited);
        db.Roles.Add(role);
        db.Groups.Add(group);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = CreateStore(db);
        var (invite, token) = await store.CreateInviteAsync(Constants.DefaultOrganizationId, inviter.Id, invited.Email, [role.Id], [group.Id], TestContext.Current.CancellationToken);
        var accepted = await store.AcceptInviteAsync(token, invited.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(accepted);
        Assert.Equal(invite.Id, accepted!.Id);
        Assert.NotNull(accepted.AcceptedAt);
        Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.OrgId == Constants.DefaultOrganizationId && x.UserId == invited.Id, TestContext.Current.CancellationToken));
        Assert.True(await db.Roles.Where(x => x.Id == role.Id).SelectMany(x => x.Users).AnyAsync(x => x.Id == invited.Id, TestContext.Current.CancellationToken));
        Assert.True(await db.Groups.Where(x => x.Id == group.Id).SelectMany(x => x.Members).AnyAsync(x => x.Id == invited.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies accepting an invite requires the invited email address.
    /// </summary>
    [Fact]
    public async Task AcceptAsync_RejectsDifferentEmailUser()
    {
        await using var db = CreateDatabase();
        var inviter = new User(Guid.NewGuid(), "inviter2@example.com", "Inviter Two")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        var other = new User(Guid.NewGuid(), "other@example.com", "Other")
        {
            OrgId = Constants.DefaultOrganizationId,
        };
        db.Users.AddRange(inviter, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var store = CreateStore(db);
        var (_, token) = await store.CreateInviteAsync(Constants.DefaultOrganizationId, inviter.Id, "person@example.com", [], [], TestContext.Current.CancellationToken);

        Assert.Null(await store.AcceptInviteAsync(token, other.Id, TestContext.Current.CancellationToken));
    }
}