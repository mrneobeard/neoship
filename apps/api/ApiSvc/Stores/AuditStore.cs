using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class AuditStore
{
    private readonly ShipDb db;
    private readonly RequestContext requestContext;
    private readonly ILogger<AuditStore> logger;

    public AuditStore(ShipDb db, RequestContext requestContext, ILogger<AuditStore> logger)
    {
        this.db = db;
        this.requestContext = requestContext;
        this.logger = logger;
    }

    public async Task RecordAsync(
        string type,
        Guid? orgId,
        Guid? userId,
        string? action,
        string? targetType = null,
        string? targetId = null,
        string? dataJson = null,
        CancellationToken ct = default)
    {
        var evt = new AuditEvent
        {
            Type = type,
            OrgId = orgId ?? requestContext.OrgId,
            UserId = userId ?? requestContext.UserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = dataJson,
            IpAddress = requestContext.IpAddress,
            UserAgent = requestContext.UserAgent,
            SessionId = requestContext.SessionId?.ToString(),
            TraceId = requestContext.TraceId,
            SpanId = requestContext.SpanId,
            Timestamp = DateTime.UtcNow,
        };

        db.AuditEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Audit event recorded: {Type} action={Action} userId={UserId} orgId={OrgId}",
            type, action, evt.UserId, evt.OrgId);
    }
}