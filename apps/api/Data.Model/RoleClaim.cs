using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class RoleClaim
{
    public long Id { get; set; }

    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [StringLength(128)]
    [Required]
    public string Type { get; set; } = string.Empty;

    [StringLength(1024)]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; } = Guid.Empty;

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }
}