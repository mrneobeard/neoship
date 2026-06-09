using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class UserApiKey
{
    public UserApiKey()
    {
    }

    public UserApiKey(Guid userId)
    {
        this.UserId = userId;
    }

    public UserApiKey(Guid userId, string name, string keyDigest, string scopesJson, DateTime? expiresAt = null, string? description = null)
    {
        this.UserId = userId;
        this.Name = name;
        this.KeyDigest = keyDigest;
        this.ScopesJson = scopesJson;
        this.ExpiresAt = expiresAt;
        this.Description = description;
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; } = Guid.Empty;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Description { get; set; } = null;

    [StringLength(1024)]
    public string KeyDigest { get; set; } = string.Empty;

    [StringLength(1024)]
    public string ScopesJson { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; } = null;

    public DateTime? RevokedAt { get; set; } = null;

    public DateTime? DeletedAt { get; set; } = null;

    public DateTime? LastUsedAt { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}