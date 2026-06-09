using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class UserIdentityProvider
{
    public long Id { get; set; }

    public Guid UserId { get; set; }

    public Guid OrgId { get; set; }

    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public ushort ProviderTypeId { get; set; }

    [NotMapped]
    public UserIdentityProviderType ProviderType
    {
        get => this.ProviderTypeId;
        set => ProviderTypeId = value;
    }

    public ushort StatusId { get; set; } = UserIdentityProviderStatus.Active.Id;

    [NotMapped]
    public UserIdentityProviderStatus Status
    {
        get => this.StatusId;
        set => this.StatusId = value;
    }

    [StringLength(2048)]
    public string? IssuerUrl { get; set; }

    [StringLength(256)]
    public string? ClientId { get; set; }

    [Column("client_secret_enc")]

    public byte[] ClientSecretEncrypted { get; set; } = [];

    [StringLength(4096)]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}

public readonly struct UserIdentityProviderStatus
{
    internal UserIdentityProviderStatus(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public bool IsActive => this.Id < 100;

    public ushort Id { get; init; }

    public string Name { get; init; }

    public static UserIdentityProviderStatus Unknown => new(0, "unknown");

    public static UserIdentityProviderStatus Active => new(1, "active");

    public static UserIdentityProviderStatus Inactive => new(2, "inactive");

    public static implicit operator UserIdentityProviderStatus(ushort id) => id switch
    {
        1 => Active,
        2 => Inactive,
        _ => Unknown
    };

    public static implicit operator ushort(UserIdentityProviderStatus status) => status.Id;

    public static implicit operator string(UserIdentityProviderStatus status) => status.Name;
}

public readonly struct UserIdentityProviderType
{
    internal UserIdentityProviderType(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public ushort Id { get; init; }

    public string Name { get; init; }

    public static UserIdentityProviderType Unknown => new(0, "Unknown");

    public static UserIdentityProviderType OIDC => new(1, "OIDC");

    public static UserIdentityProviderType SAML => new(2, "SAML");

    public static UserIdentityProviderType OAUTH2 => new(3, "OAUTH2");

    public static readonly UserIdentityProviderType[] All = new[]
    {
        OIDC,
        SAML,
        OAUTH2,
    };

    public static implicit operator UserIdentityProviderType(ushort id)
    {
        switch (id)
        {
            case 1: return OIDC;
            case 2: return SAML;
            case 3: return OAUTH2;
            default: return Unknown;
        }
    }

    public static implicit operator ushort(UserIdentityProviderType type) => type.Id;

    public static implicit operator string(UserIdentityProviderType type) => type.Name;
}
