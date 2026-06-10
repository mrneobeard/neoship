using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class ServiceAccountApiKey
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid ServiceAccountId { get; set; } = Guid.Empty;

    public ServiceAccount? ServiceAccount { get; set; }

    [StringLength(64)]
    [Required]
    public string Name { get; set; } = string.Empty;

    [StringLength(512)]
    public string? Description { get; set; } = null;

    [StringLength(1024)]
    [Required]
    public string KeyDigest { get; set; } = string.Empty;

    [StringLength(2048)]
    [Required]
    public string ScopesJson { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; } = null;

    public DateTime? RevokedAt { get; set; } = null;

    public DateTime? DeletedAt { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    public HashSet<Role> Roles { get; set; } = new();
}