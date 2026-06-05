namespace NeoShip.Data.Model;

/// <summary>
/// Represents a known network for a user. This is used to track the networks that a user has accessed from
/// for anomaly detection and prevention.
/// </summary>
public class UserKnownNetwork
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid UserId { get; set; } = Guid.Empty;

    public string? IpAddress { get; set; } = null;

    public string? IpDigest { get; set; } = null;

    public string? CountryCode { get; set; } = null;

    public string? Region { get; set; } = null;

    public uint? Asn { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public uint Count { get; set; } = 0;
}