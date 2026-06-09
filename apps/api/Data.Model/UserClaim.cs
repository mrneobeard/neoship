using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class UserClaim
{
    public ulong Id { get; set; }

    public Guid UserId { get; set; }

    [StringLength(128)]
    public string Type { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Value { get; set; }
}
