namespace NeoShip.Data.Model;

public class ServiceAccountApiKey
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid ServiceAccountId { get; set; } = Guid.Empty;

    public ServiceAccount? ServiceAccount { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; } = null;

    public string KeyDigest { get; set; } = string.Empty;

    public string ScopesJson { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; } = null;

    public DateTime? RevokedAt { get; set; } = null;

    public DateTime? DeletedAt { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    public HashSet<Role> Roles { get; set; } = new();
}
