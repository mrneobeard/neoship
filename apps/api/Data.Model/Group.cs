using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class Group
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid OrgId { get; set; } = Guid.Empty;

    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NameUpcase { get; set; } = string.Empty;

    public string? Email { get; set; } = null;

    public string? EmailUpcase { get; set; } = null;

    public string? Description { get; set; } = null;

    public HashSet<Role> Roles { get; set; } = new();

    public HashSet<User> Members { get; set; } = new();

    public HashSet<ServiceAccount> ServiceAccountMembers { get; set; } = new();

    public HashSet<User> Owners { get; set; } = new();

    public HashSet<ServiceAccount> ServiceAccountOwners { get; set; } = new();
}
