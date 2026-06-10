using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class AuditEvent
{
    public long Id { get; set; } = 0;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(256)]
    [Required]
    public string Type { get; set; } = string.Empty;

    public Guid? OrgId { get; set; } = Guid.Empty;

    public Guid? UserId { get; set; } = Guid.Empty;

    [StringLength(256)]
    public string? TargetType { get; set; } = null;

    [StringLength(256)]
    public string? TargetId { get; set; } = null;

    [StringLength(128)]
    [Required]
    public string? Action { get; set; } = null;

    [StringLength(128)]
    public string? RequestId { get; set; } = null;

    [StringLength(128)]
    public string? SessionId { get; set; } = null;

    [StringLength(64)]
    public string? MachineName { get; set; } = null;

    [StringLength(32)]
    public string? TraceId { get; set; } = null;

    [StringLength(32)]
    public string? SpanId { get; set; } = null;

    [StringLength(32)]
    public string? ParentSpanId { get; set; } = null;

    [StringLength(46)]
    public string? IpAddress { get; set; } = null;

    [StringLength(512)]
    public string? IpDigest { get; set; } = null;

    [StringLength(1024)]
    public string? UserAgent { get; set; } = null;

    [StringLength(2)]
    public string? CountryCode { get; set; } = null;

    [StringLength(256)]
    public string? Region { get; set; } = null;

    public uint? Asn { get; set; } = null;

    public ushort RiskLevel { get; set; } = 0;

    [StringLength(1024)]
    public string? RiskFlagsJson { get; set; } = null;

    [StringLength(4096)]
    public string? DataJson { get; set; } = null;
}