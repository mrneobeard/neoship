namespace NeoShip.Data.Model;

public class UserApiKeyClaim
{
    public uint Id { get; set; } = 0;

    public Guid UserApiKeyId { get; set; } = Guid.Empty;

    public UserApiKey? UserApiKey { get; set; }
    
    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}