using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests shared user authentication behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
public class MeAuthenticationTests
{
    /// <summary>
    /// Verifies that bearer user API keys authenticate and mark the HTTP context with the API key id.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_AcceptsBearerUserApiKey()
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlite("Data Source=:memory:")
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new ShipDb(options);
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var user = new User(Guid.NewGuid(), "api@example.com", "API User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

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
        db.Users.Add(user);

        var apiKeys = new ApiKeyStore(db, NullLogger<ApiKeyStore>.Instance);
        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(user.Id, "test", null, "[]", null);
        db.UserApiKeys.Add(apiKey);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var services = new ServiceCollection()
            .AddSingleton(db)
            .AddSingleton(apiKeys)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
        };
        httpContext.Request.Headers.Authorization = $"Bearer {plaintextKey}";

        var sessions = new SessionStore(
            db,
            new TokenStore(),
            new RequestContext(),
            new PermissionSnapshotCodec(),
            NullLogger<SessionStore>.Instance);

        var authenticated = await MeEndpoints.AuthenticateAsync(httpContext, sessions, TestContext.Current.CancellationToken);

        Assert.NotNull(authenticated);
        Assert.Equal(user.Id, authenticated!.Id);
        Assert.Equal(apiKey.Id, Assert.IsType<Guid>(httpContext.Items[MeEndpoints.UserApiKeyItemKey]));
    }
}