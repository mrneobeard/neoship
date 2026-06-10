using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Creates, validates, and revokes browser sessions.
/// </summary>
/// <example>
/// <code>
/// var (session, rawToken) = await sessions.CreateSessionAsync(userId, orgId, permissions, ct);
/// </code>
/// </example>
public class SessionStore
{
    private readonly ShipDb db;
    private readonly TokenStore tokens;
    private readonly RequestContext requestContext;
    private readonly ILogger<SessionStore> logger;
    private readonly PermissionSnapshotCodec snapshotCodec;

    /// <summary>
    /// Initializes a new <see cref="SessionStore"/> instance.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="tokens">The token helper.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="snapshotCodec">The permission snapshot codec.</param>
    /// <param name="logger">The logger.</param>
    public SessionStore(ShipDb db, TokenStore tokens, RequestContext requestContext, PermissionSnapshotCodec snapshotCodec, ILogger<SessionStore> logger)
    {
        this.db = db;
        this.tokens = tokens;
        this.requestContext = requestContext;
        this.snapshotCodec = snapshotCodec;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a new browser session for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="orgId">The current organization identifier.</param>
    /// <param name="permissions">The optional permission snapshot.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="UserSession"/> and raw token.</returns>
    public async Task<(UserSession Session, string RawToken)> CreateSessionAsync(
        Guid userId,
        Guid orgId,
        PermissionSet? permissions = null,
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
            ClaimsJson = permissions is null ? null : this.snapshotCodec.Serialize(permissions),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Session created: {SessionId} for user {UserId}", session.Id, userId);
        return (session, rawToken);
    }

    /// <summary>
    /// Validates a raw session token and updates the session activity marker.
    /// </summary>
    /// <param name="rawToken">The raw session token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The active <see cref="UserSession"/>, or <see langword="null"/>.</returns>
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

    /// <summary>
    /// Records MFA verification on an active session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task MarkMfaVerifiedAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.UserSessions.FindAsync([sessionId], ct);
        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= DateTime.UtcNow)
        {
            return;
        }

        session.MfaVerifiedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Revokes an active session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="reason">The optional revocation reason.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Lists active sessions for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The active <see cref="UserSession"/> list.</returns>
    public async Task<List<UserSession>> ListSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt)
            .ToListAsync(ct);
    }
}