using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Migration)]
public class MigrationTests
{
    private static (ShipDb db, RequestContext ctx) CreateDatabase()
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

        var ctx = new RequestContext { IpAddress = "127.0.0.1", UserAgent = "test-agent" };
        return (db, ctx);
    }

    private static AuthStore CreateAuthStore(ShipDb db, RequestContext ctx)
    {
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens, ctx, NullLogger<SessionStore>.Instance);
        var audit = new AuditStore(db, ctx, NullLogger<AuditStore>.Instance);
        var apiKeys = new ApiKeyStore(db, NullLogger<ApiKeyStore>.Instance);
        return new AuthStore(db, passwords, sessions, audit, apiKeys, ctx, NullLogger<AuthStore>.Instance);
    }

    [Fact]
    public void CanCreateAndReadUser()
    {
        var (db, _) = CreateDatabase();
        var user = new User(Guid.NewGuid(), "test@example.com", "Test User")
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        db.SaveChanges();

        var found = db.Users.FirstOrDefault(u => u.Email == "test@example.com");
        Assert.NotNull(found);
        Assert.Equal("Test User", found.Name);
    }

    [Fact]
    public async Task Signup_CreatesUserAndSession()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        var (result, user, _, rawToken) = await auth.SignupAsync(
            "new@example.com", "New User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.Equal(SignupResult.Success, result);
        Assert.NotNull(user);
        Assert.NotNull(rawToken);
        Assert.Equal("new@example.com", user.Email);
    }

    [Fact]
    public async Task ApiKeyLogin_CreatesSessionAndReturnsSuccess()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);
        var apiKeys = new ApiKeyStore(db, NullLogger<ApiKeyStore>.Instance);

        var (_, user, _, _) = await auth.SignupAsync(
            "apikey@example.com", "Api Key User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.NotNull(user);

        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(
            user!.Id, "cli", null, "[]", DateTime.UtcNow.AddHours(1));

        db.UserApiKeys.Add(apiKey);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (result, loggedInUser, session, rawToken) = await auth.LoginWithUserApiKeyAsync(
            plaintextKey, TestContext.Current.CancellationToken);

        Assert.Equal(LoginResult.Success, result);
        Assert.NotNull(loggedInUser);
        Assert.NotNull(session);
        Assert.NotNull(rawToken);
        Assert.Equal(user.Id, loggedInUser!.Id);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        await auth.SignupAsync(
            "login@example.com", "Login User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        var (result, user, _, rawToken) = await auth.LoginAsync(
            "login@example.com", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.Equal(LoginResult.Success, result);
        Assert.NotNull(user);
        Assert.NotNull(rawToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        await auth.SignupAsync(
            "wrong@example.com", "Wrong User", "correct-password",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        var (result, user, _, _) = await auth.LoginAsync(
            "wrong@example.com", "wrong-password",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.Equal(LoginResult.InvalidCredentials, result);
        Assert.Null(user);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ReturnsEmailAlreadyExists()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        await auth.SignupAsync(
            "dup@example.com", "First", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        var (result, user, _, _) = await auth.SignupAsync(
            "dup@example.com", "Second", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.Equal(SignupResult.EmailAlreadyExists, result);
        Assert.Null(user);
    }

    [Fact]
    public async Task Session_CanBeValidated()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        var (_, _, _, rawToken) = await auth.SignupAsync(
            "session@example.com", "Session User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        Assert.NotNull(rawToken);
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens, ctx, NullLogger<SessionStore>.Instance);
        var session = await sessions.ValidateSessionAsync(rawToken!, TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task Session_CanBeRevoked()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        var (_, _, session, _) = await auth.SignupAsync(
            "revoke@example.com", "Revoke User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens, ctx, NullLogger<SessionStore>.Instance);
        await sessions.RevokeSessionAsync(session!.Id, "test", TestContext.Current.CancellationToken);

        var active = await sessions.ListSessionsAsync(session.UserId, TestContext.Current.CancellationToken);
        Assert.Empty(active);
    }

    [Fact]
    public async Task Audit_Events_AreRecorded()
    {
        var (db, ctx) = CreateDatabase();
        var auth = CreateAuthStore(db, ctx);

        await auth.SignupAsync(
            "audit@example.com", "Audit User", "password123",
            Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);

        var events = db.AuditEvents.Where(e => e.Type == "auth.signup.success").ToList();
        Assert.NotEmpty(events);
    }
}
