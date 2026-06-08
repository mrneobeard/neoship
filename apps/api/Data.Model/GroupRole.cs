using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class GroupRole
{
    public Guid GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; } = Guid.Empty;
}