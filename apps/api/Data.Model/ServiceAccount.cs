using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class ServiceAccount
{
    public Guid Id { get; set; } = Guid.Empty;

    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    [StringLength(64)]
    public string NameUpcase { get; set; } = string.Empty;

    [StringLength(256)]
    public string Description { get; set; } = string.Empty;

    public Guid OrgId { get; set; } = Guid.Empty;

    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid CreatedBy { get; set; } = Guid.Empty;

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    public DateTime? UpdatedAt { get; set; } = null;

    public DateTime? DeletedAt { get; set; } = null;
}

public readonly struct ServiceAccountStatus
{
    public ServiceAccountStatus(ushort id, string name)
    {
        this.Id = id;
        this.Name = string.Intern(name);
    }

    public ushort Id { get; init; }

    public string Name { get; init; }

    public bool IsActive => this.Id < 100;

    public static ServiceAccountStatus Active => new(1, "active");

    public static ServiceAccountStatus Inactive => new(100, "inactive");

    public static ServiceAccountStatus Suspended => new(200, "suspended");

    public static ServiceAccountStatus Deleted => new(300, "deleted");
}