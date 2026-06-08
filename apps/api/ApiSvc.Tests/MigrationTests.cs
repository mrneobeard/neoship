using Microsoft.EntityFrameworkCore;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Migration)]
public class MigrationTests
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
    public void CanCreateAndReadUser()
    {
        using var db = CreateDatabase();
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
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        var (result, user, _, rawToken) = await auth.SignupAsync(
            "new@example.com", "New User", "password123",
            Constants.DefaultOrganizationId, "127.0.0.1", "test-agent", TestContext.Current.CancellationToken);

        Assert.Equal(SignupResult.Success, result);
        Assert.NotNull(user);
        Assert.NotNull(rawToken);
        Assert.Equal("new@example.com", user.Email);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        await auth.SignupAsync(
            "login@example.com", "Login User", "password123",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        var (result, user, _, rawToken) = await auth.LoginAsync(
            "login@example.com", "password123",
            Constants.DefaultOrganizationId, "127.0.0.1", "test-agent", TestContext.Current.CancellationToken);

        Assert.Equal(LoginResult.Success, result);
        Assert.NotNull(user);
        Assert.NotNull(rawToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        await auth.SignupAsync(
            "wrong@example.com", "Wrong User", "correct-password",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        var (result, user, _, _) = await auth.LoginAsync(
            "wrong@example.com", "wrong-password",
            Constants.DefaultOrganizationId, "127.0.0.1", "test-agent", TestContext.Current.CancellationToken);

        Assert.Equal(LoginResult.InvalidCredentials, result);
        Assert.Null(user);
    }

    [Fact]
    public async Task Signup_DuplicateEmail_ReturnsEmailAlreadyExists()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        await auth.SignupAsync(
            "dup@example.com", "First", "password123",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        var (result, user, _, _) = await auth.SignupAsync(
            "dup@example.com", "Second", "password123",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        Assert.Equal(SignupResult.EmailAlreadyExists, result);
        Assert.Null(user);
    }

    [Fact]
    public async Task Session_CanBeValidated()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        var (_, _, _, rawToken) = await auth.SignupAsync(
            "session@example.com", "Session User", "password123",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        Assert.NotNull(rawToken);
        var session = await sessions.ValidateSessionAsync(rawToken!, TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task Session_CanBeRevoked()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        var (_, _, session, _) = await auth.SignupAsync(
            "revoke@example.com", "Revoke User", "password123",
            Constants.DefaultOrganizationId, null, null, TestContext.Current.CancellationToken);

        await sessions.RevokeSessionAsync(session!.Id, "test", TestContext.Current.CancellationToken);

        var active = await sessions.ListSessionsAsync(session.UserId, TestContext.Current.CancellationToken);
        Assert.Empty(active);
    }

    [Fact]
    public async Task Audit_Events_AreRecorded()
    {
        using var db = CreateDatabase();
        var passwords = new PasswordStore();
        var tokens = new TokenStore();
        var sessions = new SessionStore(db, tokens);
        var audit = new AuditStore(db);
        var auth = new AuthStore(db, passwords, sessions, audit);

        await auth.SignupAsync(
            "audit@example.com", "Audit User", "password123",
            Constants.DefaultOrganizationId, "127.0.0.1", "test-agent", TestContext.Current.CancellationToken);

        var events = db.AuditEvents.Where(e => e.Type == "auth.signup.success").ToList();
        Assert.NotEmpty(events);
    }
}
