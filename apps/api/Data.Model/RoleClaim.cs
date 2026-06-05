using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class RoleClaim
{
    public ulong Id { get; set; }

    public Guid RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
