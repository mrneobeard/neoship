using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class ServiceAccountClaim
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid ServiceAccountId { get; set; } = Guid.Empty;

    public ServiceAccount? ServiceAccount { get; set; }

    [StringLength(128)]
    public string Type { get; set; } = string.Empty;

    [StringLength(1024)]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}