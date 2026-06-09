using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests MFA store behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class MfaStoreTests
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
    /// Verifies that TOTP setup can be confirmed and disabled.
    /// </summary>
    [Fact]
    public async Task TotpFactor_CanBeConfirmedAndDisabled()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "mfa@example.com", "MFA User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new MfaStore(db, NullLogger<MfaStore>.Instance);
        var (factor, secret) = await store.StartTotpAsync(user.Id, "Phone", TestContext.Current.CancellationToken);
        var code = MfaStore.ComputeTotp(factor.ValueEncrypted, DateTimeOffset.UtcNow);

        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.True(await store.ConfirmTotpAsync(user.Id, factor.Id, code, TestContext.Current.CancellationToken));

        var confirmed = await db.UserMfaFactors.SingleAsync(x => x.Id == factor.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(confirmed.VerifiedAt);

        Assert.True(await store.DisableTotpAsync(user.Id, factor.Id, TestContext.Current.CancellationToken));
        Assert.Empty(await db.UserMfaFactors.ToListAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that invalid TOTP codes are rejected.
    /// </summary>
    [Fact]
    public async Task ConfirmTotpAsync_RejectsInvalidCode()
    {
        await using var db = CreateDatabase();
        var user = new User(Guid.NewGuid(), "mfa2@example.com", "MFA User Two")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new MfaStore(db, NullLogger<MfaStore>.Instance);
        var (factor, _) = await store.StartTotpAsync(user.Id, "Phone", TestContext.Current.CancellationToken);

        Assert.False(await store.ConfirmTotpAsync(user.Id, factor.Id, "000000", TestContext.Current.CancellationToken));
    }
}
