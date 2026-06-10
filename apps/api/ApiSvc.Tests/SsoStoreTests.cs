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
        var store = new SsoStore(db, challenges, new FakeSsoTokenClient(), new FakeSsoTokenValidator());
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
        var store = new SsoStore(db, new SsoChallengeStore(cache), new FakeSsoTokenClient(), new FakeSsoTokenValidator());
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

    /// <summary>
    /// Verifies organization policy can block OIDC begin.
    /// </summary>
    [Fact]
    public async Task BeginOidcAsync_WhenOidcIsDisabled_ReturnsNull()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new SsoStore(db, new SsoChallengeStore(cache), new FakeSsoTokenClient(), new FakeSsoTokenValidator());
        var org = await db.Orgs.SingleAsync(TestContext.Current.CancellationToken);
        org.AllowOidcSso = false;
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-disabled@example.com", "SSO Disabled")
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

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies OIDC finish signs in an existing active user by verified email.
    /// </summary>
    [Fact]
    public async Task FinishOidcAsync_ReturnsExistingUserForVerifiedEmail()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-user@example.com", "SSO User")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
            MetadataJson = "{\"authorization_endpoint\":\"https://idp.example.com/oauth2/authorize\"}",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var challenge = challenges.Create(Constants.DefaultOrganizationId, 10, "https://app.example.com/api/v1/auth/sso/callback", "nonce");
        var store = new SsoStore(
            db,
            challenges,
            new FakeSsoTokenClient(),
            new FakeSsoTokenValidator(new SsoExternalIdentity("subject", "sso-user@example.com", true, "SSO User")));

        var user = await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal(userId, user!.Id);
        Assert.NotNull(user.LastLoginAt);
        var link = await db.UserExternalIdentities.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(userId, link.UserId);
        Assert.Equal("subject", link.Subject);
        Assert.NotEmpty(link.SubjectDigest);
        Assert.NotNull(link.LastUsedAt);
        Assert.Null(await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies OIDC finish provisions a user when the provider supplies a verified email.
    /// </summary>
    [Fact]
    public async Task FinishOidcAsync_ProvisionsUserForVerifiedEmail()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var adminId = Guid.NewGuid();
        db.Users.Add(new User(adminId, "sso-owner@example.com", "SSO Owner")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = adminId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var challenge = challenges.Create(Constants.DefaultOrganizationId, 10, "https://app.example.com/api/v1/auth/sso/callback", "nonce");
        var store = new SsoStore(
            db,
            challenges,
            new FakeSsoTokenClient(),
            new FakeSsoTokenValidator(new SsoExternalIdentity("new-subject", "new-sso-user@example.com", true, "New SSO User")));

        var user = await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal("new-sso-user@example.com", user!.Email);
        Assert.Equal("New SSO User", user.Name);
        Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == user.Id && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
        Assert.True(await db.UserEmails.AnyAsync(x => x.UserId == user.Id && x.VerifiedAt != null, TestContext.Current.CancellationToken));
        Assert.True(await db.UserExternalIdentities.AnyAsync(x => x.UserId == user.Id && x.Subject == "new-subject", TestContext.Current.CancellationToken));
        Assert.True(await db.Users.Where(x => x.Id == user.Id).SelectMany(x => x.Roles).AnyAsync(x => x.Name == BuiltInRoleStore.MemberRoleName, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies OIDC finish prefers an existing external identity link over email matching.
    /// </summary>
    [Fact]
    public async Task FinishOidcAsync_UsesExistingSubjectLink()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var linkedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Users.Add(new User(linkedUserId, "linked-user@example.com", "Linked User")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.Users.Add(new User(otherUserId, "new-email@example.com", "Other User")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = linkedUserId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        db.UserExternalIdentities.Add(new UserExternalIdentity
        {
            OrgId = Constants.DefaultOrganizationId,
            UserId = linkedUserId,
            ProviderId = 10,
            Subject = "subject",
            SubjectDigest = TokenStore.ComputeDigestBase64(System.Text.Encoding.UTF8.GetBytes("subject")),
            Email = "linked-user@example.com",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var challenge = challenges.Create(Constants.DefaultOrganizationId, 10, "https://app.example.com/api/v1/auth/sso/callback", "nonce");
        var store = new SsoStore(
            db,
            challenges,
            new FakeSsoTokenClient(),
            new FakeSsoTokenValidator(new SsoExternalIdentity("subject", "new-email@example.com", true, "SSO User")));

        var user = await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.Equal(linkedUserId, user!.Id);
        var link = await db.UserExternalIdentities.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("new-email@example.com", link.Email);
        Assert.NotNull(link.LastUsedAt);
    }

    [Fact]
    public async Task UnlinkExternalIdentityAsync_RejectsLastSignInMethod()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var userId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        db.Users.Add(new User(userId, "unlink-user@example.com", "Unlink User")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        db.UserExternalIdentities.Add(new UserExternalIdentity
        {
            Id = linkId,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            ProviderId = 10,
            Subject = "subject",
            SubjectDigest = TokenStore.ComputeDigestBase64(System.Text.Encoding.UTF8.GetBytes("subject")),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new SsoStore(db, new SsoChallengeStore(cache), new FakeSsoTokenClient(), new FakeSsoTokenValidator());
        var result = await store.UnlinkExternalIdentityAsync(userId, linkId, TestContext.Current.CancellationToken);

        Assert.Equal(SsoExternalIdentityUnlinkResult.LastMethod, result);
        Assert.NotNull(await db.UserExternalIdentities.FindAsync([linkId], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnlinkExternalIdentityAsync_AllowsWhenPasswordExists()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var userId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        db.Users.Add(new User(userId, "unlink-password@example.com", "Unlink Password")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserPasswordAuths.Add(new UserPasswordAuth
        {
            UserId = userId,
            PasswordHash = "hash",
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        db.UserExternalIdentities.Add(new UserExternalIdentity
        {
            Id = linkId,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            ProviderId = 10,
            Subject = "subject",
            SubjectDigest = TokenStore.ComputeDigestBase64(System.Text.Encoding.UTF8.GetBytes("subject")),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new SsoStore(db, new SsoChallengeStore(cache), new FakeSsoTokenClient(), new FakeSsoTokenValidator());
        var result = await store.UnlinkExternalIdentityAsync(userId, linkId, TestContext.Current.CancellationToken);

        Assert.Equal(SsoExternalIdentityUnlinkResult.Success, result);
        Assert.Null(await db.UserExternalIdentities.FindAsync([linkId], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnlinkExternalIdentityAsync_RespectsOrganizationPolicy()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var userId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        var org = await db.Orgs.SingleAsync(TestContext.Current.CancellationToken);
        org.AllowSelfServiceExternalIdentityUnlink = false;
        db.Users.Add(new User(userId, "unlink-policy@example.com", "Unlink Policy")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserPasswordAuths.Add(new UserPasswordAuth
        {
            UserId = userId,
            PasswordHash = "hash",
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        db.UserExternalIdentities.Add(new UserExternalIdentity
        {
            Id = linkId,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            ProviderId = 10,
            Subject = "subject",
            SubjectDigest = TokenStore.ComputeDigestBase64(System.Text.Encoding.UTF8.GetBytes("subject")),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var store = new SsoStore(db, new SsoChallengeStore(cache), new FakeSsoTokenClient(), new FakeSsoTokenValidator());
        var result = await store.UnlinkExternalIdentityAsync(userId, linkId, TestContext.Current.CancellationToken);

        Assert.Equal(SsoExternalIdentityUnlinkResult.PolicyDenied, result);
        Assert.NotNull(await db.UserExternalIdentities.FindAsync([linkId], TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies OIDC finish rejects unverified email identities.
    /// </summary>
    [Fact]
    public async Task FinishOidcAsync_RejectsUnverifiedEmail()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-user@example.com", "SSO User")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var challenge = challenges.Create(Constants.DefaultOrganizationId, 10, "https://app.example.com/api/v1/auth/sso/callback", "nonce");
        var store = new SsoStore(
            db,
            challenges,
            new FakeSsoTokenClient(),
            new FakeSsoTokenValidator(new SsoExternalIdentity("subject", "sso-user@example.com", false, "SSO User")));

        var user = await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken);

        Assert.Null(user);
    }

    /// <summary>
    /// Verifies organization policy can block OIDC callback even after a challenge exists.
    /// </summary>
    [Fact]
    public async Task FinishOidcAsync_WhenOidcIsDisabled_ReturnsNull()
    {
        await using var db = CreateDatabase();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var challenges = new SsoChallengeStore(cache);
        var org = await db.Orgs.SingleAsync(TestContext.Current.CancellationToken);
        org.AllowOidcSso = false;
        var userId = Guid.NewGuid();
        db.Users.Add(new User(userId, "sso-callback-disabled@example.com", "SSO Disabled")
        {
            OrgId = Constants.DefaultOrganizationId,
        });
        db.UserIdentityProviders.Add(new UserIdentityProvider
        {
            Id = 10,
            OrgId = Constants.DefaultOrganizationId,
            UserId = userId,
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var challenge = challenges.Create(Constants.DefaultOrganizationId, 10, "https://app.example.com/api/v1/auth/sso/callback", "nonce");
        var store = new SsoStore(
            db,
            challenges,
            new FakeSsoTokenClient(),
            new FakeSsoTokenValidator(new SsoExternalIdentity("subject", "sso-callback-disabled@example.com", true, "SSO User")));

        var user = await store.FinishOidcAsync(challenge.State, "code", TestContext.Current.CancellationToken);

        Assert.Null(user);
    }

    private sealed class FakeSsoTokenClient : ISsoTokenClient
    {
        public Task<SsoTokenResponse?> ExchangeAsync(UserIdentityProvider provider, string code, string redirectUri, CancellationToken ct = default)
        {
            return Task.FromResult<SsoTokenResponse?>(new SsoTokenResponse("id-token"));
        }
    }

    private sealed class FakeSsoTokenValidator : ISsoTokenValidator
    {
        private readonly SsoExternalIdentity? identity;

        public FakeSsoTokenValidator(SsoExternalIdentity? identity = null)
        {
            this.identity = identity;
        }

        public Task<SsoExternalIdentity?> ValidateAsync(UserIdentityProvider provider, string idToken, string nonce, CancellationToken ct = default)
        {
            return Task.FromResult(this.identity);
        }
    }
}
