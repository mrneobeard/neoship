using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using Fido2NetLib;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NeoShip.ApiSvc;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Lib.Iam;
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
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("data", out _));
        Assert.True(json.RootElement.TryGetProperty("meta", out _));
        Assert.Contains("https://idp.example.com/oauth2/authorize?", body, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApi_GeneratesIamRoutes()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/auth/login", body, StringComparison.Ordinal);
        Assert.Contains("/api/v1/me/api-keys", body, StringComparison.Ordinal);
        Assert.Contains("/api/v1/service-accounts", body, StringComparison.Ordinal);
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
                PasswordHash = PasswordHashing.Hash("password123"),
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
    public async Task Login_WhenOrgRequiresMfaEnrollment_ReturnsMfaRequired()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-mfa-required@example.com", "MFA Required");
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = PasswordHashing.Hash("password123"),
            });
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.MfaPolicyId = OrganizationMfaPolicy.AllMembers.Id;
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{\"email\":\"route-mfa-required@example.com\",\"password\":\"password123\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("mfa_required", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Signup_WhenPasswordAuthIsDisabled_ReturnsForbidden()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.AllowPasswordAuth = false;
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/signup",
            new StringContent("{\"email\":\"blocked-signup@example.com\",\"name\":\"Blocked\",\"password\":\"password123456\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.Users.AnyAsync(x => x.Email == "blocked-signup@example.com", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.signup.failed", TestContext.Current.CancellationToken));
        });
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
    public async Task Login_WithInvalidCredentials_ReturnsCanonicalErrorEnvelope()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{\"email\":\"missing-login@example.com\",\"password\":\"not-the-password\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("error", out var error));
        Assert.True(json.RootElement.TryGetProperty("meta", out _));
        Assert.Equal("unauthenticated", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TokenExchange_WritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-token-exchange@example.com", "Token Exchange");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token-exchange");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.token_exchange", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task Login_IsRateLimitedAfterRepeatedAttempts()
    {
        await using var app = await RouteTestApp.CreateAsync();
        HttpResponseMessage? response = null;

        for (var i = 0; i < 6; i++)
        {
            response?.Dispose();
            response = await app.Client.PostAsync(
                "/api/v1/auth/login",
                new StringContent("{\"email\":\"missing-login@example.com\",\"password\":\"not-the-password\"}", Encoding.UTF8, "application/json"),
                TestContext.Current.CancellationToken);
        }

        using (response)
        {
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
        }
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
    public async Task PasswordResetRequest_WritesGenericAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/password-reset/request",
            new StringContent("{\"email\":\"unknown-reset@example.com\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.password_reset.request", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task PasswordResetRequest_SendsResetEmailForExistingPasswordUser()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-reset-mail@example.com", "Reset Mail");
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = PasswordHashing.Hash("current-password"),
            });
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/password-reset/request",
            new StringContent("{\"email\":\"route-reset-mail@example.com\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = Assert.Single(app.EmailSender.Messages);
        Assert.Equal("route-reset-mail@example.com", message.To);
        Assert.Contains("reset-password?token=", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PasswordResetRequest_WhenPasswordAuthDisabled_DoesNotSendResetEmail()
    {
        await using var app = await RouteTestApp.CreateAsync();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-reset-disabled@example.com", "Reset Disabled");
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = PasswordHashing.Hash("current-password"),
            });
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.AllowPasswordAuth = false;
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/password-reset/request",
            new StringContent("{\"email\":\"route-reset-disabled@example.com\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(app.EmailSender.Messages);
        await app.WithDbAsync(async db =>
        {
            var auth = await db.UserPasswordAuths.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Null(auth.ResetTokenDigest);
        });
    }

    [Fact]
    public async Task ApiKeyLogin_WhenSsoRequired_ReturnsForbidden()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-api-key-policy@example.com", "API Key Policy");
            var store = TestUserStore.Create(db);
            var (rawKey, apiKey) = store.GenerateUserApiKey(user.Id, "cli", null, "[]", DateTime.UtcNow.AddDays(1));
            plaintextKey = rawKey;
            db.UserApiKeys.Add(apiKey);
            var org = db.Orgs.Single(o => o.Id == Constants.DefaultOrganizationId);
            org.RequireSso = true;
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/api-keys/login",
            new StringContent($"{{\"apiKey\":\"{plaintextKey}\"}}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));
    }

    [Fact]
    public async Task EmailVerificationRequest_WritesGenericAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var response = await app.Client.PostAsync(
            "/api/v1/auth/email-verification/request",
            new StringContent("{\"email\":\"unknown-verify@example.com\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.email_verification.request", TestContext.Current.CancellationToken));
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
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.sso.external_identity.link", TestContext.Current.CancellationToken));
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
    public async Task GetMe_ReturnsCanonicalEnvelopeForSession()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-me-envelope@example.com", "Me Envelope");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("data", out var data));
        Assert.True(json.RootElement.TryGetProperty("meta", out _));
        Assert.Equal("route-me-envelope@example.com", data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task RevokeSession_WritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-session-revoke@example.com", "Session Revoke");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);
        var sessionId = Guid.Empty;
        await app.WithDbAsync(async db =>
        {
            sessionId = await db.UserSessions.Where(x => x.UserId == userId).Select(x => x.Id).SingleAsync(TestContext.Current.CancellationToken);
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/me/sessions/{sessionId}/revoke");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.session.revoke" && x.TargetId == sessionId.ToString(), TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task GetSessions_SupportsCollectionQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-session-query@example.com", "Session Query");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/sessions?limit=1&sort=-createdAt");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.TryGetProperty("pagination", out _));
        Assert.Equal("-createdAt", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("sort")[0].GetString());
    }

    [Fact]
    public async Task DeleteMe_SoftDeletesUserAndRevokesAccess()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-delete-me@example.com", "Delete Me");
            userId = user.Id;
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
            });
            var store = TestUserStore.Create(db);
            var (_, apiKey) = store.GenerateUserApiKey(user.Id, "cli", null, "[]", null);
            db.UserApiKeys.Add(apiKey);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/me/");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = Assert.Single(app.EmailSender.Messages);
        Assert.Equal("route-delete-me@example.com", message.To);
        Assert.Contains("deleted", message.Body, StringComparison.OrdinalIgnoreCase);
        await app.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Id == userId, TestContext.Current.CancellationToken);
            Assert.Equal(UserStatus.Deleted.Id, user.StatusId);
            Assert.NotNull(user.DeletedAt);
            Assert.NotNull(user.HardDeleteAt);
            Assert.True(user.HardDeleteAt > user.DeletedAt);
            Assert.True(await db.UserSessions.AnyAsync(x => x.UserId == userId && x.RevokedAt != null, TestContext.Current.CancellationToken));
            Assert.True(await db.UserApiKeys.AnyAsync(x => x.UserId == userId && x.RevokedAt != null, TestContext.Current.CancellationToken));
            Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == userId && x.DeletedAt != null, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.user.delete", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task DeleteMe_WhenConfigured_AnonymizesUserIdentity()
    {
        await using var app = await RouteTestApp.CreateAsync(configuration: new Dictionary<string, string?>
        {
            ["Auth:Deletion:AnonymizeOnDelete"] = "true",
        });
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-delete-anon@example.com", "Delete Anon");
            userId = user.Id;
            db.UserEmails.Add(new UserEmail
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Email = user.Email,
                EmailUpcase = user.EmailUpcase,
                EmailDigest = TokenGenerator.ComputeDigestBase64(user.Email),
                StatusId = UserEmailStatus.Active.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                VerificationTokenDigest = "pending-token",
                VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/me/");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Id == userId, TestContext.Current.CancellationToken);
            var email = await db.UserEmails.SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
            Assert.Equal(UserStatus.Deleted.Id, user.StatusId);
            Assert.StartsWith("deleted-", user.Email, StringComparison.Ordinal);
            Assert.Equal("Deleted user", user.Name);
            Assert.Equal(user.Email, email.Email);
            Assert.Null(email.VerificationTokenDigest);
        });
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
    public async Task CreateUserApiKey_WritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-audit@example.com", "User Key Audit");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"cli\",\"description\":\"CLI key\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var key = await db.UserApiKeys.SingleAsync(x => x.UserId == userId && x.Name == "cli", TestContext.Current.CancellationToken);
            Assert.NotNull(key.ExpiresAt);
            Assert.InRange(key.ExpiresAt!.Value, DateTime.UtcNow.AddDays(89), DateTime.UtcNow.AddDays(91));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.api_key.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task GetUserApiKeys_SupportsCollectionQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-query@example.com", "User Key Query");
            userId = user.Id;
            var store = TestUserStore.Create(db);
            var (_, alpha) = store.GenerateUserApiKey(user.Id, "alpha-key", null, "[]", DateTime.UtcNow.AddDays(1));
            var (_, beta) = store.GenerateUserApiKey(user.Id, "beta-key", null, "[]", DateTime.UtcNow.AddDays(1));
            db.UserApiKeys.AddRange(alpha, beta);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/api-keys?limit=1&filter[name]=key");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("key", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateUserApiKey_WhenExpiryExceedsPolicy_ReturnsValidationError()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-max@example.com", "User Key Max");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);
        var expiresAt = DateTime.UtcNow.AddDays(400).ToString("O");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"name\":\"cli\",\"scopesJson\":\"[]\",\"expiresAt\":\"{expiresAt}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("expiresAt", out _));
    }

    [Fact]
    public async Task CreateUserApiKey_WithStaleSession_ReturnsStepUpRequired()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-stale@example.com", "User Key Stale");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);
        await app.WithDbAsync(async db =>
        {
            var session = await db.UserSessions.SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
            session.CreatedAt = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"cli\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("step_up_required", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.UserApiKeys.AnyAsync(x => x.UserId == userId, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task RotateUserApiKey_RevokesOldKeyAndReturnsNewPlaintextKey()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var oldKeyId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-key-rotate@example.com", "User Key Rotate");
            userId = user.Id;
            var store = TestUserStore.Create(db);
            var (_, apiKey) = store.GenerateUserApiKey(user.Id, "old", "Old key", "[]", DateTime.UtcNow.AddDays(7));
            oldKeyId = apiKey.Id;
            db.UserApiKeys.Add(apiKey);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/me/api-keys/{oldKeyId}/rotate");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"new\",\"description\":\"New key\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("plaintextKey", body, StringComparison.OrdinalIgnoreCase);
        await app.WithDbAsync(async db =>
        {
            Assert.NotNull((await db.UserApiKeys.SingleAsync(x => x.Id == oldKeyId, TestContext.Current.CancellationToken)).RevokedAt);
            Assert.True(await db.UserApiKeys.AnyAsync(x => x.UserId == userId && x.Name == "new", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.api_key.rotate", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task StartTotp_WritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-totp-audit@example.com", "Totp Audit");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/mfa/totp/start");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Phone\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.UserMfaFactors.AnyAsync(x => x.UserId == userId && x.Name == "Phone", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.mfa.totp.start", TestContext.Current.CancellationToken));
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
    public async Task ConfirmTotp_WithStaleSession_AllowsSensitiveMutation()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var factorId = Guid.CreateVersion7();
        var secret = Enumerable.Range(1, 20).Select(i => (byte)i).ToArray();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-totp-stepup@example.com", "Totp Stepup");
            userId = user.Id;
            db.UserMfaFactors.Add(new UserMfaFactor
            {
                Id = factorId,
                UserId = user.Id,
                Name = "Phone",
                Type = MfaFactorType.Totp.Id,
                ValueEncrypted = secret,
                CreatedAt = DateTime.UtcNow,
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);
        await app.WithDbAsync(async db =>
        {
            var session = await db.UserSessions.SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
            session.CreatedAt = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });
        var code = UserStore.ComputeTotp(secret, DateTimeOffset.UtcNow);

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/mfa/totp/confirm");
        confirmRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        confirmRequest.Content = new StringContent($"{{\"factorId\":\"{factorId}\",\"code\":\"{code}\"}}", Encoding.UTF8, "application/json");
        using var confirmResponse = await app.Client.SendAsync(confirmRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        using var keyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/me/api-keys");
        keyRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        keyRequest.Content = new StringContent("{\"name\":\"cli\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var keyResponse = await app.Client.SendAsync(keyRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, keyResponse.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var session = await db.UserSessions.SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
            Assert.NotNull(session.MfaVerifiedAt);
            Assert.True(await db.UserApiKeys.AnyAsync(x => x.UserId == userId && x.Name == "cli", TestContext.Current.CancellationToken));
        });
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
    public async Task GetPasskeys_SupportsCollectionQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-passkey-query@example.com", "Passkey Query");
            userId = user.Id;
            db.UserMfaFactors.AddRange(
                new UserMfaFactor { Id = Guid.CreateVersion7(), UserId = user.Id, Name = "Alpha Key", Type = MfaFactorType.Passkey.Id, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserMfaFactor { Id = Guid.CreateVersion7(), UserId = user.Id, Name = "Beta Key", Type = MfaFactorType.Passkey.Id, CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/passkeys?limit=1&filter[name]=Key");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("Key", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
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
                SubjectDigest = TokenGenerator.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject")),
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
    public async Task GetExternalIdentities_SupportsCollectionQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-external-query@example.com", "External Query");
            userId = user.Id;
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 11,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Acme OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                ClientId = "client-id",
            });
            db.UserExternalIdentities.AddRange(
                new UserExternalIdentity { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, UserId = user.Id, ProviderId = 11, Subject = "subject-a", SubjectDigest = TokenGenerator.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject-a")), CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserExternalIdentity { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, UserId = user.Id, ProviderId = 11, Subject = "subject-b", SubjectDigest = TokenGenerator.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject-b")), CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me/external-identities?limit=1&sort=createdAt");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("createdAt", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("sort")[0].GetString());
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
                SubjectDigest = TokenGenerator.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject")),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/me/external-identities/{linkId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkExternalIdentity_WhenAnotherSignInMethodExists_WritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var linkId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-unlink@example.com", "Unlink User");
            userId = user.Id;
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = PasswordHashing.Hash("current-password"),
            });
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
                SubjectDigest = TokenGenerator.ComputeDigestBase64(Encoding.UTF8.GetBytes("subject")),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/me/external-identities/{linkId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.UserExternalIdentities.AnyAsync(x => x.Id == linkId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "auth.external_identity.unlink" && x.TargetId == linkId.ToString(), TestContext.Current.CancellationToken));
        });
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
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

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
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

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
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
    public async Task UpdateAuthPolicy_PersistsMfaPolicy()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-policy-mfa@example.com", "Policy MFA");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"mfaPolicy\":\"all_members\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("all_members", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.Equal(OrganizationMfaPolicy.AllMembers.Id, (await db.Orgs.SingleAsync(x => x.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken)).MfaPolicyId);
        });
    }

    [Fact]
    public async Task GetAuthPolicy_CurrentOrgRouteAllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.settings.read");
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("allowPasswordAuth", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAuthPolicy_CurrentOrgRouteWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-policy-writer@example.com", "Current Policy Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/org/{Constants.DefaultOrganizationId}/auth/policy");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"mfaPolicy\":\"all_members\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.Equal(OrganizationMfaPolicy.AllMembers.Id, (await db.Orgs.SingleAsync(x => x.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken)).MfaPolicyId);
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/service-accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("deploy-bot", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListServiceAccounts_CurrentOrgRouteAllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false);
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/service-accounts?filter[name]=deploy");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal("deploy-bot", data[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task ListServiceAccounts_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-query@example.com", "SA Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.read");
            db.ServiceAccounts.AddRange(
                new ServiceAccount { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "alpha-bot", NameUpcase = "ALPHA-BOT", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ServiceAccount { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "beta-bot", NameUpcase = "BETA-BOT", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new ServiceAccount { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "gamma-bot", NameUpcase = "GAMMA-BOT", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/service-accounts?limit=2&sort=-createdAt&filter[name]=bot");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(2, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("bot", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/service-accounts");
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/service-accounts");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/service-accounts");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/service-accounts");
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
    public async Task CreateServiceAccount_CurrentOrgRouteCreatesServiceAccount()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-sa-create@example.com", "Current SA Create");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/service-accounts");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"api-bot\",\"description\":\"API bot\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("api-bot", json.RootElement.GetProperty("data").GetProperty("name").GetString());
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.ServiceAccounts.AnyAsync(x => x.NameUpcase == "API-BOT" && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateServiceAccountApiKey_CurrentOrgRouteReturnsPlaintextKey()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-sa-key@example.com", "Current SA Key");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            db.ServiceAccounts.Add(new ServiceAccount { Id = serviceAccountId, OrgId = Constants.DefaultOrganizationId, Name = "key-bot", NameUpcase = "KEY-BOT", CreatedBy = user.Id, CreatedAt = DateTime.UtcNow });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"primary\",\"description\":null,\"scopesJson\":\"[]\",\"expiresAt\":null}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.StartsWith("nssa_", json.RootElement.GetProperty("data").GetProperty("plaintextKey").GetString(), StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.ServiceAccountApiKeys.AnyAsync(x => x.ServiceAccountId == serviceAccountId && x.Name == "primary", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.create", TestContext.Current.CancellationToken));
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/service-accounts");
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
    public async Task CreateServiceAccountApiKey_AppliesDefaultExpiry()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-default-expiry@example.com", "Service Account Key Default Expiry");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            db.ServiceAccounts.Add(new ServiceAccount
            {
                Id = serviceAccountId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "default-key-bot",
                NameUpcase = "DEFAULT-KEY-BOT",
                Description = "Default key bot",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/api-keys");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"ci\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var key = await db.ServiceAccountApiKeys.SingleAsync(x => x.ServiceAccountId == serviceAccountId && x.Name == "ci", TestContext.Current.CancellationToken);
            Assert.NotNull(key.ExpiresAt);
            Assert.InRange(key.ExpiresAt!.Value, DateTime.UtcNow.AddDays(89), DateTime.UtcNow.AddDays(91));
        });
    }

    [Fact]
    public async Task ListServiceAccountApiKeys_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
            serviceAccountId = seed.ServiceAccountId;
            var user = SeedUser(db, "route-sa-key-query@example.com", "SA Key Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.read");
            var store = new ServiceAccountStore(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)), Microsoft.Extensions.Logging.Abstractions.NullLogger<ServiceAccountStore>.Instance);
            var (_, alpha) = store.GenerateApiKey(serviceAccountId, "alpha-key", null, "[]", DateTime.UtcNow.AddDays(1));
            var (_, beta) = store.GenerateApiKey(serviceAccountId, "beta-key", null, "[]", DateTime.UtcNow.AddDays(1));
            db.ServiceAccountApiKeys.AddRange(alpha, beta);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{serviceAccountId}/api-keys?limit=1&filter[name]=key");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("key", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/api-keys");
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
    public async Task RotateServiceAccountApiKey_RevokesOldKeyAndReturnsNewPlaintextKey()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-rotate@example.com", "Service Account Key Rotate");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/rotate");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"rotated\",\"description\":\"Rotated key\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("plaintextKey", body, StringComparison.OrdinalIgnoreCase);
        await app.WithDbAsync(async db =>
        {
            Assert.NotNull((await db.ServiceAccountApiKeys.SingleAsync(x => x.Id == seed.ApiKeyId, TestContext.Current.CancellationToken)).RevokedAt);
            Assert.True(await db.ServiceAccountApiKeys.AnyAsync(x => x.ServiceAccountId == seed.ServiceAccountId && x.Name == "rotated", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.rotate", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task RotateServiceAccountApiKey_CurrentOrgRouteRevokesOldKeyAndReturnsNewPlaintextKey()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-sa-key-rotate@example.com", "Current Service Account Key Rotate");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/rotate");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"current-rotated\",\"description\":\"Current rotated key\",\"scopesJson\":\"[]\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.StartsWith("nssa_", json.RootElement.GetProperty("data").GetProperty("plaintextKey").GetString(), StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.NotNull((await db.ServiceAccountApiKeys.SingleAsync(x => x.Id == seed.ApiKeyId, TestContext.Current.CancellationToken)).RevokedAt);
            Assert.True(await db.ServiceAccountApiKeys.AnyAsync(x => x.ServiceAccountId == seed.ServiceAccountId && x.Name == "current-rotated", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.service_accounts.api_key.rotate", TestContext.Current.CancellationToken));
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{seed.ServiceAccountId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListServiceAccountClaims_SupportsQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-claim-query@example.com", "SA Claim Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.read");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
            db.ServiceAccountClaims.AddRange(
                new ServiceAccountClaim { Id = Guid.CreateVersion7(), ServiceAccountId = seed.ServiceAccountId, Type = "org.service_accounts.read", Value = "organization:default", CreatedAt = DateTime.UtcNow },
                new ServiceAccountClaim { Id = Guid.CreateVersion7(), ServiceAccountId = seed.ServiceAccountId, Type = "org.service_accounts.write", Value = "organization:default", CreatedAt = DateTime.UtcNow });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{seed.ServiceAccountId}/claims?limit=1&filter[type]=org.service_accounts");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("org.service_accounts", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("type").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/claims");
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
    public async Task AddServiceAccountClaim_CurrentOrgRouteWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var serviceAccountId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-sa-claim-writer@example.com", "Current Service Account Claim Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            var serviceAccount = new ServiceAccount
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "current-claim-bot",
                NameUpcase = "CURRENT-CLAIM-BOT",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            };
            serviceAccountId = serviceAccount.Id;
            db.ServiceAccounts.Add(serviceAccount);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{serviceAccountId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListServiceAccountApiKeyClaims_CurrentOrgRouteAllowsScopedServiceAccountBearer()
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims?filter[type]=org.service_accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seed.PlaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Contains(data.EnumerateArray(), x => x.GetProperty("type").GetString() == "org.service_accounts.read");
    }

    [Fact]
    public async Task ListServiceAccountApiKeyClaims_SupportsQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-sa-key-claim-query@example.com", "SA Key Claim Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.read");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
            db.ServiceAccountApiKeyClaims.AddRange(
                new ServiceAccountApiKeyClaim { ServiceAccountApiKeyId = seed.ApiKeyId, Type = "org.service_accounts.read", Value = "organization:default", CreatedAt = DateTime.UtcNow },
                new ServiceAccountApiKeyClaim { ServiceAccountApiKeyId = seed.ApiKeyId, Type = "org.service_accounts.write", Value = "organization:default", CreatedAt = DateTime.UtcNow });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims?limit=1&sort=-type");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("-type", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("sort")[0].GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
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
    public async Task AddServiceAccountApiKeyClaim_CurrentOrgRouteWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        ServiceAccountBearerSeed seed = default;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-sa-key-claim-writer@example.com", "Current Service Account Key Claim Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.service_accounts.write");
            seed = SeedServiceAccountBearerContext(db, includeReadClaim: false, disabled: false);
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/service-accounts/{seed.ServiceAccountId}/api-keys/{seed.ApiKeyId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/permissions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("org.service_accounts.read", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListPermissions_CurrentOrgRouteAllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.roles.read");
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/permissions?limit=1&filter[resource]=org.service_accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.Equal("org.service_accounts", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("resource").GetString());
    }

    [Fact]
    public async Task ListPermissions_SupportsCollectionQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-permission-query@example.com", "Permission Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/permissions?limit=1&filter[resource]=org.service_accounts");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("org.service_accounts", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("resource").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/permissions");
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/identity-providers/20");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Acme OIDC", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetIdentityProvider_CurrentOrgRouteAllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-idp-owner@example.com", "Current IDP Owner");
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.identity_providers.read");
            db.UserIdentityProviders.Add(new UserIdentityProvider
            {
                Id = 120,
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
                Name = "Current OIDC",
                ProviderTypeId = UserIdentityProviderType.OIDC.Id,
                StatusId = UserIdentityProviderStatus.Active.Id,
                IssuerUrl = "https://idp.example.com",
                ClientId = "client-id",
            });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/identity-providers/120");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Current OIDC", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListIdentityProviders_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-query@example.com", "IDP Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.read");
            db.UserIdentityProviders.AddRange(
                new UserIdentityProvider { Id = 30, OrgId = Constants.DefaultOrganizationId, UserId = user.Id, Name = "Alpha OIDC", ProviderTypeId = UserIdentityProviderType.OIDC.Id, StatusId = UserIdentityProviderStatus.Active.Id, ClientId = "client-id", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new UserIdentityProvider { Id = 31, OrgId = Constants.DefaultOrganizationId, UserId = user.Id, Name = "Beta OIDC", ProviderTypeId = UserIdentityProviderType.OIDC.Id, StatusId = UserIdentityProviderStatus.Active.Id, ClientId = "client-id", CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new UserIdentityProvider { Id = 32, OrgId = Constants.DefaultOrganizationId, UserId = user.Id, Name = "Gamma OAuth", ProviderTypeId = UserIdentityProviderType.OAUTH2.Id, StatusId = UserIdentityProviderStatus.Active.Id, ClientId = "client-id", CreatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/identity-providers?limit=1&sort=name&filter[name]=OIDC");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("OIDC", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
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
    public async Task CreateIdentityProvider_CurrentOrgRouteWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-idp-writer@example.com", "Current IDP Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Current OIDC\",\"providerType\":\"oidc\",\"issuerUrl\":\"https://idp.example.com\",\"clientId\":\"client-id\",\"metadataJson\":\"{}\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Current OIDC", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.UserIdentityProviders.AnyAsync(x => x.Name == "Current OIDC", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.identity_providers.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateIdentityProvider_UserManagementAliasWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-idp-writer@example.com", "User IDP Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"User Alias OIDC\",\"providerType\":\"oidc\",\"issuerUrl\":\"https://idp.example.com\",\"clientId\":\"client-id\",\"metadataJson\":\"{}\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("User Alias OIDC", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.UserIdentityProviders.AnyAsync(x => x.Name == "User Alias OIDC", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.identity_providers.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task IdentityProviderRoutes_AreOnlyMappedUnderUsers()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var topLevelResponse = await app.Client.GetAsync("/api/v1/identity-providers", TestContext.Current.CancellationToken);
        using var orgScopedResponse = await app.Client.GetAsync("/api/v1/orgs/default/identity-providers", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, topLevelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, orgScopedResponse.StatusCode);
    }

    [Fact]
    public async Task CreateIdentityProvider_WithStaleSessionReturnsStepUpRequired()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-stale@example.com", "IDP Stale");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);
        await app.WithDbAsync(async db =>
        {
            var session = await db.UserSessions.SingleAsync(x => x.UserId == userId, TestContext.Current.CancellationToken);
            session.CreatedAt = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Acme OIDC\",\"providerType\":\"oidc\",\"issuerUrl\":\"https://idp.example.com\",\"clientId\":\"client-id\",\"metadataJson\":\"{}\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("step_up_required", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.UserIdentityProviders.AnyAsync(x => x.Name == "Acme OIDC", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateIdentityProvider_WithGooglePresetFillsOidcMetadata()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-idp-google@example.com", "IDP Google");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.identity_providers.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Google\",\"preset\":\"google\",\"clientId\":\"client-id\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("OIDC", body, StringComparison.Ordinal);
        Assert.Contains("https://accounts.google.com", body, StringComparison.Ordinal);
        Assert.Contains("https://oauth2.googleapis.com/token", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            var provider = await db.UserIdentityProviders.SingleAsync(x => x.Name == "Google", TestContext.Current.CancellationToken);
            Assert.Equal(UserIdentityProviderType.OIDC.Id, provider.ProviderTypeId);
            Assert.Equal("https://accounts.google.com", provider.IssuerUrl);
            Assert.Contains("oauth2.googleapis.com/token", provider.MetadataJson, StringComparison.Ordinal);
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/identity-providers");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\" \" ,\"providerType\":\"ldap\",\"preset\":\"unknown\",\"issuerUrl\":\"http://idp.example.com\",\"clientId\":\" \" ,\"metadataJson\":\"not-json\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("name", out _));
        Assert.True(fields.TryGetProperty("providerType", out _));
        Assert.True(fields.TryGetProperty("preset", out _));
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

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("data", out var data));
        Assert.True(json.RootElement.TryGetProperty("meta", out _));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Contains("Reader", body, StringComparison.Ordinal);
        Assert.Contains(data.EnumerateArray(), x => x.GetProperty("key").GetString() == BuiltInRoleStore.OwnerRoleName && x.GetProperty("builtIn").GetBoolean() && !x.GetProperty("editable").GetBoolean());
    }

    [Fact]
    public async Task ListRoles_CurrentOrgRouteUsesSessionOrganization()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-role-list@example.com", "Current Role List");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles?filter[name]=owner");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal(BuiltInRoleStore.OwnerRoleName, data[0].GetProperty("key").GetString());
    }

    [Fact]
    public async Task CreateRole_CurrentOrgRouteCreatesCustomRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-role-create@example.com", "Current Role Create");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Maintainer\",\"description\":\"Maintains things\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Maintainer", json.RootElement.GetProperty("data").GetProperty("name").GetString());
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.Roles.AnyAsync(x => x.NameUpcase == "MAINTAINER" && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.roles.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task GetRole_ReturnsBuiltInRoleDefinition()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-builtin@example.com", "Role Built In");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/roles/{BuiltInRoleStore.OwnerRoleId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(BuiltInRoleStore.OwnerRoleName, data.GetProperty("key").GetString());
        Assert.True(data.GetProperty("builtIn").GetBoolean());
        Assert.False(data.GetProperty("editable").GetBoolean());
        Assert.True(data.GetProperty("claims").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ListRoles_HidesLegacyBuiltInRoleRows()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-legacy-built-in@example.com", "Role Legacy Built In");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
            db.Roles.Add(new Role
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "Owner",
                NameUpcase = "OWNER",
                Description = "Legacy owner row",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles?filter[name]=owner");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal(BuiltInRoleStore.OwnerRoleName, data[0].GetProperty("key").GetString());
        Assert.True(data[0].GetProperty("builtIn").GetBoolean());
    }

    [Fact]
    public async Task ListRoles_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-query@example.com", "Role Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
            db.Roles.AddRange(
                new Role { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Alpha", NameUpcase = "ALPHA", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Beta", NameUpcase = "BETA", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new Role { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Gamma", NameUpcase = "GAMMA", CreatedBy = user.Id, CreatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles?limit=1&sort=name&filter[name]=a");
        firstRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var firstResponse = await app.Client.SendAsync(firstRequest, TestContext.Current.CancellationToken);
        var firstBody = await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        using var firstJson = JsonDocument.Parse(firstBody);
        Assert.Equal(1, firstJson.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(firstJson.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("a", firstJson.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
        var cursor = firstJson.RootElement.GetProperty("pagination").GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/roles?limit=1&sort=name&filter[name]=a&cursor={Uri.EscapeDataString(cursor!)}");
        secondRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var secondResponse = await app.Client.SendAsync(secondRequest, TestContext.Current.CancellationToken);
        var secondBody = await secondResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var secondJson = JsonDocument.Parse(secondBody);
        Assert.Equal(1, secondJson.RootElement.GetProperty("data").GetArrayLength());
        Assert.NotEqual(
            firstJson.RootElement.GetProperty("data")[0].GetProperty("id").GetString(),
            secondJson.RootElement.GetProperty("data")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task ListRoles_RejectsInvalidQueryContractValues()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-query-invalid@example.com", "Role Query Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/roles?sort=bad&unknown=1");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("sort", out _));
        Assert.True(fields.TryGetProperty("unknown", out _));
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles");
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
    public async Task CreateRole_RejectsBuiltInRoleName()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-role-builtin-name@example.com", "Role Built In Name");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/roles");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"owner\",\"description\":\"Reserved\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields").TryGetProperty("name", out _));
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.Roles.AnyAsync(x => x.NameUpcase == "OWNER", TestContext.Current.CancellationToken));
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.roles.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AttachRoleUser_AssignsBuiltInRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var adminId = Guid.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var admin = SeedUser(db, "route-builtin-assign-admin@example.com", "Built In Assign Admin");
            var target = SeedUser(db, "route-builtin-assign-target@example.com", "Built In Assign Target");
            adminId = admin.Id;
            targetId = target.Id;
            GrantUserOrgPermission(db, admin.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(adminId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{targetId}/roles");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"roleId\":\"{BuiltInRoleStore.ReaderRoleId}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.RoleAssignments.AnyAsync(x => x.UserId == targetId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.users.roles.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateRoleAssignment_CurrentOrgRouteAssignsBuiltInRoleToUser()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var adminId = Guid.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var admin = SeedUser(db, "route-current-assignment-admin@example.com", "Current Assignment Admin");
            var target = SeedUser(db, "route-current-assignment-target@example.com", "Current Assignment Target");
            adminId = admin.Id;
            targetId = target.Id;
            GrantUserOrgPermission(db, admin.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(adminId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/role-assignments");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent($"{{\"roleId\":\"{BuiltInRoleStore.ReaderRoleId}\",\"principalType\":\"user\",\"principalId\":\"{targetId}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.RoleAssignments.AnyAsync(x => x.UserId == targetId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.role_assignments.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateRoleAssignment_RejectsServiceAccountBearerEvenWithWriteClaim()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var target = SeedUser(db, "route-current-assignment-service-account-target@example.com", "Current Assignment Service Account Target");
            targetId = target.Id;
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.roles.write");
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/role-assignments");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        request.Content = new StringContent($"{{\"roleId\":\"{BuiltInRoleStore.ReaderRoleId}\",\"principalType\":\"user\",\"principalId\":\"{targetId}\"}}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.RoleAssignments.AnyAsync(x => x.UserId == targetId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task DetachRoleUser_RemovesBuiltInRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var adminId = Guid.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var admin = SeedUser(db, "route-builtin-detach-admin@example.com", "Built In Detach Admin");
            var target = SeedUser(db, "route-builtin-detach-target@example.com", "Built In Detach Target");
            adminId = admin.Id;
            targetId = target.Id;
            GrantUserOrgPermission(db, admin.Id, "org.roles.write");
            db.RoleAssignments.Add(new RoleAssignment
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                UserId = target.Id,
                RoleKey = BuiltInRoleStore.ReaderRoleName,
                ScopeKind = PermissionScopeKind.Organization,
                ScopeId = "default",
                CreatedBy = admin.Id,
                CreatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(adminId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/{targetId}/roles/{BuiltInRoleStore.ReaderRoleId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.RoleAssignments.AnyAsync(x => x.UserId == targetId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.users.roles.remove", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task UpdateRole_RejectsBuiltInRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-builtin-update@example.com", "Built In Update");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/roles/{BuiltInRoleStore.OwnerRoleId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"description\":\"Changed\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("conflict", json.RootElement.GetProperty("error").GetProperty("code").GetString());
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.AuditEvents.AnyAsync(x => x.Type == "org.roles.update", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AttachGroupRole_AssignsBuiltInRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var groupId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-builtin-assign@example.com", "Group Built In Assign");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.write");
            db.Groups.Add(new Group
            {
                Id = groupId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "readers",
                NameUpcase = "READERS",
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/groups/{groupId}/roles/{BuiltInRoleStore.ReaderRoleId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.RoleAssignments.AnyAsync(x => x.GroupId == groupId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.groups.role.add", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task DetachGroupRole_RemovesBuiltInRole()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var groupId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-builtin-detach@example.com", "Group Built In Detach");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.write");
            db.Groups.Add(new Group
            {
                Id = groupId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "readers",
                NameUpcase = "READERS",
            });
            db.RoleAssignments.Add(new RoleAssignment
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                GroupId = groupId,
                RoleKey = BuiltInRoleStore.ReaderRoleName,
                ScopeKind = PermissionScopeKind.Organization,
                ScopeId = "default",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 4, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/groups/{groupId}/roles/{BuiltInRoleStore.ReaderRoleId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            Assert.False(await db.RoleAssignments.AnyAsync(x => x.GroupId == groupId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.groups.role.remove", TestContext.Current.CancellationToken));
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

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/roles/{roleId}/claims");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/groups");
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
    public async Task ListGroups_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-query@example.com", "Group Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.read");
            db.Groups.AddRange(
                new Group { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Alpha", NameUpcase = "ALPHA" },
                new Group { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Beta", NameUpcase = "BETA" },
                new Group { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Gamma", NameUpcase = "GAMMA" });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/groups?limit=1&sort=-name&filter[name]=a");
        firstRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var firstResponse = await app.Client.SendAsync(firstRequest, TestContext.Current.CancellationToken);
        var firstBody = await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        using var firstJson = JsonDocument.Parse(firstBody);
        Assert.Equal(1, firstJson.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(firstJson.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("-name", firstJson.RootElement.GetProperty("meta").GetProperty("query").GetProperty("sort")[0].GetString());
        var cursor = firstJson.RootElement.GetProperty("pagination").GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/groups?limit=1&sort=-name&filter[name]=a&cursor={Uri.EscapeDataString(cursor!)}");
        secondRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var secondResponse = await app.Client.SendAsync(secondRequest, TestContext.Current.CancellationToken);
        var secondBody = await secondResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var secondJson = JsonDocument.Parse(secondBody);
        Assert.Equal(1, secondJson.RootElement.GetProperty("data").GetArrayLength());
        Assert.NotEqual(
            firstJson.RootElement.GetProperty("data")[0].GetProperty("id").GetString(),
            secondJson.RootElement.GetProperty("data")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetGroup_IncludesBuiltInRoleAssignmentsInRoleCount()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var groupId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-role-count@example.com", "Group Role Count");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.read");
            var group = new Group
            {
                Id = groupId,
                OrgId = Constants.DefaultOrganizationId,
                Name = "readers",
                NameUpcase = "READERS",
            };
            var legacyRole = new Role
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                Name = "Owner",
                NameUpcase = "OWNER",
                CreatedBy = user.Id,
            };

            db.Groups.Add(group);
            db.Roles.Add(legacyRole);
            group.Roles.Add(legacyRole);
            db.RoleAssignments.Add(new RoleAssignment
            {
                Id = Guid.CreateVersion7(),
                OrgId = Constants.DefaultOrganizationId,
                GroupId = groupId,
                RoleKey = BuiltInRoleStore.ReaderRoleName,
                ScopeKind = PermissionScopeKind.Organization,
                ScopeId = "default",
                CreatedBy = user.Id,
                CreatedAt = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/groups/{groupId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetProperty("roleCount").GetInt32());
    }

    [Fact]
    public async Task ListGroups_RejectsInvalidQueryContractValues()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-group-query-invalid@example.com", "Group Query Invalid");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.read");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/groups?limit=0&cursor=bad%%");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var fields = json.RootElement.GetProperty("error").GetProperty("details").GetProperty("fields");
        Assert.True(fields.TryGetProperty("limit", out _));
        Assert.True(fields.TryGetProperty("cursor", out _));
    }

    [Fact]
    public async Task ListGroups_CurrentOrgRouteAllowsScopedServiceAccountBearer()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var plaintextKey = string.Empty;
        await app.SeedAsync(db =>
        {
            plaintextKey = SeedServiceAccountBearer(db, includeReadClaim: true, disabled: false, claimType: "org.groups.read");
            db.Groups.Add(new Group { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Name = "Ops", NameUpcase = "OPS" });
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/groups?filter[name]=op");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintextKey);
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal("Ops", data[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateGroup_CurrentOrgRouteCreatesGroup()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-group-create@example.com", "Current Group Create");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/groups");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Operators\",\"email\":\"operators@example.com\",\"description\":\"Runs systems\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("Operators", json.RootElement.GetProperty("data").GetProperty("name").GetString());
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.Groups.AnyAsync(x => x.NameUpcase == "OPERATORS" && x.OrgId == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.groups.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task AddGroupMember_CurrentOrgRouteAddsUserMember()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var adminId = Guid.Empty;
        var targetId = Guid.Empty;
        var groupId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var admin = SeedUser(db, "route-current-group-admin@example.com", "Current Group Admin");
            var target = SeedUser(db, "route-current-group-target@example.com", "Current Group Target");
            adminId = admin.Id;
            targetId = target.Id;
            GrantUserOrgPermission(db, admin.Id, "org.groups.write");
            db.Groups.Add(new Group { Id = groupId, OrgId = Constants.DefaultOrganizationId, Name = "Workers", NameUpcase = "WORKERS" });
        });
        var sessionToken = await app.CreateSessionAsync(adminId);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/groups/{groupId}/members/user/{targetId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var group = await db.Groups.Include(x => x.Members).SingleAsync(x => x.Id == groupId, TestContext.Current.CancellationToken);
            Assert.Contains(group.Members, x => x.Id == targetId);
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.groups.member.add", TestContext.Current.CancellationToken));
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/invites");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"email\":\"invited@example.com\",\"roleIds\":[],\"groupIds\":[]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("invited@example.com", body, StringComparison.Ordinal);
        Assert.Contains("token", body, StringComparison.OrdinalIgnoreCase);
        var message = Assert.Single(app.EmailSender.Messages);
        Assert.Equal("invited@example.com", message.To);
        Assert.Contains("accept-invite?token=", message.Body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.OrganizationInvites.AnyAsync(x => x.Email == "invited@example.com", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateInvite_CurrentOrgRouteReturnsTokenAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-invite-writer@example.com", "Current Invite Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/invites");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"email\":\"current-invited@example.com\",\"roleIds\":[],\"groupIds\":[]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("current-invited@example.com", body, StringComparison.Ordinal);
        Assert.Contains("token", body, StringComparison.OrdinalIgnoreCase);
        var message = Assert.Single(app.EmailSender.Messages);
        Assert.Equal("current-invited@example.com", message.To);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.OrganizationInvites.AnyAsync(x => x.Email == "current-invited@example.com", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task CreateInvite_UserManagementAliasReturnsTokenAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-invite-writer@example.com", "User Invite Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/invites");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"email\":\"user-alias-invited@example.com\",\"roleIds\":[],\"groupIds\":[]}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("user-alias-invited@example.com", body, StringComparison.Ordinal);
        Assert.Contains("token", body, StringComparison.OrdinalIgnoreCase);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.OrganizationInvites.AnyAsync(x => x.Email == "user-alias-invited@example.com", TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.invites.create", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task InviteRoutes_AreOnlyMappedUnderUsers()
    {
        await using var app = await RouteTestApp.CreateAsync();

        using var topLevelResponse = await app.Client.GetAsync("/api/v1/invites", TestContext.Current.CancellationToken);
        using var orgScopedResponse = await app.Client.GetAsync("/api/v1/orgs/default/invites", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, topLevelResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, orgScopedResponse.StatusCode);
    }

    [Fact]
    public async Task ListInvites_SupportsQueryContractPaginationAndFilter()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-invite-query@example.com", "Invite Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.read");
            db.OrganizationInvites.AddRange(
                new OrganizationInvite { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Email = "alpha@example.com", EmailUpcase = "ALPHA@EXAMPLE.COM", TokenDigest = "a", InvitedByUserId = user.Id, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresAt = DateTime.UtcNow.AddDays(7) },
                new OrganizationInvite { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Email = "beta@example.com", EmailUpcase = "BETA@EXAMPLE.COM", TokenDigest = "b", InvitedByUserId = user.Id, CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc), ExpiresAt = DateTime.UtcNow.AddDays(7) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/invites?limit=1&filter[email]=example&sort=createdAt");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("example", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("email").GetString());
    }

    [Fact]
    public async Task ListInvites_CurrentOrgRouteSupportsQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-invite-query@example.com", "Current Invite Query");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.read");
            db.OrganizationInvites.AddRange(
                new OrganizationInvite { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Email = "alpha-current@example.com", EmailUpcase = "ALPHA-CURRENT@EXAMPLE.COM", TokenDigest = "ca", InvitedByUserId = user.Id, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresAt = DateTime.UtcNow.AddDays(7) },
                new OrganizationInvite { Id = Guid.CreateVersion7(), OrgId = Constants.DefaultOrganizationId, Email = "beta-current@example.com", EmailUpcase = "BETA-CURRENT@EXAMPLE.COM", TokenDigest = "cb", InvitedByUserId = user.Id, CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc), ExpiresAt = DateTime.UtcNow.AddDays(7) });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/invites?limit=1&filter[email]=current&sort=createdAt");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("pagination").GetProperty("hasMore").GetBoolean());
        Assert.Equal("current", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("email").GetString());
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/invites");
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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/invites");
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
                TokenDigest = TokenGenerator.ComputeDigestBase64(rawToken),
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

    [Fact]
    public async Task DeleteOrganization_RequiresConfirmationAndWritesAuditEvent()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-org-delete@example.com", "Org Delete");
            userId = user.Id;
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = Constants.DefaultOrganizationId,
                UserId = user.Id,
            });
            GrantUserOrgPermission(db, user.Id, "org.settings.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var badRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/org");
        badRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        badRequest.Content = new StringContent("{\"confirmation\":\"wrong\"}", Encoding.UTF8, "application/json");
        using var badResponse = await app.Client.SendAsync(badRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badResponse.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/org");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"confirmation\":\"default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var org = await db.Orgs.SingleAsync(x => x.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken);
            Assert.Equal(OrganizationStatus.PendingDeleted.Id, org.StatusId);
            Assert.NotNull(org.DeletedAt);
            Assert.NotNull(org.HardDeleteAt);
            Assert.True(org.HardDeleteAt > org.DeletedAt);
            Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == userId && x.DeletedAt != null, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.delete", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task GetCurrentOrganization_ReturnsSessionOrganization()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-org-reader@example.com", "Current Org Reader");
            userId = user.Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/org");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("default", json.RootElement.GetProperty("data").GetProperty("slug").GetString());
    }

    [Fact]
    public async Task UpdateCurrentOrganization_UpdatesSessionOrganization()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-current-org-writer@example.com", "Current Org Writer");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.settings.write");
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/org");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        request.Content = new StringContent("{\"name\":\"Default Renamed\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Default Renamed", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.Equal("Default Renamed", (await db.Orgs.SingleAsync(x => x.Id == Constants.DefaultOrganizationId, TestContext.Current.CancellationToken)).Name);
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.update", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListOrganizations_SupportsQueryContract()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var otherOrgId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-org-query@example.com", "Org Query");
            userId = user.Id;
            db.Orgs.Add(new Organization
            {
                Id = otherOrgId,
                Name = "Alpha Org",
                NameUpcase = "ALPHA ORG",
                Slug = "alpha-org",
                StatusId = OrganizationStatus.Active.Id,
                TenantModeId = TenantMode.Multi.Id,
                OrganizationPlanId = 1,
                CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });
            db.OrganizationMemberships.Add(new OrganizationMembership { OrgId = otherOrgId, UserId = user.Id, CreatedAt = DateTime.UtcNow, AcceptedAt = DateTime.UtcNow });
            db.OrganizationMemberships.Add(new OrganizationMembership { OrgId = Constants.DefaultOrganizationId, UserId = user.Id, CreatedAt = DateTime.UtcNow, AcceptedAt = DateTime.UtcNow });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/orgs?limit=1&filter[name]=Org");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.True(json.RootElement.TryGetProperty("pagination", out _));
        Assert.Equal("Org", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("name").GetString());
    }

    [Fact]
    public async Task BootstrapAdmin_RequiresRootToken()
    {
        await using var app = await RouteTestApp.CreateAsync(configuration: new Dictionary<string, string?>
        {
            ["NEO_ROOT_TOKEN"] = "test-root-token",
        });

        using var response = await app.Client.PostAsync(
            "/api/v1/admin/bootstrap-admin",
            new StringContent("{\"email\":\"root@example.com\",\"password\":\"correct-horse-password\",\"org\":\"default\"}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("permission_denied", json.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task BootstrapAdmin_WithRootTokenCreatesOwnerThroughApi()
    {
        await using var app = await RouteTestApp.CreateAsync(configuration: new Dictionary<string, string?>
        {
            ["NEO_ROOT_TOKEN"] = "test-root-token",
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/bootstrap-admin");
        request.Headers.Add("X-NeoShip-Root-Token", "test-root-token");
        request.Content = new StringContent("{\"email\":\"root@example.com\",\"password\":\"correct-horse-password\",\"name\":\"Root User\",\"org\":\"default\",\"orgName\":\"Default\"}", Encoding.UTF8, "application/json");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal("root@example.com", json.RootElement.GetProperty("data").GetProperty("email").GetString());
        await app.WithDbAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Email == "root@example.com", TestContext.Current.CancellationToken);
            Assert.Equal("Root User", user.Name);
            Assert.True(PasswordHashing.Verify("correct-horse-password", (await db.UserPasswordAuths.SingleAsync(x => x.UserId == user.Id, TestContext.Current.CancellationToken)).PasswordHash).Success);
            Assert.True(await db.OrganizationMemberships.AnyAsync(x => x.UserId == user.Id && x.OrgId == Constants.DefaultOrganizationId && x.DeletedAt == null, TestContext.Current.CancellationToken));
            Assert.True(await db.RoleAssignments.AnyAsync(x => x.OrgId == Constants.DefaultOrganizationId && x.UserId == user.Id && x.RoleKey == BuiltInRoleStore.OwnerRoleName, TestContext.Current.CancellationToken));
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "admin.bootstrap", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task ListUsers_ReturnsCurrentOrgUsersWithEnvelope()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-users-reader@example.com", "Users Reader");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.read");
            SeedUser(db, "route-users-target@example.com", "Users Target");
            SeedUser(db, "route-users-deleted@example.com", "Users Deleted").DeletedAt = DateTime.UtcNow;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users?filter[email]=target");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(body);
        Assert.Equal(1, json.RootElement.GetProperty("data").GetArrayLength());
        Assert.Equal("route-users-target@example.com", json.RootElement.GetProperty("data")[0].GetProperty("email").GetString());
        Assert.True(json.RootElement.TryGetProperty("pagination", out _));
        Assert.Equal("target", json.RootElement.GetProperty("meta").GetProperty("query").GetProperty("filter").GetProperty("email").GetString());
    }

    [Fact]
    public async Task DeleteUser_SoftDeletesCurrentOrgUser()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-users-delete-admin@example.com", "Users Delete Admin");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.members.write");
            targetId = SeedUser(db, "route-users-delete-target@example.com", "Users Delete Target").Id;
            db.OrganizationMemberships.Add(new OrganizationMembership { OrgId = Constants.DefaultOrganizationId, UserId = targetId, CreatedAt = DateTime.UtcNow, AcceptedAt = DateTime.UtcNow });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/users/{targetId}");
        request.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var response = await app.Client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.WithDbAsync(async db =>
        {
            var target = await db.Users.SingleAsync(x => x.Id == targetId, TestContext.Current.CancellationToken);
            Assert.Equal(UserStatus.Deleted.Id, target.StatusId);
            Assert.NotNull(target.DeletedAt);
            Assert.True(await db.AuditEvents.AnyAsync(x => x.Type == "org.users.delete", TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task UserRoleRoutes_AssignBuiltInRoleToCurrentOrgUser()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var targetId = Guid.Empty;
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-role-admin@example.com", "User Role Admin");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.roles.write");
            GrantUserOrgPermission(db, user.Id, "org.roles.read");
            targetId = SeedUser(db, "route-user-role-target@example.com", "User Role Target").Id;
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{targetId}/roles");
        addRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        addRequest.Content = new StringContent($"{{\"roleId\":\"{BuiltInRoleStore.ReaderRoleId}\"}}", Encoding.UTF8, "application/json");
        using var addResponse = await app.Client.SendAsync(addRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{targetId}/roles");
        listRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var listResponse = await app.Client.SendAsync(listRequest, TestContext.Current.CancellationToken);
        var body = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains("reader", body, StringComparison.Ordinal);
        await app.WithDbAsync(async db =>
        {
            Assert.True(await db.RoleAssignments.AnyAsync(x => x.UserId == targetId && x.RoleKey == BuiltInRoleStore.ReaderRoleName, TestContext.Current.CancellationToken));
        });
    }

    [Fact]
    public async Task UserGroupRoutes_AddCurrentOrgUserToGroup()
    {
        await using var app = await RouteTestApp.CreateAsync();
        var userId = Guid.Empty;
        var targetId = Guid.Empty;
        var groupId = Guid.CreateVersion7();
        await app.SeedAsync(db =>
        {
            var user = SeedUser(db, "route-user-group-admin@example.com", "User Group Admin");
            userId = user.Id;
            GrantUserOrgPermission(db, user.Id, "org.groups.write");
            GrantUserOrgPermission(db, user.Id, "org.groups.read");
            targetId = SeedUser(db, "route-user-group-target@example.com", "User Group Target").Id;
            db.Groups.Add(new Group { Id = groupId, OrgId = Constants.DefaultOrganizationId, Name = "Deployers", NameUpcase = "DEPLOYERS" });
        });
        var sessionToken = await app.CreateSessionAsync(userId);

        using var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/users/{targetId}/groups");
        addRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        addRequest.Content = new StringContent($"{{\"groupId\":\"{groupId}\"}}", Encoding.UTF8, "application/json");
        using var addResponse = await app.Client.SendAsync(addRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{targetId}/groups");
        listRequest.Headers.Add("Cookie", $"{AuthEndpoints.SessionCookieName}={sessionToken}");
        using var listResponse = await app.Client.SendAsync(listRequest, TestContext.Current.CancellationToken);
        var body = await listResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains("Deployers", body, StringComparison.Ordinal);
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

        public TestEmailSender EmailSender => this.host.Services.GetRequiredService<TestEmailSender>();

        public static async Task<RouteTestApp> CreateAsync(SsoExternalIdentity? externalIdentity = null, Dictionary<string, string?>? configuration = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var fakeTokenValidator = new FakeSsoTokenValidator(externalIdentity);
            var host = await new HostBuilder()
                .ConfigureAppConfiguration(builder =>
                {
                    if (configuration is not null)
                    {
                        builder.AddInMemoryCollection(configuration);
                    }
                })
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddProblemDetails();
                        services.AddOpenApi();
                        services.AddLogging();
                        services.AddMemoryCache();
                        services.AddDistributedMemoryCache();
                        services.AddRateLimiter(options =>
                        {
                            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                            options.AddPolicy(AuthEndpoints.LoginRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 5,
                                    Window = TimeSpan.FromMinutes(1),
                                    QueueLimit = 0,
                                    AutoReplenishment = true,
                                }));
                            options.AddPolicy(AuthEndpoints.SensitiveRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 10,
                                    Window = TimeSpan.FromMinutes(1),
                                    QueueLimit = 0,
                                    AutoReplenishment = true,
                                }));
                        });
                        services.AddSingleton(connection);
                        services.AddDbContext<ShipDb>((sp, options) => options
                            .UseSqlite(sp.GetRequiredService<SqliteConnection>())
                            .UseSnakeCaseNamingConvention());
                        services.AddScoped<RequestContext>();
                        services.AddSingleton(new PermissionRegistry(CorePermissions.All));
                        services.AddSingleton<PermissionClaimCodec>(sp => new PermissionClaimCodec(sp.GetRequiredService<PermissionRegistry>()));
                        services.AddSingleton<PermissionSnapshotCodec>();
                        services.AddScoped<PermissionResolver>();
                        services.AddSingleton<TokenExchangeStore>();
                        services.AddSingleton<TestEmailSender>();
                        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<TestEmailSender>());
                        services.AddScoped<SessionStore>();
                        services.AddScoped<AuditStore>();
                        services.AddScoped<UserStore>();
                        services.AddScoped<OrganizationStore>();
                        services.AddScoped<RoleStore>();
                        services.AddScoped<GroupStore>();
                        services.AddScoped<ServiceAccountStore>();
                        services.AddSingleton<IdentityProviderSecretProtector>();
                        services.AddSingleton(new Fido2(new Fido2Configuration
                        {
                            ServerDomain = "localhost",
                            ServerName = "NeoShip",
                            Origins = new HashSet<string> { "https://localhost", "http://localhost" },
                        }, metadataService: null));
                        services.AddSingleton<ISsoTokenClient, FakeSsoTokenClient>();
                        services.AddSingleton<ISsoTokenValidator>(fakeTokenValidator);
                        services.AddSingleton<ISsoOAuth2ProfileClient, FakeSsoOAuth2ProfileClient>();
                        services.AddScoped<SsoStore>();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRateLimiter();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapOpenApi();
                            endpoints.MapAuthEndpoints();
                            endpoints.MapAdminEndpoints();
                            endpoints.MapMeEndpoints();
                            endpoints.MapUserEndpoints();
                            endpoints.MapOrgEndpoints();
                            endpoints.MapRoleEndpoints();
                            endpoints.MapGroupEndpoints();
                            endpoints.MapServiceAccountEndpoints();
                            endpoints.MapPermissionEndpoints();
                            endpoints.MapAuthPolicyEndpoints();
                            endpoints.MapCurrentOrganizationEndpoints();
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
            var sso = scope.ServiceProvider.GetRequiredService<SsoStore>();
            return sso.CreateChallenge(Constants.DefaultOrganizationId, providerId, "https://localhost/api/v1/auth/sso/callback", "nonce").State;
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

    private sealed class FakeSsoOAuth2ProfileClient : ISsoOAuth2ProfileClient
    {
        public Task<SsoOAuth2Profile?> FetchAsync(UserIdentityProvider provider, string accessToken, CancellationToken ct = default)
            => Task.FromResult<SsoOAuth2Profile?>(new SsoOAuth2Profile(accessToken, $"{accessToken}@example.com", true, accessToken));
    }
}