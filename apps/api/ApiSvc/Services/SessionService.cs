using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Services;

public class SessionService
{
    private readonly ShipDb _db;
    private readonly TokenService _tokens;

    public SessionService(ShipDb db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        Guid orgId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var (rawToken, digest) = _tokens.GenerateSessionToken();
        var digestBase64 = Convert.ToBase64String(digest);

        var session = new UserSession
        {
            Id = Factory.NewGuid(),
            UserId = userId,
            OrgId = orgId,
            TokenDigest = digestBase64,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return (session, rawToken);
    }

    public async Task<UserSession?> ValidateSessionAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenBytes = Convert.FromBase64String(rawToken);
        var digest = SHA256.HashData(tokenBytes);
        var digestBase64 = Convert.ToBase64String(digest);

        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == digestBase64
                && s.ExpiresAt > DateTime.UtcNow
                && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return session;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string? reason = null, CancellationToken ct = default)
    {
        var session = await _db.UserSessions.FindAsync([sessionId], ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = reason;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<UserSession>> ListSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);
    }
}
