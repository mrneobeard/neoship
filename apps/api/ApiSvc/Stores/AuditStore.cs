using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

public class AuditStore
{
    private readonly ShipDb db;

    public AuditStore(ShipDb db)
    {
        this.db = db;
    }

    public async Task RecordAsync(
        string type,
        Guid? orgId,
        Guid? userId,
        string? action,
        string? ipAddress,
        string? userAgent,
        string? targetType = null,
        string? targetId = null,
        string? dataJson = null,
        CancellationToken ct = default)
    {
        var evt = new AuditEvent
        {
            Type = type,
            OrgId = orgId,
            UserId = userId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DataJson = dataJson,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow,
        };

        this.db.AuditEvents.Add(evt);
        await this.db.SaveChangesAsync(ct);
    }
}