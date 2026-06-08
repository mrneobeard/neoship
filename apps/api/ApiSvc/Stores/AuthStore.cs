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
}

public class AuthStore
{
    private readonly ShipDb db;
    private readonly PasswordStore passwords;
    private readonly SessionStore sessions;
    private readonly AuditStore audit;

    public AuthStore(ShipDb db, PasswordStore passwords, SessionStore sessions, AuditStore audit)
    {
        this.db = db;
        this.passwords = passwords;
        this.sessions = sessions;
        this.audit = audit;
    }

    public async Task<(SignupResult Result, User? User, UserSession? Session, string? RawToken)> SignupAsync(
        string email,
        string name,
        string password,
        Guid orgId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var exists = await this.db.Users.AnyAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (exists)
        {
            await this.audit.RecordAsync("auth.signup.failed", orgId, null, "signup", ipAddress, userAgent,
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
        this.db.UserEmails.Add(userEmail);
        this.db.UserPasswordAuths.Add(userPasswordAuth);
        await this.db.SaveChangesAsync(ct);

        var (session, rawToken) = await this.sessions.CreateSessionAsync(userId, orgId, ipAddress, userAgent, ct);

        await this.audit.RecordAsync("auth.signup.success", orgId, userId, "signup", ipAddress, userAgent, ct: ct);

        return (SignupResult.Success, user, session, rawToken);
    }

    public async Task<(LoginResult Result, User? User, UserSession? Session, string? RawToken)> LoginAsync(
        string email,
        string password,
        Guid orgId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var user = await this.db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);

        if (user is null)
        {
            await this.audit.RecordAsync("auth.login.failed", orgId, null, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"user_not_found\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (user.StatusId == UserStatus.Suspended.Id)
        {
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"account_suspended\"}", ct: ct);
            return (LoginResult.AccountSuspended, null, null, null);
        }

        var auth = await this.db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);

        if (auth is null)
        {
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"no_password_auth\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (auth.LockedUntil.HasValue && auth.LockedUntil > DateTime.UtcNow)
        {
            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
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

            await this.audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"invalid_password\"}", ct: ct);

            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (needsRehash)
        {
            auth.PasswordHash = this.passwords.Hash(password);
        }

        auth.FailedAttempts = 0;
        auth.LockedUntil = null;

        user.LastLoginIp = ipAddress;
        user.LastLoginAt = DateTime.UtcNow;

        var (session, rawToken) = await this.sessions.CreateSessionAsync(user.Id, orgId, ipAddress, userAgent, ct);

        await this.audit.RecordAsync("auth.login.success", orgId, user.Id, "login", ipAddress, userAgent, ct: ct);

        return (LoginResult.Success, user, session, rawToken);
    }

    public async Task LogoutAsync(string tokenDigest, CancellationToken ct = default)
    {
        var session = await this.db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == tokenDigest && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = "logout";
            await this.db.SaveChangesAsync(ct);

            await this.audit.RecordAsync("auth.logout", session.OrgId, session.UserId, "logout",
                session.IpAddress, session.UserAgent, ct: ct);
        }
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var user = await this.db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (user is null)
        {
            return;
        }

        var auth = await this.db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);
        if (auth is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var digest = TokenStore.ComputeDigestBase64(rawToken);

        auth.ResetTokenDigest = digest;
        auth.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        await this.db.SaveChangesAsync(ct);
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var digest = TokenStore.ComputeDigestBase64(token);

        var auth = await this.db.UserPasswordAuths
            .FirstOrDefaultAsync(a => a.ResetTokenDigest == digest
                && a.ResetTokenExpiresAt > DateTime.UtcNow, ct);

        if (auth is null)
        {
            return false;
        }

        auth.PasswordHash = this.passwords.Hash(newPassword);
        auth.ResetTokenDigest = null;
        auth.ResetTokenExpiresAt = null;
        auth.FailedAttempts = 0;
        auth.LockedUntil = null;
        auth.PasswordChangedAt = DateTime.UtcNow;
        await this.db.SaveChangesAsync(ct);

        await this.audit.RecordAsync("auth.password_reset", null, auth.UserId, "password_reset", null, null, ct: ct);

        return true;
    }

    public async Task RequestEmailVerificationAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var userEmail = await this.db.UserEmails
            .FirstOrDefaultAsync(e => e.EmailUpcase == emailUpcase && e.StatusId == UserEmailStatus.Pending.Id, ct);

        if (userEmail is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var digest = TokenStore.ComputeDigestBase64(rawToken);

        userEmail.VerificationTokenDigest = digest;
        userEmail.VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await this.db.SaveChangesAsync(ct);
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

        await this.audit.RecordAsync("auth.email_verified", null, userEmail.UserId, "email_verified", null, null, ct: ct);

        return true;
    }
}