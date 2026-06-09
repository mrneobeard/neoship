using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class UserSession
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid UserId { get; set; } = Guid.Empty;

    public Guid OrgId { get; set; } = Guid.Empty;

    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [StringLength(512)]
    public string TokenDigest { get; set; } = string.Empty;

    [StringLength(46)]
    public string? IpAddress { get; set; } = string.Empty;

    [StringLength(128)]
    public string? IpDigest { get; set; } = null;

    [StringLength(512)]
    public string? UserAgent { get; set; } = null;

    public ushort RiskLevel { get; set; } = 0;

    [StringLength(1024)]
    public string? RiskFlagsJson { get; set; } = null;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public DateTime? MfaVerifiedAt { get; set; } = null;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);

    public DateTime? RevokedAt { get; set; } = null;

    [StringLength(512)]
    public string? RevokeReason { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    [StringLength(4096)]
    public string? ClaimsJson { get; set; } = null;
}
