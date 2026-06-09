using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents a known network for a user. This is used to track the networks that a user has accessed from
/// for anomaly detection and prevention.
/// </summary>
public class UserKnownNetwork
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid UserId { get; set; } = Guid.Empty;

    [StringLength(46)]
    public string? IpAddress { get; set; } = null;

    [StringLength(128)]
    public string? IpDigest { get; set; } = null;

    [StringLength(2)]
    public string? CountryCode { get; set; } = null;

    [StringLength(128)]
    public string? Region { get; set; } = null;

    public uint? Asn { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public uint Count { get; set; } = 0;
}