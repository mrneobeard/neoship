using System.Diagnostics;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public enum SignupResult
{
    Success,
    EmailAlreadyExists,
}

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked,
    AccountSuspended,
    AuthMethodNotAllowed,
}

public class AuthStore
{
    private readonly ShipDb db;
    private readonly PasswordStore passwords;
    private readonly SessionStore sessions;
    private readonly AuditStore audit;
    private readonly ApiKeyStore apiKeys;
    private readonly PermissionResolver permissions;

    private readonly RequestContext requestContext;
    private readonly ILogger<AuthStore> logger;
    private static readonly ActivitySource activitySource = new(OTelConstants.ActivitySourceName);

    /// <summary>
    /// Initializes a new <see cref="AuthStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="passwords">The password helper.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="audit">The audit store.</param>
    /// <param name="apiKeys">The API key store.</param>
    /// <param name="apiKeys">The API key store.</param>
    /// <param name="permissions">The permission resolver.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="logger">The logger.</param>
    public AuthStore(ShipDb db, PasswordStore passwords, SessionStore sessions, AuditStore audit, ApiKeyStore apiKeys, PermissionResolver permissions, RequestContext requestContext, ILogger<AuthStore> logger)
    {
        this.db = db;
        this.passwords = passwords;
        this.sessions = sessions;
        this.audit = audit;
        this.apiKeys = apiKeys;
        this.permissions = permissions;
        this.requestContext = requestContext;
        this.logger = logger;
    }

    public async Task<(SignupResult Result, User? User, UserSession? Session, string? RawToken)> SignupAsync(
        string email,
        string name,
        string password,
        Guid orgId,
        CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("auth.signup", ActivityKind.Internal);
        activity?.SetTag(OTelConstants.AuthAction, "signup");

        var emailUpcase = email.ToUpperInvariant();

        var exists = await this.db.Users.AnyAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (exists)
        {
            this.logger.LogWarning("Signup attempt with existing email: {Email}", email);
            activity?.SetTag(OTelConstants.AuthResult, "email_exists");
            await this.audit.RecordAsync("auth.signup.failed", orgId, null, "signup",
                dataJson: "{\"reason\":\"email_exists\"}", ct: ct);
            return (SignupResult.EmailAlreadyExists, null, null, null);
        }

        var userId = Factory.NewGuid();
        var user = new User(userId, email, name)
        {
            OrgId = orgId,
            StatusId = UserStatus.Active.Id,
        };

        var emailDigest = TokenStore.ComputeDigestBase64(email);

        var userEmail = new UserEmail
        {
            Id = Factory.NewGuid(),
            UserId = userId,
            Email = email,
            EmailUpcase = emailUpcase,
            EmailDigest = emailDigest,
            StatusId = UserEmailStatus.Active.Id,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            VerifiedAt = DateTime.UtcNow,
        };

        var passwordHash = this.passwords.Hash(password);
        var userPasswordAuth = new UserPasswordAuth
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
        };

        this.db.Users.Add(user);
        this.db.OrganizationMemberships.Add(new OrganizationMembership
        {
            OrgId = orgId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            AcceptedAt = DateTime.UtcNow,
        });
        this.db.UserEmails.Add(userEmail);
        this.db.UserPasswordAuths.Add(userPasswordAuth);
        await this.db.SaveChangesAsync(ct);

        var permissions = await this.permissions.ResolveUserAsync(userId, ct);
        var (session, rawToken) = await this.sessions.CreateSessionAsync(userId, orgId, permissions, ct);

        this.requestContext.UserId = userId;
        this.requestContext.OrgId = orgId;
        this.requestContext.SessionId = session.Id;

        this.logger.LogInformation("User signed up: {UserId} {Email}", userId, email);
        activity?.SetTag(OTelConstants.AuthResult, "success");
        activity?.SetTag(OTelConstants.UserId, userId.ToString());
        await this.audit.RecordAsync("auth.signup.success", orgId, userId, "signup", ct: ct);

        return (SignupResult.Success, user, session, rawToken);
    }

    public async Task<(LoginResult Result, User? User, UserSession? Session, string? RawToken)> LoginAsync(
        string email,
        string password,
        Guid orgId,
        CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("auth.login", ActivityKind.Internal);
        activity?.SetTag(OTelConstants.AuthAction, "login");

        var emailUpcase = email.ToUpperInvariant();

        var user = await this.db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);

        if (user is null)
        {
            this.logger.LogWarning("Login attempt for unknown email: {Email}", email);
            activity?.SetTag(OTelConstants.AuthResult, "user_not_found");
            await this.audit.RecordAsync("auth.login.failed", orgId, null, "login",
                dataJson: "{\"reason\":\"user_not_found\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (user.StatusId == UserStatus.Suspended.Id)
        {
            this.logger.LogWarning("Login attempt for suspended user: {UserId}", user.Id);
            activity?.SetTag(OTelConstants.AuthResult, "account_suspended");
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login",
                dataJson: "{\"reason\":\"account_suspended\"}", ct: ct);
            return (LoginResult.AccountSuspended, null, null, null);
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(o => o.Id == user.OrgId, ct);
        if (org is null || org.RequireSso || !org.AllowPasswordAuth)
        {
            this.logger.LogWarning("Password login blocked by organization policy: {UserId}", user.Id);
            activity?.SetTag(OTelConstants.AuthResult, "password_auth_policy_denied");
            await this.audit.RecordAsync("auth.login.failed", user.OrgId, user.Id, "login",
                dataJson: "{\"reason\":\"password_auth_policy_denied\"}", ct: ct);
            return (LoginResult.AuthMethodNotAllowed, null, null, null);
        }

        var auth = await this.db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);

        if (auth is null)
        {
            this.logger.LogWarning("Login attempt for user without password auth: {UserId}", user.Id);
            activity?.SetTag(OTelConstants.AuthResult, "no_password_auth");
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login",
                dataJson: "{\"reason\":\"no_password_auth\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (auth.LockedUntil.HasValue && auth.LockedUntil > DateTime.UtcNow)
        {
            this.logger.LogWarning("Login attempt for locked account: {UserId}", user.Id);
            activity?.SetTag(OTelConstants.AuthResult, "account_locked");
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login",
                dataJson: "{\"reason\":\"account_locked\"}", ct: ct);
            return (LoginResult.AccountLocked, null, null, null);
        }

        var (success, needsRehash) = this.passwords.Verify(password, auth.PasswordHash);

        if (!success)
        {
            auth.FailedAttempts += 1;
            if (auth.FailedAttempts >= 5)
            {
                auth.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            }

            await this.db.SaveChangesAsync(ct);

            this.logger.LogWarning("Failed login for user {UserId} (attempt {Attempt})", user.Id, auth.FailedAttempts);
            activity?.SetTag(OTelConstants.AuthResult, "invalid_password");
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login",
                dataJson: "{\"reason\":\"invalid_password\"}", ct: ct);

            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (needsRehash)
        {
            auth.PasswordHash = this.passwords.Hash(password);
        }

        auth.FailedAttempts = 0;
        auth.LockedUntil = null;

        user.LastLoginIp = this.requestContext.IpAddress;
        user.LastLoginAt = DateTime.UtcNow;

        var permissions = await this.permissions.ResolveUserAsync(user.Id, ct);
        var (session, rawToken) = await this.sessions.CreateSessionAsync(user.Id, orgId, permissions, ct);

        this.requestContext.UserId = user.Id;
        this.requestContext.OrgId = orgId;
        this.requestContext.SessionId = session.Id;

        this.logger.LogInformation("User logged in: {UserId} {Email}", user.Id, email);
        activity?.SetTag(OTelConstants.AuthResult, "success");
        activity?.SetTag(OTelConstants.UserId, user.Id.ToString());
        await this.audit.RecordAsync("auth.login.success", orgId, user.Id, "login", ct: ct);

        return (LoginResult.Success, user, session, rawToken);
    }

    /// <summary>
    /// Logs in a user with a plaintext API key and creates a session.
    /// </summary>
    /// <param name="rawKey">The plaintext API key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The login result, authenticated user, session, and session token.</returns>
    public async Task<(LoginResult Result, User? User, UserSession? Session, string? RawToken)> LoginWithUserApiKeyAsync(
        string rawKey,
        CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("auth.api_key_login", ActivityKind.Internal);
        activity?.SetTag(OTelConstants.AuthAction, "api_key_login");

        var apiKey = await this.apiKeys.AuthenticateUserApiKeyAsync(rawKey, ct);
        if (apiKey is null || apiKey.User is null)
        {
            activity?.SetTag(OTelConstants.AuthResult, "invalid_api_key");
            await this.audit.RecordAsync("auth.api_key_login.failed", null, null, "api_key_login",
                dataJson: "{\"reason\":\"invalid_api_key\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        var user = apiKey.User;

        if (user.StatusId == UserStatus.Suspended.Id)
        {
            activity?.SetTag(OTelConstants.AuthResult, "account_suspended");
            await this.audit.RecordAsync("auth.api_key_login.failed", user.OrgId, user.Id, "api_key_login",
                dataJson: "{\"reason\":\"account_suspended\"}", ct: ct);
            return (LoginResult.AccountSuspended, null, null, null);
        }

        var permissions = await this.permissions.ResolveUserAsync(user.Id, ct);
        var (session, rawToken) = await this.sessions.CreateSessionAsync(user.Id, user.OrgId, permissions, ct);

        this.requestContext.UserId = user.Id;
        this.requestContext.OrgId = user.OrgId;
        this.requestContext.SessionId = session.Id;

        this.logger.LogInformation("User API key login: {UserId} key={ApiKeyId}", user.Id, apiKey.Id);
        activity?.SetTag(OTelConstants.AuthResult, "success");
        activity?.SetTag(OTelConstants.UserId, user.Id.ToString());
        await this.audit.RecordAsync("auth.api_key_login.success", user.OrgId, user.Id, "api_key_login", ct: ct);

        return (LoginResult.Success, user, session, rawToken);
    }

    public async Task LogoutAsync(string tokenDigest, CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("auth.logout", ActivityKind.Internal);

        var session = await this.db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == tokenDigest && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = "logout";
            await this.db.SaveChangesAsync(ct);

            this.logger.LogInformation("User logged out: {UserId}", session.UserId);
            await this.audit.RecordAsync("auth.logout", session.OrgId, session.UserId, "logout", ct: ct);
        }
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var user = await this.db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (user is null)
        {
            await this.audit.RecordAsync("auth.password_reset.request", null, null, "password_reset.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
            return;
        }

        var auth = await this.db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);
        if (auth is null)
        {
            await this.audit.RecordAsync("auth.password_reset.request", user.OrgId, user.Id, "password_reset.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var digest = TokenStore.ComputeDigestBase64(rawToken);

        auth.ResetTokenDigest = digest;
        auth.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Password reset requested for user {UserId}", user.Id);
        await this.audit.RecordAsync("auth.password_reset.request", user.OrgId, user.Id, "password_reset.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("auth.password_reset_confirm", ActivityKind.Internal);

        var digest = TokenStore.ComputeDigestBase64(token);

        var auth = await this.db.UserPasswordAuths
            .FirstOrDefaultAsync(a => a.ResetTokenDigest == digest
                && a.ResetTokenExpiresAt > DateTime.UtcNow, ct);

        if (auth is null) return false;

        auth.PasswordHash = this.passwords.Hash(newPassword);
        auth.ResetTokenDigest = null;
        auth.ResetTokenExpiresAt = null;
        auth.FailedAttempts = 0;
        auth.LockedUntil = null;
        auth.PasswordChangedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Password reset confirmed for user {UserId}", auth.UserId);
        activity?.SetTag(OTelConstants.AuthResult, "success");
        await this.audit.RecordAsync("auth.password_reset", null, auth.UserId, "password_reset", ct: ct);
        return true;
    }

    public async Task RequestEmailVerificationAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var userEmail = await this.db.UserEmails
            .FirstOrDefaultAsync(e => e.EmailUpcase == emailUpcase && e.StatusId == UserEmailStatus.Pending.Id, ct);

        if (userEmail is null)
        {
            await this.audit.RecordAsync("auth.email_verification.request", null, null, "email_verification.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var digest = TokenStore.ComputeDigestBase64(rawToken);

        userEmail.VerificationTokenDigest = digest;
        userEmail.VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await this.db.SaveChangesAsync(ct);
        await this.audit.RecordAsync("auth.email_verification.request", null, userEmail.UserId, "email_verification.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
    }

    public async Task<bool> ConfirmEmailVerificationAsync(string token, CancellationToken ct = default)
    {
        var digest = TokenStore.ComputeDigestBase64(token);

        var userEmail = await this.db.UserEmails
            .FirstOrDefaultAsync(e => e.VerificationTokenDigest == digest
                && e.VerificationTokenExpiresAt > DateTime.UtcNow, ct);

        if (userEmail is null)
        {
            return false;
        }

        userEmail.VerificationTokenDigest = null;
        userEmail.VerificationTokenExpiresAt = null;
        userEmail.VerifiedAt = DateTime.UtcNow;
        userEmail.StatusId = UserEmailStatus.Active.Id;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Email verified for user {UserId}", userEmail.UserId);
        await this.audit.RecordAsync("auth.email_verified", null, userEmail.UserId, "email_verified", ct: ct);

        return true;
    }
}
