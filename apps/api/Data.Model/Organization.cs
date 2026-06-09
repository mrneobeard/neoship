using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

[Table("orgs")]
public class Organization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(128)]
    public string NameUpcase { get; set; } = string.Empty;

    [StringLength(128)]
    public string Slug { get; set; } = string.Empty;

    public ushort OrganizationPlanId { get; set; } = 1;

    public ushort TenantModeId { get; set; }

    public ushort StatusId { get; set; } = 1;

    [NotMapped]
    public bool IsPrimaryTenant => this.Id.Equals(Guid.Empty);

    [NotMapped]
    public OrganizationStatus Status
    {
        get => this.StatusId;
        set => this.StatusId = value;
    }

    [NotMapped]
    public TenantMode TenantMode
    {
        get => this.TenantModeId;
        set => this.TenantModeId = value;
    }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}