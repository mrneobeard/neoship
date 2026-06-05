namespace NeoShip.Data.Model;

public class UserClaim
{
    public ulong Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
