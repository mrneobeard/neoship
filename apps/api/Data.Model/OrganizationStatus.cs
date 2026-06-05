

namespace NeoShip.Data.Model;

public readonly struct OrganizationStatus
{
    private OrganizationStatus(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public ushort Id { get; init; }

    public string Name { get; init; }

    public static OrganizationStatus Active => new(1, "active");

    public static OrganizationStatus Suspended => new(10, "suspended");

    public static OrganizationStatus PendingDeleted => new(40, "pending_deleted");

    public static OrganizationStatus Deleted => new(40, "deleted");

    public static implicit operator ushort(OrganizationStatus status) => status.Id;

    public static implicit operator OrganizationStatus(ushort id) => id switch
    {
        1 => Active,
        10 => Suspended,
        40 => PendingDeleted,
        50 => Deleted,
        _ => throw new InvalidOperationException($"Invalid organization status id: {id}")
    };

    public override string ToString() => this.Name;
}