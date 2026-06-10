namespace NeoShip.Data.Model;

public readonly struct TenantMode
{
    internal TenantMode(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public ushort Id { get; init; }

    public string Name { get; init; }

    public static implicit operator ushort(TenantMode mode) => mode.Id;

    public static implicit operator TenantMode(ushort id) => id switch
    {
        0 => None,
        1 => Single,
        2 => Multi,
        _ => Unknown
    };

    public static implicit operator string(TenantMode mode) => mode.Name;

    public static TenantMode None => new(0, "none");

    public static TenantMode Single => new(1, "single");

    public static TenantMode Multi => new(2, "multi");

    public static TenantMode Unknown => new(ushort.MaxValue, "unknown");
}