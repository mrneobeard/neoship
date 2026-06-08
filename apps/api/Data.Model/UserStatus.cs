
namespace NeoShip.Data.Model;

public readonly struct UserStatus
{
    internal UserStatus(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public ushort Id { get; init; }

    public string Name { get; init; }

    public static implicit operator ushort(UserStatus status) => status.Id;

    public static implicit operator UserStatus(ushort id) => id switch
    {
        0 => None,
        1 => Active,
        90 => Invited,
        100 => Inactive,
        200 => Suspended,
        900 => Deleted,
        _ => Unknown
    };

    public static implicit operator string(UserStatus status) => status.Name;

    public static UserStatus None => new(0, "none");

    public static UserStatus Active => new(1, "active");

    public static UserStatus Invited => new(90, "invited");

    public static UserStatus Inactive => new(100, "inactive");

    public static UserStatus Suspended => new(200, "suspended");

    public static UserStatus Deleted => new(900, "deleted");

    public static UserStatus Unknown => new(ushort.MaxValue, "unknown");
}