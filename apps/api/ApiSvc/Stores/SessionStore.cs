using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class SessionStore
{
    private readonly ShipDb db;
    private readonly TokenStore tokens;
    private readonly RequestContext requestContext;
    private readonly ILogger<SessionStore> logger;

    public SessionStore(ShipDb db, TokenStore tokens, RequestContext requestContext, ILogger<SessionStore> logger)
    {
        this.db = db;
        this.tokens = tokens;
        this.requestContext = requestContext;
        this.logger = logger;
    }

    public async Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        Guid orgId,
        CancellationToken ct = default)
    {
        var (rawToken, digest) = tokens.GenerateSessionToken();
        var digestBase64 = Convert.ToBase64String(digest);

        var session = new UserSession
        {
            Id = Factory.NewGuid(),
            UserId = userId,
            OrgId = orgId,
            TokenDigest = digestBase64,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Session created: {SessionId} for user {UserId}", session.Id, userId);
        return (session, rawToken);
    }

    public async Task<UserSession?> ValidateSessionAsync(string rawToken, CancellationToken ct = default)
    {
        var tokenBytes = Convert.FromBase64String(rawToken);
        var digest = TokenStore.ComputeDigest(tokenBytes);
        var digestBase64 = Convert.ToBase64String(digest);

        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.TokenDigest == digestBase64
                && s.ExpiresAt > DateTime.UtcNow
                && s.RevokedAt == null, ct);

        if (session is not null)
        {
            session.LastUsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            requestContext.UserId = session.UserId;
            requestContext.OrgId = session.OrgId;
            requestContext.SessionId = session.Id;
        }
        else
        {
            logger.LogDebug("Session validation failed for digest");
        }

        return session;
    }

    public async Task RevokeSessionAsync(Guid sessionId, string? reason = null, CancellationToken ct = default)
    {
        var session = await db.UserSessions.FindAsync([sessionId], ct);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevokeReason = reason;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Session revoked: {SessionId} for user {UserId} reason={Reason}",
                sessionId, session.UserId, reason);
        }
    }

    public async Task<List<UserSession>> ListSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);
    }
}