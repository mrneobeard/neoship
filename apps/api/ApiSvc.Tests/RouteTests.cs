using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Fido2NetLib;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NeoShip.ApiSvc;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Integration)]
[Trait(Traits.Category, Traits.Auth)]
[Trait(Traits.Category, Traits.Api)]
public sealed class RouteTests
{
    [Fact]
    public async Task BeginSso_ReturnsAuthorizationUrl()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sso-owner@example.com");
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                ClientId = "client-id",
                MetadataJson = "{\"authorization_endpoint\":\"https://idp.example.com/oauth2/authorize\"}",
            });
        });

        using var response = await app.Client.GetAsync("/api/v1/auth/sso/default/begin", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://idp.example.com/oauth2/authorize?", body, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BeginSso_ReturnsNotFoundForUnknownOrg()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.GetAsync("/api/v1/auth/sso/missing/begin", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenSsoIsRequired_ReturnsForbidden()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-password-blocked@example.com", "Password Blocked");
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = new PasswordStore().Hash("password123"),
            });
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.RequireSso = true;
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{\"email\":\"route-password-blocked@example.com\",\"password\":\"password123\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Signup_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/signup",
            new StringContent("{\"email\":\"not-email\",\"name\":\" \" ,\"password\":\"short\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("email", out _));
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("password", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.Users.AnyAsync(x => x.Email == "not-email", TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "auth.signup.success" || x.Type == "auth.signup.failed", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task Login_ReturnsAggregateValidationErrorsBeforeAuditWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{\"email\":\"not-email\",\"password\":\"\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("email", out _));
        Assert.True(fields.TryGetProperty("password", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "auth.login.failed" || x.Type == "auth.login.success", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ConfirmPasswordReset_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/password-reset/confirm",
            new StringContent("{\"token\":\" \" ,\"newPassword\":\"short\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("token", out _));
        Assert.True(fields.TryGetProperty("newPassword", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "auth.password_reset", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task FinishSso_SetsSessionCookieAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync(new SsoExternalIdentity("subject", "route-sso-user@example.com", true, "SSO User"));
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sso-user@example.com", "SSO User");
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 10,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                ClientId = "client-id",
            });
        });
        var state = await app.CreateSsoChallengeAsync(10);

        using var response = await app.Client.GetAsync($"/api/v1/auth/sso/callback?state={Uri.EscapeDataString(state)}&code=code", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith($"{AuthEndpoints.SessionCookieName}=", StringComparison.Ordinal));
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.UserSessions.AnyAsync(TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.sso.login", TestContext.Current.CancellationToken));
            Assert.True(await db.UserExternalIdentities.AnyAsync(x => x.Subject == "subject", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task GetExternalIdentities_RequiresAuthentication()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.GetAsync("/api/v1/me/external-identities", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUserApiKey_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-invalid@example.com", "User Key Invalid");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\" \" ,\"description\":\"{new string('x', 1025)}\",\"scopesJson\":\"not-json\",\"expiresAt\":\"2020-01-01T00:00:00Z\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("description", out _));
        Assert.True(fields.TryGetProperty("scopesJson", out _));
        Assert.True(fields.TryGetProperty("expiresAt", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.UserApiKeys.AnyAsync(x => x.UserId == userId, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ConfirmTotp_ReturnsAggregateValidationErrorsBeforeDbLookup()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-totp-invalid@example.com", "Totp Invalid");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/mfa/totp/confirm");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"factorId\":\"00000000-0000-0000-0000-000000000000\",\"code\":\"abc\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("factorId", out _));
        Assert.True(fields.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task FinishPasskeyRegistration_ReturnsAggregateValidationErrorsBeforeChallengeLookup()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-passkey-invalid@example.com", "Passkey Invalid");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/passkeys/finish-registration");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"challengeId\":\"00000000-0000-0000-0000-000000000000\",\"name\":\" \" ,\"response\":null}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("challengeId", out _));
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("response", out _));
    }

    [Fact]
    public async Task GetExternalIdentities_ReturnsLinkedIdentitiesForSession()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-linked@example.com", "Linked User");
            userId = user.Id;
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 10,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                ClientId = "client-id",
            });
            db.UserExternalIdentities.Add(new UserExternalIdentity
            {
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                ProviderId = 10,
                Subject = "subject",
                SubjectDigest = TokenStore.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject")),
                Email = "route-linked@example.com",
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/external-identities");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Acme OIDC", body, StringComparison.Ordinal);
        Assert.Contains("subject", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnlinkExternalIdentity_ReturnsConflictForLastSignInMethod()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var linkId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-conflict@example.com", "Conflict User");
            userId = user.Id;
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 10,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                ClientId = "client-id",
            });
            db.UserExternalIdentities.Add(new UserExternalIdentity
            {
                Id = linkId,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                ProviderId = 10,
                Subject = "subject",
                SubjectDigest = TokenStore.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject")),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/me/external-identities/{linkId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAuthPolicy_ReturnsPolicyForUserWithSettingsRead()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-policy-read@example.com", "Policy Reader");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.read");
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.RequireSso = true;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/auth-policy");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"requireSso\":true", body, StringComparison.Ordinal);
        Assert.Contains("\"allowPasswordAuth\":true", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAuthPolicy_RequiresSettingsWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-policy-forbid@example.com", "Policy No Write");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/orgs/default/auth-policy");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"requireSso\":true}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAuthPolicy_UpdatesPolicyAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-policy-write@example.com", "Policy Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/orgs/default/auth-policy");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent(
            "{\"allowPasswordAuth\":false,\"allowOidcSso\":false,\"requireSso\":true,\"allowSelfServiceExternalIdentityUnlink\":false}",
            Encoding.UTF8,
            "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"allowPasswordAuth\":false", body, StringComparison.Ordinal);
        Assert.Contains("\"allowOidcSso\":false", body, StringComparison.Ordinal);
        Assert.Contains("\"requireSso\":true", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            var org = await db.Orgs.SingleAsync(o => o.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);
            Assert.False(org.AllowPasswordAuth);
            Assert.False(org.AllowOidcSso);
            Assert.True(org.RequireSso);
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.auth_policy.update", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListServiceAccounts_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false);
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/service-accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("deploy-bot", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListServiceAccounts_RejectsServiceAccountBearerWithoutReadClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: false, disabled: false);
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/service-accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListServiceAccounts_RejectsDisabledServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: true);
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/service-accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAccount_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.service_accounts.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/service-accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        request.Content = new StringContent("{\"name\":\"new-bot\",\"description\":\"New bot\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAccount_WithHumanWritePermissionWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-writer@example.com", "Service Account Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/service-accounts");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"new-bot\",\"description\":\"New bot\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("new-bot", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.ServiceAccounts.AnyAsync(x => x.Name == "new-bot", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateServiceAccount_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-invalid@example.com", "Service Account Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/service-accounts");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\" \" ,\"description\":\"{new string('x', 1025)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("description", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.ServiceAccounts.AnyAsync(x => x.CreatedBy == userId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateServiceAccountApiKey_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-invalid@example.com", "Service Account Key Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            db.ServiceAccounts.Add(new ServiceAccount
            {
                Id = serviceAccountId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "key-bot",
                NameUpcase = "KEY-BOT",
                Description = "Key bot",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{serviceAccountId}/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\" \" ,\"description\":\"{new string('x', 1025)}\",\"scopesJson\":\"not-json\",\"expiresAt\":\"2020-01-01T00:00:00Z\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("description", out _));
        Assert.True(fields.TryGetProperty("scopesJson", out _));
        Assert.True(fields.TryGetProperty("expiresAt", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.ServiceAccountApiKeys.AnyAsync(x => x.ServiceAccountId == serviceAccountId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListServiceAccountClaims_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: true, disabled: false);
            db.ServiceAccountClaims.Add(new ServiceAccountClaim
            {
                Id = Guid.CreateVersion7(),
                ServiceAccountId = seed.ServiceAccountId,
                Type = "org.service_accounts.read",
                Value = "organization:default",
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddServiceAccountClaim_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: true, disabled: false, claimType: "org.service_accounts.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        request.Content = new StringContent("{\"permission\":\"org.service_accounts.read\",\"scopeKind\":2,\"scopeId\":\"default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddServiceAccountClaim_WithHumanWritePermissionWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-claim-writer@example.com", "Service Account Claim Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            var serviceAccount = new ServiceAccount
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "claim-bot",
                NameUpcase = "CLAIM-BOT",
                Description = "Claim bot",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            };
            serviceAccountId = serviceAccount.Id;
            db.ServiceAccounts.Add(serviceAccount);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{serviceAccountId}/claims");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"permission\":\"org.service_accounts.read\",\"scopeKind\":2,\"scopeId\":\"default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.ServiceAccountClaims.AnyAsync(x => x.ServiceAccountId == serviceAccountId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.claim.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AddServiceAccountClaim_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-claim-invalid@example.com", "Service Account Claim Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            var serviceAccount = new ServiceAccount
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "claim-invalid-bot",
                NameUpcase = "CLAIM-INVALID-BOT",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            };
            serviceAccountId = serviceAccount.Id;
            db.ServiceAccounts.Add(serviceAccount);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{serviceAccountId}/claims");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"permission\":\"not-valid\",\"scopeKind\":999,\"scopeId\":\"{new string('x', 161)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("permission", out _));
        Assert.True(fields.TryGetProperty("scopeKind", out _));
        Assert.True(fields.TryGetProperty("scopeId", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.ServiceAccountClaims.AnyAsync(x => x.ServiceAccountId == serviceAccountId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.claim.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListServiceAccountApiKeyClaims_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: true, disabled: false);
            db.ServiceAccountApiKeyClaims.Add(new ServiceAccountApiKeyClaim
            {
                ServiceAccountApiKeyId = seed.ApiKeyId,
                Type = "org.service_accounts.read",
                Value = "organization:default",
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddServiceAccountApiKeyClaim_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: true, disabled: false, claimType: "org.service_accounts.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        request.Content = new StringContent("{\"permission\":\"org.service_accounts.read\",\"scopeKind\":2,\"scopeId\":\"default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddServiceAccountApiKeyClaim_WithHumanWritePermissionWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-claim-writer@example.com", "Service Account Key Claim Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"permission\":\"org.service_accounts.read\",\"scopeKind\":2,\"scopeId\":\"default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.ServiceAccountApiKeyClaims.AnyAsync(x => x.ServiceAccountApiKeyId == seed.ApiKeyId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.claim.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AddServiceAccountApiKeyClaim_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-claim-invalid@example.com", "Service Account Key Claim Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"permission\":\"not-valid\",\"scopeKind\":999,\"scopeId\":\"{new string('x', 161)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("permission", out _));
        Assert.True(fields.TryGetProperty("scopeKind", out _));
        Assert.True(fields.TryGetProperty("scopeId", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.ServiceAccountApiKeyClaims.AnyAsync(x => x.ServiceAccountApiKeyId == seed.ApiKeyId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.claim.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListPermissions_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.roles.read");
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListPermissions_RejectsServiceAccountBearerWithoutRolesRead()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.service_accounts.read");
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetIdentityProvider_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-owner@example.com", "IDP Owner");
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.identity_providers.read");
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 20,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                IssuerUrl = "https://idp.example.com",
                ClientId = "client-id",
            });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/identity-providers/20");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Acme OIDC", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateIdentityProvider_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.identity_providers.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/identity-providers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        request.Content = new StringContent("{\"name\":\"Acme OIDC\",\"providerType\":\"oidc\",\"issuerUrl\":\"https://idp.example.com\",\"clientId\":\"client-id\",\"metadataJson\":\"{}\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdentityProvider_WithHumanWritePermissionWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-writer@example.com", "IDP Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Acme OIDC\",\"providerType\":\"oidc\",\"issuerUrl\":\"https://idp.example.com\",\"clientId\":\"client-id\",\"metadataJson\":\"{}\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Acme OIDC", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.UserIdentityProviders.AnyAsync(x => x.Name == "Acme OIDC", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.identity_providers.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateIdentityProvider_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-invalid@example.com", "IDP Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\" \" ,\"providerType\":\"ldap\",\"issuerUrl\":\"http://idp.example.com\",\"clientId\":\" \" ,\"metadataJson\":\"not-json\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("providerType", out _));
        Assert.True(fields.TryGetProperty("issuerUrl", out _));
        Assert.True(fields.TryGetProperty("clientId", out _));
        Assert.True(fields.TryGetProperty("metadataJson", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.UserIdentityProviders.AnyAsync(x => x.UserId == userId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.identity_providers.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListRoles_AllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-owner@example.com", "Role Owner");
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.roles.read");
            db.Roles.Add(new Role
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "Reader",
                NameUpcase = "READER",
                Description = "Read access",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs/default/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Reader", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateRole_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.roles.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        request.Content = new StringContent("{\"name\":\"Writer\",\"description\":\"Write access\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-invalid@example.com", "Role Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/roles");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\" \" ,\"description\":\"{new string('x', 1025)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("description", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.Roles.AnyAsync(x => x.CreatedBy == userId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.roles.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AddRoleClaim_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var roleId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-claim-invalid@example.com", "Role Claim Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
            db.Roles.Add(new Role
            {
                Id = roleId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "Writer",
                NameUpcase = "WRITER",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orgs/default/roles/{roleId}/claims");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"permission\":\"not-valid\",\"scopeKind\":999,\"scopeId\":\"{new string('x', 161)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("permission", out _));
        Assert.True(fields.TryGetProperty("scopeKind", out _));
        Assert.True(fields.TryGetProperty("scopeId", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.RoleClaims.AnyAsync(x => x.RoleId == roleId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.roles.claim.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateGroup_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-invalid@example.com", "Group Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/groups");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\" \" ,\"email\":\"not-email\",\"description\":\"{new string('x', 1025)}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("email", out _));
        Assert.True(fields.TryGetProperty("description", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.Groups.AnyAsync(x => x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.groups.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateInvite_WithHumanWritePermissionReturnsTokenAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-invite-writer@example.com", "Invite Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/invites");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"email\":\"invited@example.com\",\"roleIds\":[],\"groupIds\":[]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("invited@example.com", body, StringComparison.Ordinal);
        Assert.Contains("token", body, StringComparison.OrdinalIgnoreCase);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.OrganizationInvites.AnyAsync(x => x.Email == "invited@example.com", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateInvite_RejectsServiceAccountBearerEvenWithMembersWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.members.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/invites");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        request.Content = new StringContent("{\"email\":\"invited@example.com\",\"roleIds\":[],\"groupIds\":[]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvite_ReturnsAggregateValidationErrorsBeforeDbWrite()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-invite-invalid@example.com", "Invite Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orgs/default/invites");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"email\":\"not-email\",\"roleIds\":[\"00000000-0000-0000-0000-000000000000\"],\"groupIds\":[\"11111111-1111-1111-1111-111111111111\",\"11111111-1111-1111-1111-111111111111\"]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("email", out _));
        Assert.True(fields.TryGetProperty("roleIds", out _));
        Assert.True(fields.TryGetProperty("groupIds", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.OrganizationInvites.AnyAsync(x => x.InvitedByUserId == userId, TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AcceptInvite_CreatesMembershipAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        const string rawToken = "invite-token";
        await app.SeedAsync(db =>
        {
            var inviter = SeedUser(db, "route-invite-owner@example.com", "Invite Owner");
            var invited = SeedUser(db, "route-invited@example.com", "Invited User");
            userId = invited.Id;
            db.OrganizationInvites.Add(new OrganizationInvite
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                InvitedByUserId = inviter.Id,
                Email = invited.Email,
                EmailUpcase = invited.EmailUpcase,
                TokenDigest = TokenStore.ComputeDigestBase64(rawToken),
                PendingRoleIdsJson = "[]",
                PendingGroupIdsJson = "[]",
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/invites/accept");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"token\":\"invite-token\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == userId && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.accept", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AcceptInvite_ReturnsValidationErrorBeforeDbLookup()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-invite-token-invalid@example.com", "Invite Token Invalid");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/invites/accept");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"token\":\" \"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields").TryGetProperty("token", out _));
    }

    [Fact]
    public async Task SwitchOrg_RequiresMembershipAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var otherOrgId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-switch@example.com", "Switch User");
            userId = user.Id;
            db.Orgs.Add(new Organization
            {
                Id = otherOrgId,
                Name = "Other",
                NameUpcase = "OTHER",
                Slug = "other",
                StatusId = OrganizationStatus.Active.Id,
                TenantModeId = TenantMode.Multi.Id,
                OrganizationPlanId = 1,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/me/orgs/{otherOrgId}/switch");
        firstRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var firstResponse = await app.Client.SendAsync(firstRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, firstResponse.StatusCode);

        await app.SeedAsync(db =>
        {
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = otherOrgId,
                UserId = userId,
            });
        });

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/me/orgs/{otherOrgId}/switch");
        secondRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var secondResponse = await app.Client.SendAsync(secondRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.Equal(otherOrgId, (await db.Users.SingleAsync(x => x.Id == userId, TestContext.Current.CancellationToken)).OrgId);
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.switch", TestContext.Current.CancellationToken));
        });
    }

    private static User SeedUser(ShipDb db, string email, string name = "Route User")
    {
        var user = new User(Guid.CreateVersion7(), email, name)
        {
            OrgId = Constants.DefaultOrganizationId,
        };

        db.Users.Add(user);
        return user;
    }

    private static string SeedServiceAccountBearer(ShipDb db, bool includeReadClaim, bool disabled, string claimType = "org.service_accounts.read")
    {
        return SeedServiceAccountBearerContext(db, includeReadClaim, disabled, claimType).PlaintextKey;
    }

    private static ServiceAccountBearerSeed SeedServiceAccountBearerContext(ShipDb db, bool includeReadClaim, bool disabled, string claimType = "org.service_accounts.read")
    {
        var user = SeedUser(db, $"sa-owner-{Guid.NewGuid():N}@example.com", "Service Account Owner");
        var serviceAccount = new ServiceAccount
        {
            Id = Guid.CreateVersion7(),
            OrgId = Constants.DefaultOrganizationId,
            Name = "deploy-bot",
            NameUpcase = "DEPLOY-BOT",
            Description = "Deploy bot",
            CreatedBy = user.Id,
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DeletedAt = disabled ? DateTime.UtcNow : null,
        };
        db.ServiceAccounts.Add(serviceAccount);

        var store = new ServiceAccountStore(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)), Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceAccountStore>.Instance);
        var (plaintextKey, apiKey) = store.GenerateApiKey(serviceAccount.Id, "ci", null, "[]", DateTime.UtcNow.AddHours(1));
        db.ServiceAccountApiKeys.Add(apiKey);

        if (includeReadClaim)
        {
            db.ServiceAccountApiKeyClaims.Add(new ServiceAccountApiKeyClaim
            {
                ServiceAccountApiKeyId = apiKey.Id,
                Type = claimType,
                Value = "organization:default",
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        }

        return new ServiceAccountBearerSeed(plaintextKey, serviceAccount.Id, apiKey.Id);
    }

    private readonly record struct ServiceAccountBearerSeed(string PlaintextKey, Guid ServiceAccountId, Guid ApiKeyId);

    private static void GrantUserOrgPermission(ShipDb db, Guid userId, string permission)
    {
        db.UserClaims.Add(new UserClaim
        {
            UserId = userId,
            Type = permission,
            Value = "organization:default",
        });
    }

    private sealed class RouteTestApp : IAsyncDisposable
    {
        private readonly IHost host;

        private RouteTestApp(IHost host)
        {
            this.host = host;
            this.Client = host.GetTestClient();
            this.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public HttpClient Client { get; }

        public static async Task<RouteTestApp> CreateAsync(SsoExternalIdentity? externalIdentity = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var fakeTokenValidator = new FakeSsoTokenValidator(externalIdentity);
            var host = await new HostBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddProblemDetails();
                        services.AddLogging();
                        services.AddMemoryCache();
                        services.AddSingleton(connection);
                        services.AddDbContext<ShipDb>((sp, options) => options
                            .UseSqlite(sp.GetRequiredService<SqliteConnection>())
                            .UseSnakeCaseNamingConvention());
                        services.AddScoped<RequestContext>();
                        services.AddSingleton(new PermissionRegistry(CorePermissions.All));
                        services.AddSingleton<PermissionClaimCodec>(sp => new PermissionClaimCodec(sp.GetRequiredService<PermissionRegistry>()));
                        services.AddSingleton<PermissionSnapshotCodec>();
                        services.AddScoped<PermissionResolver>();
                        services.AddSingleton<PasswordStore>();
                        services.AddSingleton<TokenStore>();
                        services.AddSingleton<TokenExchangeStore>();
                        services.AddScoped<SessionStore>();
                        services.AddScoped<AuthStore>();
                        services.AddScoped<AuditStore>();
                        services.AddScoped<ApiKeyStore>();
                        services.AddScoped<OrganizationStore>();
                        services.AddScoped<OrganizationInviteStore>();
                        services.AddScoped<RoleStore>();
                        services.AddScoped<GroupStore>();
                        services.AddScoped<ServiceAccountStore>();
                        services.AddSingleton<IdentityProviderSecretProtector>();
                        services.AddScoped<IdentityProviderStore>();
                        services.AddScoped<MfaStore>();
                        services.AddSingleton(new Fido2(new Fido2Configuration
                        {
                            ServerDomain = "localhost",
                            ServerName = "NeoShip",
                            Origins = new HashSet<string> { "https://localhost", "http://localhost" },
                        }, metadataService: null));
                        services.AddScoped<PasskeyStore>();
                        services.AddSingleton<PasskeyChallengeStore>();
                        services.AddSingleton<SsoChallengeStore>();
                        services.AddSingleton<ISsoTokenClient, FakeSsoTokenClient>();
                        services.AddSingleton<ISsoTokenValidator>(fakeTokenValidator);
                        services.AddScoped<SsoStore>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapAuthEndpoints();
                            endpoints.MapMeEndpoints();
                            endpoints.MapOrgEndpoints();
                        });
                    }))
                .StartAsync(TestContext.Current.CancellationToken);

            var app = new RouteTestApp(host);
            await app.SeedDefaultOrgAsync();
            return app;
        }

        public async Task SeedAsync(Action<ShipDb> seed)
        {
            await using var scope = this.host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ShipDb>();
            seed(db);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async Task WithDbAsync(Func<ShipDb, Task> action)
        {
            await using var scope = this.host.Services.CreateAsyncScope();
            await action(scope.ServiceProvider.GetRequiredService<ShipDb>());
        }

        public async Task<string> CreateSessionAsync(Guid userId)
        {
            await using var scope = this.host.Services.CreateAsyncScope();
            var sessions = scope.ServiceProvider.GetRequiredService<SessionStore>();
            var (_, rawToken) = await sessions.CreateSessionAsync(userId, Constants.DefaultOrganizationId, ct: TestContext.Current.CancellationToken);
            return rawToken;
        }

        public async Task<string> CreateSsoChallengeAsync(long providerId)
        {
            await using var scope = this.host.Services.CreateAsyncScope();
            var challenges = scope.ServiceProvider.GetRequiredService<SsoChallengeStore>();
            return challenges.Create(Constants.DefaultOrganizationId, providerId, "https://localhost/api/v1/auth/sso/callback", "nonce").State;
        }

        public async ValueTask DisposeAsync()
        {
            this.Client.Dispose();
            var connection = this.host.Services.GetRequiredService<SqliteConnection>();
            await this.host.StopAsync(TestContext.Current.CancellationToken);
            this.host.Dispose();
            await connection.DisposeAsync();
        }

        private async Task SeedDefaultOrgAsync()
        {
            await using var scope = this.host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ShipDb>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
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

        public FakeSsoTokenValidator(SsoExternalIdentity? identity)
        {
            this.identity = identity;
        }

        public Task<SsoExternalIdentity?> ValidateAsync(UserIdentityProvider provider, string idToken, string nonce, CancellationToken ct = default)
        {
            return Task.FromResult(this.identity);
        }
    }
}
