using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class AuditEvent
{
    public ulong Id { get; set; } = 0;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(256)]
    [Required]
    public string Type { get; set; } = string.Empty;

    public string? DataJson { get; set; } = null;

    public Guid? OrgId { get; set; } = Guid.Empty;

    public Guid? UserId { get; set; } = Guid.Empty;

    public string? TargetType { get; set; } = null;

    public string? TargetId { get; set; } = null;

    public string? Action { get; set; } = null;

    public string? RequestId { get; set; } = null;

    public string? SessionId { get; set; } = null;

    public string? MachineName { get; set; } = null;

    public string? TraceId { get; set; } = null;

    public string? SpanId { get; set; } = null;

    public string? ParentSpanId { get; set; } = null;

    public string? IpAddress { get; set; } = null;

    public string? IpDigest { get; set; } = null;

    public string? UserAgent { get; set; } = null;

    public string? CountryCode { get; set; } = null;

    public string? Region { get; set; } = null;

    public uint? Asn { get; set; } = null;

    public ushort RiskLevel { get; set; } = 0;

    public string? RiskFlagsJson { get; set; } = null;
}
