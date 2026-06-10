using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class UserApiKeyClaim
{
    public int Id { get; set; } = 0;

    public Guid UserApiKeyId { get; set; } = Guid.Empty;

    public UserApiKey? UserApiKey { get; set; }

    [StringLength(128)]
    public string Type { get; set; } = string.Empty;

    [StringLength(1024)]
    public string Value { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}