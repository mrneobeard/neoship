namespace NeoShip.Data.Model;

public class ServiceAccountApiKeyClaim
{
    public ulong Id { get; set; } = 0;

    public Guid ServiceAccountApiKeyId { get; set; } = Guid.Empty;

    public ServiceAccountApiKey? ServiceAccountApiKey { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}