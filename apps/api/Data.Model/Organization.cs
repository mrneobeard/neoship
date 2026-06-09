using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

[Table("orgs")]
public class Organization
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(128)]
    public string NameUpcase { get; set; } = string.Empty;

    [StringLength(128)]
    public string Slug { get; set; } = string.Empty;

    public ushort OrganizationPlanId { get; set; } = 1;

    public ushort TenantModeId { get; set; }

    public ushort StatusId { get; set; } = 1;

    [NotMapped]
    public bool IsPrimaryTenant => this.Id.Equals(Guid.Empty);

    [NotMapped]
    public OrganizationStatus Status
    {
        get => this.StatusId;
        set => this.StatusId = value;
    }

    [NotMapped]
    public TenantMode TenantMode
    {
        get => this.TenantModeId;
        set => this.TenantModeId = value;
    }

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether password sign-in is allowed.
    /// </summary>
    public bool AllowPasswordAuth { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether passkey sign-in is allowed.
    /// </summary>
    public bool AllowPasskeyAuth { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether OIDC SSO sign-in is allowed.
    /// </summary>
    public bool AllowOidcSso { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SAML SSO sign-in is allowed.
    /// </summary>
    public bool AllowSamlSso { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SSO is required for the organization.
    /// </summary>
    public bool RequireSso { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether users may unlink external identities themselves.
    /// </summary>
    public bool AllowSelfServiceExternalIdentityUnlink { get; set; } = true;
}
