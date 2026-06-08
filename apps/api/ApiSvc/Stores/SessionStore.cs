using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class SessionStore
{
    private readonly ShipDb db;
    private readonly TokenStore tokens;

    public SessionStore(ShipDb db, TokenStore tokens)
    {
        this.db = db;
        this.tokens = tokens;
    }

    public async Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        Guid orgId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var (rawToken, digest) = this.tokens.GenerateSessionToken();
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

        this.db.UserSessions.Add(session);
        await this.db.SaveChangesAsync(ct);

        return (session, rawToken);
    }

    public async Task<UserSession?> ValidateSessionAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenBytes = Convert.FromBase64String(rawToken);
        var digest = TokenStore.ComputeDigest(tokenBytes);
        var digestBase64 = Convert.ToBase64String(digest);

        var session = await this.db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == digestBase64
                && s.ExpiresAt > DateTime.UtcNow
                && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.LastUsedAt = DateTime.UtcNow;
            await this.db.SaveChangesAsync(ct);
        }

        return session;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string? reason = null, CancellationToken ct = default)
    {
        var session = await this.db.UserSessions.FindAsync([sessionId], ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = reason;
            await this.db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<UserSession>> ListSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await this.db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);
    }
}