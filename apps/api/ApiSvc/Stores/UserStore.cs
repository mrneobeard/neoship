using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Fido2NetLib;
using Fido2NetLib.Objects;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public enum SignupResult
{
    Success,
    EmailAlreadyExists,
    AuthMethodNotAllowed,
}

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked,
    AccountSuspended,
    AuthMethodNotAllowed,
    MfaRequired,
}

/// <summary>
/// Stores user-owned authentication, API key, MFA, and passkey operations.
/// </summary>
/// <example>
/// <code>
/// var result = await users.LoginAsync(email, password, orgId, ct);
/// </code>
/// </example>
public class UserStore
{
    private const int DefaultRecoveryCodeCount = 10;
    private const int RecoveryCodeBytes = 10;
    private const int TotpSecretBytes = 20;
    private const int TotpDigits = 6;
    private const long TotpStepSeconds = 30;

    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
    private static readonly ActivitySource ActivitySource = new(OTelConstants.ActivitySourceName);

    private readonly ShipDb db;
    private readonly Fido2 fido2;
    private readonly SessionStore sessions;
    private readonly AuditStore audit;
    private readonly PermissionResolver permissions;
    private readonly IEmailSender emails;
    private readonly IConfiguration configuration;

    private readonly RequestContext requestContext;
    private readonly ILogger<UserStore> logger;

    /// <summary>
    /// Initializes a new <see cref="UserStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="fido2">The FIDO2 core service.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="audit">The audit store.</param>
    /// <param name="permissions">The permission resolver.</param>
    /// <param name="emails">The email sender.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="logger">The logger.</param>
    public UserStore(ShipDb db, Fido2 fido2, SessionStore sessions, AuditStore audit, PermissionResolver permissions, IEmailSender emails, IConfiguration configuration, RequestContext requestContext, ILogger<UserStore> logger)
    {
        this.db = db;
        this.fido2 = fido2;
        this.sessions = sessions;
        this.audit = audit;
        this.permissions = permissions;
        this.emails = emails;
        this.configuration = configuration;
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
        using var activity = ActivitySource.StartActivity("auth.signup", ActivityKind.Internal);
        activity?.SetTag(OTelConstants.AuthAction, "signup");

        var org = await this.db.Orgs.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org is null || org.RequireSso || !org.AllowPasswordAuth)
        {
            activity?.SetTag(OTelConstants.AuthResult, "password_auth_policy_denied");
            await this.audit.RecordAsync("auth.signup.failed", orgId, null, "signup",
                dataJson: "{\"reason\":\"password_auth_policy_denied\"}", ct: ct);
            return (SignupResult.AuthMethodNotAllowed, null, null, null);
        }

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

        var emailDigest = TokenGenerator.ComputeDigestBase64(email);

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

        var passwordHash = PasswordHashing.Hash(password);
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
        using var activity = ActivitySource.StartActivity("auth.login", ActivityKind.Internal);
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

        if (user.StatusId != UserStatus.Active.Id)
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

        if (org.MfaPolicyId == OrganizationMfaPolicy.AllMembers.Id && !await this.UserHasRequiredMfaEnrollmentAsync(user.Id, ct))
        {
            this.logger.LogWarning("Password login requires MFA enrollment: {UserId}", user.Id);
            activity?.SetTag(OTelConstants.AuthResult, "mfa_required");
            await this.audit.RecordAsync("auth.login.failed", user.OrgId, user.Id, "login",
                dataJson: "{\"reason\":\"mfa_required\"}", ct: ct);
            return (LoginResult.MfaRequired, null, null, null);
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

        var (success, needsRehash) = PasswordHashing.Verify(password, auth.PasswordHash);

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
            auth.PasswordHash = PasswordHashing.Hash(password);
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

    private async Task<bool> UserHasRequiredMfaEnrollmentAsync(Guid userId, CancellationToken ct)
    {
        var hasVerifiedFactor = await this.db.UserMfaFactors.AnyAsync(x => x.UserId == userId
            && x.VerifiedAt != null
            && (x.Type == MfaFactorType.Totp.Id
                || x.Type == MfaFactorType.Passkey.Id
                || x.Type == MfaFactorType.WebAuthnSecurityKey.Id), ct);
        if (!hasVerifiedFactor)
        {
            return false;
        }

        return await this.db.UserMfaFactors.AnyAsync(x => x.UserId == userId
            && x.VerifiedAt != null
            && x.Type == MfaFactorType.RecoverCode.Id, ct);
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
        using var activity = ActivitySource.StartActivity("auth.api_key_login", ActivityKind.Internal);
        activity?.SetTag(OTelConstants.AuthAction, "api_key_login");

        var apiKey = await this.AuthenticateUserApiKeyAsync(rawKey, ct);
        if (apiKey is null || apiKey.User is null)
        {
            activity?.SetTag(OTelConstants.AuthResult, "invalid_api_key");
            await this.audit.RecordAsync("auth.api_key_login.failed", null, null, "api_key_login",
                dataJson: "{\"reason\":\"invalid_api_key\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        var user = apiKey.User;

        if (user.StatusId != UserStatus.Active.Id)
        {
            activity?.SetTag(OTelConstants.AuthResult, "account_suspended");
            await this.audit.RecordAsync("auth.api_key_login.failed", user.OrgId, user.Id, "api_key_login",
                dataJson: "{\"reason\":\"account_suspended\"}", ct: ct);
            return (LoginResult.AccountSuspended, null, null, null);
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(o => o.Id == user.OrgId, ct);
        if (org is null || org.RequireSso)
        {
            activity?.SetTag(OTelConstants.AuthResult, "api_key_login_policy_denied");
            await this.audit.RecordAsync("auth.api_key_login.failed", user.OrgId, user.Id, "api_key_login",
                dataJson: "{\"reason\":\"api_key_login_policy_denied\"}", ct: ct);
            return (LoginResult.AuthMethodNotAllowed, null, null, null);
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
        using var activity = ActivitySource.StartActivity("auth.logout", ActivityKind.Internal);

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

        if (!await this.OrgAllowsPasswordMutationAsync(user.OrgId, ct))
        {
            await this.audit.RecordAsync("auth.password_reset.request", user.OrgId, user.Id, "password_reset.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var digest = TokenGenerator.ComputeDigestBase64(rawToken);

        auth.ResetTokenDigest = digest;
        auth.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        await this.db.SaveChangesAsync(ct);

        await this.emails.SendAsync(new EmailMessage(
            user.Email,
            "Reset your NeoShip password",
            $"Use this link to reset your password: {this.PublicBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}"), ct);

        this.logger.LogInformation("Password reset requested for user {UserId}", user.Id);
        await this.audit.RecordAsync("auth.password_reset.request", user.OrgId, user.Id, "password_reset.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("auth.password_reset_confirm", ActivityKind.Internal);

        var digest = TokenGenerator.ComputeDigestBase64(token);

        var auth = await this.db.UserPasswordAuths
            .FirstOrDefaultAsync(a => a.ResetTokenDigest == digest
                && a.ResetTokenExpiresAt > DateTime.UtcNow, ct);

        if (auth is null) return false;

        var user = await this.db.Users.FirstOrDefaultAsync(x => x.Id == auth.UserId, ct);
        if (user is null || !await this.OrgAllowsPasswordMutationAsync(user.OrgId, ct))
        {
            return false;
        }

        auth.PasswordHash = PasswordHashing.Hash(newPassword);
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

    private async Task<bool> OrgAllowsPasswordMutationAsync(Guid orgId, CancellationToken ct)
    {
        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Id == orgId, ct);
        return org is not null && !org.RequireSso && org.AllowPasswordAuth;
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
        var digest = TokenGenerator.ComputeDigestBase64(rawToken);

        userEmail.VerificationTokenDigest = digest;
        userEmail.VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await this.db.SaveChangesAsync(ct);

        await this.emails.SendAsync(new EmailMessage(
            userEmail.Email,
            "Verify your NeoShip email",
            $"Use this link to verify your email: {this.PublicBaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}"), ct);

        await this.audit.RecordAsync("auth.email_verification.request", null, userEmail.UserId, "email_verification.request", dataJson: "{\"result\":\"accepted\"}", ct: ct);
    }

    private string PublicBaseUrl => (this.configuration["Email:PublicBaseUrl"] ?? "https://localhost").TrimEnd('/');

    public async Task<bool> ConfirmEmailVerificationAsync(string token, CancellationToken ct = default)
    {
        var digest = TokenGenerator.ComputeDigestBase64(token);

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

    /// <summary>
    /// Generates a user API key entity and plaintext key.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The API key name.</param>
    /// <param name="description">The API key description.</param>
    /// <param name="scopesJson">The API key scopes JSON.</param>
    /// <param name="expiresAt">The API key expiration.</param>
    /// <returns>The plaintext key and created <see cref="UserApiKey"/>.</returns>
    public (string PlaintextKey, UserApiKey ApiKey) GenerateUserApiKey(Guid userId, string name, string? description, string? scopesJson, DateTime? expiresAt)
    {
        var keyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);

        var plaintextKey = "nsu_" + Convert.ToBase64String(keyBytes);
        var digest = TokenGenerator.ComputeDigestBase64(keyBytes);

        var apiKey = new UserApiKey(userId, name, digest, scopesJson ?? "[]", expiresAt, description);
        return (plaintextKey, apiKey);
    }

    /// <summary>
    /// Authenticates a user API key and updates its last-used timestamp.
    /// </summary>
    /// <param name="rawKey">The plaintext API key.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching API key with its user loaded when valid; otherwise <see langword="null"/>.</returns>
    public async Task<UserApiKey?> AuthenticateUserApiKeyAsync(string rawKey, CancellationToken ct = default)
    {
        if (!TryDecodeUserKey(rawKey, out var keyBytes))
        {
            return null;
        }

        var digest = TokenGenerator.ComputeDigestBase64(keyBytes);

        var key = await this.db.UserApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyDigest == digest
                && k.DeletedAt == null
                && k.RevokedAt == null
                && k.User != null
                && k.User.StatusId == UserStatus.Active.Id
                && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow), ct);

        if (key is null || key.User is null)
        {
            return null;
        }

        key.LastUsedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("API key authenticated: {ApiKeyId} for user {UserId}", key.Id, key.UserId);
        return key;
    }

    /// <summary>
    /// Lists active user API keys.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The active user API keys.</returns>
    public async Task<List<UserApiKey>> ListUserApiKeysAsync(Guid userId, CancellationToken ct = default)
    {
        return await this.db.UserApiKeys
            .Where(k => k.UserId == userId && k.DeletedAt == null && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Revokes a user API key.
    /// </summary>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when revoked; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RevokeUserApiKeyAsync(Guid apiKeyId, Guid userId, CancellationToken ct = default)
    {
        var key = await this.db.UserApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId, ct);

        if (key is null || key.RevokedAt is not null)
        {
            return false;
        }

        key.RevokedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);
        this.logger.LogInformation("API key revoked: {ApiKeyId} for user {UserId}", apiKeyId, userId);
        return true;
    }

    /// <summary>
    /// Starts TOTP setup by creating an unverified factor.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The factor name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="UserMfaFactor"/> and Base32 secret.</returns>
    public async Task<(UserMfaFactor Factor, string Secret)> StartTotpAsync(Guid userId, string? name, CancellationToken ct = default)
    {
        var secretBytes = RandomNumberGenerator.GetBytes(TotpSecretBytes);
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Authenticator app" : name.Trim(),
            Type = MfaFactorType.Totp.Id,
            ValueEncrypted = secretBytes,
            CreatedAt = DateTime.UtcNow,
        };

        this.db.UserMfaFactors.Add(factor);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("TOTP setup started: factor={FactorId} user={UserId}", factor.Id, userId);
        return (factor, ToBase32(secretBytes));
    }

    /// <summary>
    /// Confirms a TOTP factor with a current verification code.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The factor identifier.</param>
    /// <param name="code">The verification code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when confirmed; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ConfirmTotpAsync(Guid userId, Guid factorId, string code, CancellationToken ct = default)
    {
        var factor = await this.db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Totp.Id,
            ct);

        if (factor is null || !VerifyTotp(factor.ValueEncrypted, code, DateTimeOffset.UtcNow))
        {
            return false;
        }

        factor.VerifiedAt = DateTime.UtcNow;
        factor.UpdatedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("TOTP factor confirmed: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Disables a TOTP factor.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The factor identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when disabled; otherwise <see langword="false"/>.</returns>
    public async Task<bool> DisableTotpAsync(Guid userId, Guid factorId, CancellationToken ct = default)
    {
        var factor = await this.db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Totp.Id,
            ct);

        if (factor is null)
        {
            return false;
        }

        this.db.UserMfaFactors.Remove(factor);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("TOTP factor disabled: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Regenerates recovery codes for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="count">The number of recovery codes to generate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The plaintext recovery codes. These values are only returned once.</returns>
    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(Guid userId, int count = DefaultRecoveryCodeCount, CancellationToken ct = default)
    {
        if (count is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Recovery code count must be between 1 and 50.");
        }

        var existing = await this.db.UserMfaFactors
            .Where(x => x.UserId == userId && x.Type == MfaFactorType.RecoverCode.Id)
            .ToListAsync(ct);

        this.db.UserMfaFactors.RemoveRange(existing);

        var now = DateTime.UtcNow;
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var code = GenerateRecoveryCode();
            codes.Add(code);

            this.db.UserMfaFactors.Add(new UserMfaFactor
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Name = "Recovery code",
                Type = MfaFactorType.RecoverCode.Id,
                ValueEncrypted = Encoding.UTF8.GetBytes(PasswordHashing.Hash(NormalizeRecoveryCode(code))),
                VerifiedAt = now,
                CreatedAt = now,
            });
        }

        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Recovery codes regenerated: user={UserId} count={Count}", userId, count);
        return codes;
    }

    /// <summary>
    /// Consumes one recovery code for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="code">The recovery code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when a code was consumed; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ConsumeRecoveryCodeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var normalized = NormalizeRecoveryCode(code);
        var factors = await this.db.UserMfaFactors
            .Where(x => x.UserId == userId && x.Type == MfaFactorType.RecoverCode.Id)
            .ToListAsync(ct);

        var factor = factors.FirstOrDefault(x => PasswordHashing.Verify(normalized, Encoding.UTF8.GetString(x.ValueEncrypted)).Success);
        if (factor is null)
        {
            return false;
        }

        this.db.UserMfaFactors.Remove(factor);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Recovery code consumed: user={UserId}", userId);
        return true;
    }

    /// <summary>
    /// Revokes all recovery codes for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of revoked recovery codes.</returns>
    public async Task<int> RevokeRecoveryCodesAsync(Guid userId, CancellationToken ct = default)
    {
        var factors = await this.db.UserMfaFactors
            .Where(x => x.UserId == userId && x.Type == MfaFactorType.RecoverCode.Id)
            .ToListAsync(ct);

        this.db.UserMfaFactors.RemoveRange(factors);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Recovery codes revoked: user={UserId} count={Count}", userId, factors.Count);
        return factors.Count;
    }

    /// <summary>
    /// Lists passkeys for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user's passkey factors.</returns>
    public async Task<List<UserMfaFactor>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        return await this.db.UserMfaFactors
            .Where(x => x.UserId == userId && x.Type == MfaFactorType.Passkey.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Begins passkey registration for a user.
    /// </summary>
    /// <param name="user">The user registering a passkey.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential creation options that must be sent to the browser.</returns>
    public async Task<CredentialCreateOptions> BeginRegistrationAsync(User user, CancellationToken ct = default)
    {
        var existingCredentials = await this.db.UserMfaFactors
            .Where(x => x.UserId == user.Id && x.Type == MfaFactorType.Passkey.Id)
            .Select(x => new PublicKeyCredentialDescriptor(x.WebAuthnCredentialId))
            .ToListAsync(ct);

        var options = this.fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString("N")),
                Name = user.Email,
                DisplayName = user.Name,
            },
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true,
            },
        });

        this.logger.LogInformation("Passkey registration started: user={UserId}", user.Id);
        return options;
    }

    /// <summary>
    /// Begins passkey login for a user email.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user and assertion options, or <see langword="null"/>.</returns>
    public async Task<(User User, AssertionOptions Options)?> BeginLoginAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();
        var user = await this.db.Users.FirstOrDefaultAsync(x => x.EmailUpcase == emailUpcase, ct);
        if (user is null || user.StatusId != UserStatus.Active.Id)
        {
            return null;
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Id == user.OrgId, ct);
        if (org is null || org.RequireSso || !org.AllowPasskeyAuth)
        {
            return null;
        }

        var credentials = await this.db.UserMfaFactors
            .Where(x => x.UserId == user.Id && x.Type == MfaFactorType.Passkey.Id)
            .Select(x => new PublicKeyCredentialDescriptor(x.WebAuthnCredentialId))
            .ToListAsync(ct);

        if (credentials.Count == 0)
        {
            return null;
        }

        var options = this.fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials,
            UserVerification = UserVerificationRequirement.Preferred,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
            },
        });

        this.logger.LogInformation("Passkey login started: user={UserId}", user.Id);
        return (user, options);
    }

    /// <summary>
    /// Finishes passkey login and updates credential metadata.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="options">The original assertion options.</param>
    /// <param name="response">The authenticator assertion response.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated user and passkey factor, or <see langword="null"/>.</returns>
    public async Task<(User User, UserMfaFactor Factor)?> FinishLoginAsync(Guid userId, AssertionOptions options, AuthenticatorAssertionRawResponse response, CancellationToken ct = default)
    {
        var credentialDigest = ComputeCredentialIdDigest(response.RawId);
        var factor = await this.db.UserMfaFactors
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                    && x.Type == MfaFactorType.Passkey.Id
                    && x.WebAuthnCredentialIdDigest == credentialDigest,
                ct);

        if (factor?.User is null || factor.User.StatusId != UserStatus.Active.Id)
        {
            return null;
        }

        var org = await this.db.Orgs.FirstOrDefaultAsync(x => x.Id == factor.User.OrgId, ct);
        if (org is null || org.RequireSso || !org.AllowPasskeyAuth)
        {
            return null;
        }

        var expectedUserHandle = Encoding.UTF8.GetBytes(factor.UserId.ToString("N"));
        var result = await this.fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = factor.WebAuthnPublicKeyCredentialData,
            StoredSignatureCounter = factor.WebAuthnSignCount,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(
                args.CredentialId.SequenceEqual(factor.WebAuthnCredentialId)
                    && args.UserHandle.SequenceEqual(expectedUserHandle)),
        }, ct);

        factor.WebAuthnSignCount = result.SignCount;
        factor.LastUsedAt = DateTime.UtcNow;
        factor.UpdatedAt = DateTime.UtcNow;
        factor.User.LastLoginAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Passkey login completed: factor={FactorId} user={UserId}", factor.Id, userId);
        return (factor.User, factor);
    }

    /// <summary>
    /// Finishes passkey registration and stores the verified credential.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The passkey name.</param>
    /// <param name="options">The original credential creation options.</param>
    /// <param name="response">The authenticator attestation response.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stored passkey factor.</returns>
    public async Task<UserMfaFactor> FinishRegistrationAsync(Guid userId, string? name, CredentialCreateOptions options, AuthenticatorAttestationRawResponse response, CancellationToken ct = default)
    {
        var result = await this.fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, cancellationToken) =>
            {
                var digest = ComputeCredentialIdDigest(args.CredentialId);
                return !await this.db.UserMfaFactors.AnyAsync(
                    x => x.Type == MfaFactorType.Passkey.Id && x.WebAuthnCredentialIdDigest == digest,
                    cancellationToken);
            },
        }, ct);

        var now = DateTime.UtcNow;
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Passkey" : name.Trim(),
            Type = MfaFactorType.Passkey.Id,
            WebAuthnCredentialId = result.Id,
            WebAuthnCredentialIdDigest = ComputeCredentialIdDigest(result.Id),
            WebAuthnPublicKeyCredentialData = result.PublicKey,
            WebAuthnSignCount = result.SignCount,
            TransportsJson = JsonSerializer.Serialize(result.Transports.Select(x => x.ToString()).ToArray()),
            CreatedAt = now,
            VerifiedAt = now,
        };

        this.db.UserMfaFactors.Add(factor);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Passkey registered: factor={FactorId} user={UserId}", factor.Id, userId);
        return factor;
    }

    /// <summary>
    /// Revokes a passkey for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The passkey factor identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when revoked; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RevokeAsync(Guid userId, Guid factorId, CancellationToken ct = default)
    {
        var factor = await this.db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Passkey.Id,
            ct);

        if (factor is null)
        {
            return false;
        }

        this.db.UserMfaFactors.Remove(factor);
        await this.db.SaveChangesAsync(ct);

        this.logger.LogInformation("Passkey revoked: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Computes a TOTP code for a secret at a timestamp.
    /// </summary>
    /// <param name="secret">The shared secret bytes.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>The six-digit TOTP code.</returns>
    public static string ComputeTotp(byte[] secret, DateTimeOffset timestamp)
    {
        var counter = timestamp.ToUnixTimeSeconds() / TotpStepSeconds;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);

        var otp = binary % 1_000_000;
        return otp.ToString($"D{TotpDigits}");
    }

    /// <summary>
    /// Converts bytes to Base32 without padding.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base32-encoded value.</returns>
    public static string ToBase32(byte[] bytes)
    {
        var output = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                output.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    /// <summary>
    /// Computes the stored lookup digest for a WebAuthn credential identifier.
    /// </summary>
    /// <param name="credentialId">The WebAuthn credential identifier.</param>
    /// <returns>The Base64 digest.</returns>
    public static string ComputeCredentialIdDigest(byte[] credentialId)
    {
        return TokenGenerator.ComputeDigestBase64(credentialId);
    }

    private static bool TryDecodeUserKey(string rawKey, out byte[] keyBytes)
    {
        keyBytes = [];

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return false;
        }

        if (!rawKey.StartsWith("nsu_", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            keyBytes = Convert.FromBase64String(rawKey[4..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string GenerateRecoveryCode()
    {
        var encoded = ToBase32(RandomNumberGenerator.GetBytes(RecoveryCodeBytes));

        return string.Create(11, encoded, static (span, value) =>
        {
            value.AsSpan(0, 5).CopyTo(span);
            span[5] = '-';
            value.AsSpan(5, 5).CopyTo(span[6..]);
        });
    }

    private static string NormalizeRecoveryCode(string code)
    {
        return code.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static bool VerifyTotp(byte[] secret, string code, DateTimeOffset timestamp)
    {
        var normalized = code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Length != TotpDigits || normalized.Any(c => c is < '0' or > '9'))
        {
            return false;
        }

        for (var offset = -1; offset <= 1; offset++)
        {
            var candidate = ComputeTotp(secret, timestamp.AddSeconds(offset * TotpStepSeconds));
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(normalized)))
            {
                return true;
            }
        }

        return false;
    }
}
