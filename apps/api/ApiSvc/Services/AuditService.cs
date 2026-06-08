using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Services;

public class AuditService
{
    private readonly ShipDb _db;

    public AuditService(ShipDb db)
    {
        _db = db;
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

        _db.AuditEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
    }
}
