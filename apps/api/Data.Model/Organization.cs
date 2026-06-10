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

    /// <summary>
    /// Gets or sets the organization MFA policy identifier.
    /// </summary>
    /// <value>The organization MFA policy identifier.</value>
    public ushort MfaPolicyId { get; set; } = OrganizationMfaPolicy.Off.Id;

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

    /// <summary>
    /// Gets or sets the organization MFA policy.
    /// </summary>
    /// <value>The organization MFA policy.</value>
    [NotMapped]
    public OrganizationMfaPolicy MfaPolicy
    {
        get => this.MfaPolicyId;
        set => this.MfaPolicyId = value;
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    /// <summary>
    /// Gets or sets the UTC timestamp when this organization was soft-deleted.
    /// </summary>
    /// <value>The UTC soft-deletion timestamp.</value>
    public DateTime? DeletedAt { get; set; } = null;

    /// <summary>
    /// Gets or sets the UTC timestamp when this organization becomes eligible for hard deletion.
    /// </summary>
    /// <value>The UTC hard-deletion eligibility timestamp.</value>
    public DateTime? HardDeleteAt { get; set; } = null;

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

/// <summary>
/// Represents organization MFA enforcement modes.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// org.MfaPolicy = OrganizationMfaPolicy.AllMembers;
/// </code>
/// </remarks>
public readonly struct OrganizationMfaPolicy
{
    /// <summary>
    /// Gets the policy identifier.
    /// </summary>
    /// <value>The policy identifier.</value>
    public ushort Id { get; init; }

    /// <summary>
    /// Gets the policy name.
    /// </summary>
    /// <value>The policy name.</value>
    public string Name { get; init; }

    private OrganizationMfaPolicy(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    /// <summary>
    /// Gets the disabled MFA policy.
    /// </summary>
    /// <value>The disabled MFA policy.</value>
    public static OrganizationMfaPolicy Off => new(0, "off");

    /// <summary>
    /// Gets the admin and owner MFA policy.
    /// </summary>
    /// <value>The admin and owner MFA policy.</value>
    public static OrganizationMfaPolicy AdminsAndOwners => new(10, "admins_owners");

    /// <summary>
    /// Gets the all-members MFA policy.
    /// </summary>
    /// <value>The all-members MFA policy.</value>
    public static OrganizationMfaPolicy AllMembers => new(20, "all_members");

    /// <summary>
    /// Gets the unknown MFA policy.
    /// </summary>
    /// <value>The unknown MFA policy.</value>
    public static OrganizationMfaPolicy Unknown => new(ushort.MaxValue, "unknown");

    /// <summary>
    /// Converts an MFA policy to its name.
    /// </summary>
    /// <param name="policy">The policy.</param>
    /// <returns>The policy name.</returns>
    public static implicit operator string(OrganizationMfaPolicy policy) => policy.Name;

    /// <summary>
    /// Converts an MFA policy to its identifier.
    /// </summary>
    /// <param name="policy">The policy.</param>
    /// <returns>The policy identifier.</returns>
    public static implicit operator ushort(OrganizationMfaPolicy policy) => policy.Id;

    /// <summary>
    /// Converts an identifier to an MFA policy.
    /// </summary>
    /// <param name="id">The policy identifier.</param>
    /// <returns>The matching policy, or <see cref="Unknown"/>.</returns>
    public static implicit operator OrganizationMfaPolicy(ushort id) => id switch
    {
        0 => Off,
        10 => AdminsAndOwners,
        20 => AllMembers,
        _ => Unknown,
    };
}