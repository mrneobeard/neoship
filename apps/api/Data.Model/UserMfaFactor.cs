
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class UserMfaFactor
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid UserId { get; set; } = Guid.Empty;

    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the name which is the device label.
    /// </summary>
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public ushort Type { get; set; } = 1;

    [Column("value_enc")]
    public byte[] ValueEncrypted { get; set; } = [];

    public byte[] WebAuthnPublicKeyCredentialData { get; set; } = [];

    public byte[] WebAuthnCredentialId { get; set; } = [];

    /// <summary>
    /// Gets or sets the digest used to look up the WebAuthn credential identifier.
    /// </summary>
    [StringLength(128)]
    public string? WebAuthnCredentialIdDigest { get; set; }

    /// <summary>
    /// Gets or sets the WebAuthn signature counter from the authenticator.
    /// </summary>
    public uint WebAuthnSignCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the last time this MFA factor was successfully used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [StringLength(1024)]
    public string? TransportsJson { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;
}

public readonly struct MfaFactorType
{
    public ushort Id { get; init; }

    public string Name { get; init; }

    private MfaFactorType(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public static MfaFactorType Passkey => new(1, "passkey");

    public static MfaFactorType Totp => new(2, "totp");

    public static MfaFactorType RecoverCode => new(3, "recover_code");

    public static MfaFactorType WebAuthnSecurityKey => new(4, "webauthn_security_key");

    public static MfaFactorType Email => new(5, "email");

    public static MfaFactorType Sms => new(6, "sms");

    public static MfaFactorType Unknown => new(ushort.MaxValue, "unknown");

    public static implicit operator string(MfaFactorType type) => type.Name;

    public static implicit operator ushort(MfaFactorType type) => type.Id;

    public static implicit operator MfaFactorType(ushort id) => id switch
    {
        1 => Passkey,
        2 => Totp,
        3 => RecoverCode,
        4 => WebAuthnSecurityKey,
        5 => Email,
        6 => Sms,
        _ => Unknown
    };
}



