using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Migration)]
public sealed class ProviderMigrationE2ETests
{
    [Fact]
    public async Task Sqlite_MigrationsApplyAndAuthFlowWorks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"neoship-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<ShipDb>()
                .UseSqlite($"Data Source={path}", b => b.MigrationsAssembly("NeoShip.Data.Sqlite"))
                .UseSnakeCaseNamingConvention()
                .Options;

            await using var db = new ShipDb(options);
            await RunMigrationAndAuthFlowAsync(db);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task PostgreSql_MigrationsApplyAndAuthFlowWorks()
    {
        await using var container = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("neoship")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await StartOrSkipAsync(container.StartAsync(TestContext.Current.CancellationToken));

        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseNpgsql(container.GetConnectionString(), b => b.MigrationsAssembly("NeoShip.Data.Pgsql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new ShipDb(options);
        await RunMigrationAndAuthFlowAsync(db);
    }

    [Fact]
    public async Task SqlServer_MigrationsApplyAndAuthFlowWorks()
    {
        await using var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Password123!")
            .Build();

        await StartOrSkipAsync(container.StartAsync(TestContext.Current.CancellationToken));

        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlServer(container.GetConnectionString(), b => b.MigrationsAssembly("NeoShip.Data.Mssql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new ShipDb(options);
        await RunMigrationAndAuthFlowAsync(db);
    }

    private static async Task RunMigrationAndAuthFlowAsync(ShipDb db)
    {
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await SeedDefaultOrgAsync(db);

        var ctx = new RequestContext { IpAddress = "127.0.0.1", UserAgent = "provider-e2e" };
        var auth = CreateAuthStore(db, ctx);
        var (signup, user, _, rawToken) = await auth.SignupAsync(
            $"provider-{Guid.NewGuid():N}@example.com",
            "Provider User",
            "correct-horse-password",
            Constants.DefaultOrganizationId,
            TestContext.Current.CancellationToken);

        Assert.Equal(SignupResult.Success, signup);
        Assert.NotNull(user);
        Assert.NotNull(rawToken);
        Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == user!.Id && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
    }

    private static async Task SeedDefaultOrgAsync(ShipDb db)
    {
        if (await db.Orgs.AnyAsync(x => x.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken))
        {
            return;
        }

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
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AuthStore CreateAuthStore(ShipDb db, RequestContext ctx)
    {
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var snapshot = new PermissionSnapshotCodec();
        var sessions = new SessionStore(db, tokens, ctx, snapshot, NullLogger<SessionStore>.Instance);
        var audit = new AuditStore(db, ctx, NullLogger<AuditStore>.Instance);
        var apiKeys = new ApiKeyStore(db, NullLogger<ApiKeyStore>.Instance);
        var permissions = new PermissionResolver(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:PublicBaseUrl"] = "https://localhost",
        }).Build();

        return new AuthStore(db, passwords, sessions, audit, apiKeys, permissions, new TestEmailSender(), configuration, ctx, NullLogger<AuthStore>.Instance);
    }

    private static async Task StartOrSkipAsync(Task startTask)
    {
        try
        {
            await startTask;
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            Assert.Skip($"Docker is not available for Testcontainers: {ex.Message}");
        }
    }

    private static bool IsDockerUnavailable(Exception ex)
    {
        var message = ex.ToString();
        return message.Contains("Docker", StringComparison.OrdinalIgnoreCase)
            || message.Contains("docker", StringComparison.OrdinalIgnoreCase)
            || message.Contains("container runtime", StringComparison.OrdinalIgnoreCase);
    }
}