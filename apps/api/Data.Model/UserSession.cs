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

    public string TokenDigest { get; set; } = string.Empty;

    public string? IpAddress { get; set; } = string.Empty;

    public string? IpHash { get; set; } = null;

    public string? IpPrefix { get; set; } = null;

    public string? UserAgent { get; set; } = null;

    public ushort RiskLevel { get; set; } = 0;

    public string? RiskFlagsJson { get; set; } = null;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public DateTime? MfaVerifiedAt { get; set; } = null;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);

    public DateTime? RevokedAt { get; set; } = null;

    public string? RevokeReason { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    public string? ClaimsJson { get; set; } = null;
}