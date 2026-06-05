namespace NeoShip.Data.Model;

public class ServiceAccountClaim
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid ServiceAccountId { get; set; } = Guid.Empty;

    public ServiceAccount? ServiceAccount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}