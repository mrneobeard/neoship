using Microsoft.EntityFrameworkCore;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Services;

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

public class AuthService
{
    private readonly ShipDb _db;
    private readonly PasswordService _passwords;
    private readonly SessionService _sessions;
    private readonly AuditService _audit;

    public AuthService(ShipDb db, PasswordService passwords, SessionService sessions, AuditService audit)
    {
        _db = db;
        _passwords = passwords;
        _sessions = sessions;
        _audit = audit;
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

        var exists = await _db.Users.AnyAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (exists)
        {
            await _audit.RecordAsync("auth.signup.failed", orgId, null, "signup", ipAddress, userAgent,
                dataJson: "{\"reason\":\"email_exists\"}", ct: ct);
            return (SignupResult.EmailAlreadyExists, null, null, null);
        }

        var userId = Factory.NewGuid();
        var user = new User(userId, email, name)
        {
            OrgId = orgId,
            StatusId = UserStatus.Active.Id,
        };

        var emailDigest = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(email)));

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

        var passwordHash = _passwords.Hash(password);
        var userPasswordAuth = new UserPasswordAuth
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        _db.UserEmails.Add(userEmail);
        _db.UserPasswordAuths.Add(userPasswordAuth);
        await _db.SaveChangesAsync(ct);

        var (session, rawToken) = await _sessions.CreateSessionAsync(userId, orgId, ipAddress, userAgent, ct);

        await _audit.RecordAsync("auth.signup.success", orgId, userId, "signup", ipAddress, userAgent, ct: ct);

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

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);

        if (user is null)
        {
            await _audit.RecordAsync("auth.login.failed", orgId, null, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"user_not_found\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (user.StatusId == UserStatus.Suspended.Id)
        {
            await _audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"account_suspended\"}", ct: ct);
            return (LoginResult.AccountSuspended, null, null, null);
        }

        var auth = await _db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);

        if (auth is null)
        {
            await _audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"no_password_auth\"}", ct: ct);
            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (auth.LockedUntil.HasValue && auth.LockedUntil > DateTime.UtcNow)
        {
            await _audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"account_locked\"}", ct: ct);
            return (LoginResult.AccountLocked, null, null, null);
        }

        var (success, needsRehash) = _passwords.Verify(password, auth.PasswordHash);

        if (!success)
        {
            auth.FailedAttempts += 1;
            if (auth.FailedAttempts >= 5)
            {
                auth.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            }

            await _db.SaveChangesAsync(ct);

            await _audit.RecordAsync("auth.login.failed", orgId, user.Id, "login", ipAddress, userAgent,
                dataJson: "{\"reason\":\"invalid_password\"}", ct: ct);

            return (LoginResult.InvalidCredentials, null, null, null);
        }

        if (needsRehash)
        {
            auth.PasswordHash = _passwords.Hash(password);
        }

        auth.FailedAttempts = 0;
        auth.LockedUntil = null;

        user.LastLoginIp = ipAddress;
        user.LastLoginAt = DateTime.UtcNow;

        var (session, rawToken) = await _sessions.CreateSessionAsync(user.Id, orgId, ipAddress, userAgent, ct);

        await _audit.RecordAsync("auth.login.success", orgId, user.Id, "login", ipAddress, userAgent, ct: ct);

        return (LoginResult.Success, user, session, rawToken);
    }

    public async Task LogoutAsync(string tokenDigest, CancellationToken ct = default)
    {
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == tokenDigest && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = "logout";
            await _db.SaveChangesAsync(ct);

            await _audit.RecordAsync("auth.logout", session.OrgId, session.UserId, "logout",
                session.IpAddress, session.UserAgent, ct: ct);
        }
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailUpcase == emailUpcase, ct);
        if (user is null)
        {
            return;
        }

        var auth = await _db.UserPasswordAuths.FirstOrDefaultAsync(a => a.UserId == user.Id, ct);
        if (auth is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var digest = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        auth.ResetTokenDigest = digest;
        auth.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ConfirmPasswordResetAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var digest = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token)));

        var auth = await _db.UserPasswordAuths
            .FirstOrDefaultAsync(a => a.ResetTokenDigest == digest
                && a.ResetTokenExpiresAt > DateTime.UtcNow, ct);

        if (auth is null)
        {
            return false;
        }

        auth.PasswordHash = _passwords.Hash(newPassword);
        auth.ResetTokenDigest = null;
        auth.ResetTokenExpiresAt = null;
        auth.FailedAttempts = 0;
        auth.LockedUntil = null;
        auth.PasswordChangedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync("auth.password_reset", null, auth.UserId, "password_reset", null, null, ct: ct);

        return true;
    }

    public async Task RequestEmailVerificationAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();

        var userEmail = await _db.UserEmails
            .FirstOrDefaultAsync(e => e.EmailUpcase == emailUpcase && e.StatusId == UserEmailStatus.Pending.Id, ct);

        if (userEmail is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var digest = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        userEmail.VerificationTokenDigest = digest;
        userEmail.VerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ConfirmEmailVerificationAsync(string token, CancellationToken ct = default)
    {
        var digest = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token)));

        var userEmail = await _db.UserEmails
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
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync("auth.email_verified", null, userEmail.UserId, "email_verified", null, null, ct: ct);

        return true;
    }
}
