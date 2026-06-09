using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests SSO authorization flow setup.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class SsoStoreTests
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
    /// Verifies OIDC begin creates an authorization URL and one-time challenge.
    /// </summary>
    [Fact]
    public async Task BeginOidcAsync_CreatesAuthorizationUrlAndChallenge()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var store = new SsoStore(db, challenges);
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-admin@example.com", "SSO Admin")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
            MetadataJson = "{\"authorization_endpoint\":\"https://idp.example.com/oauth2/authorize\"}",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await store.BeginOidcAsync("default", null, "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("https://idp.example.com/oauth2/authorize?", result!.AuthorizationUrl, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", result.AuthorizationUrl, StringComparison.Ordinal);
        Assert.Contains("response_type=code", result.AuthorizationUrl, StringComparison.Ordinal);
        Assert.Contains($"state={result.State}", result.AuthorizationUrl, StringComparison.Ordinal);

        var challenge = challenges.Take(result.State);
        Assert.NotNull(challenge);
        Assert.Equal(Constants.DefaultOrganizationId, challenge!.OrgId);
        Assert.Null(challenges.Take(result.State));
    }

    /// <summary>
    /// Verifies non-HTTPS authorization endpoints are rejected.
    /// </summary>
    [Fact]
    public async Task BeginOidcAsync_RejectsNonHttpsAuthorizationEndpoint()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new SsoStore(db, new SsoChallengeStore(cache));
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-http@example.com", "SSO Http")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Bad OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
            MetadataJson = "{\"authorization_endpoint\":\"http://idp.example.com/oauth2/authorize\"}",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await store.BeginOidcAsync("default", null, "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
