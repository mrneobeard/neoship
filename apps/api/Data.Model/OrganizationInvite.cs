using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents an email invitation to join an organization.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var invite = new OrganizationInvite { OrgId = orgId, Email = "person@example.com" };
/// </code>
/// </remarks>
public sealed class OrganizationInvite
{
    /// <summary>
    /// Gets or sets the invite identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the organization identifier.
    /// </summary>
    public Guid OrgId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the organization navigation.
    /// </summary>
    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    /// <summary>
    /// Gets or sets the invited email address.
    /// </summary>
    [StringLength(256)]
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized invited email address.
    /// </summary>
    [StringLength(256)]
    [Required]
    public string EmailUpcase { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invite token digest.
    /// </summary>
    [StringLength(256)]
    [Required]
    public string TokenDigest { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the inviter user identifier.
    /// </summary>
    public Guid InvitedByUserId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the inviter user navigation.
    /// </summary>
    [ForeignKey(nameof(InvitedByUserId))]
    public User? InvitedByUser { get; set; }

    /// <summary>
    /// Gets or sets pending role assignment identifiers as JSON.
    /// </summary>
    [StringLength(4096)]
    public string? PendingRoleIdsJson { get; set; }

    /// <summary>
    /// Gets or sets pending group assignment identifiers as JSON.
    /// </summary>
    [StringLength(4096)]
    public string? PendingGroupIdsJson { get; set; }

    /// <summary>
    /// Gets or sets the invite creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the invite expiration timestamp.
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>
    /// Gets or sets the invite acceptance timestamp.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the accepting user identifier.
    /// </summary>
    public Guid? AcceptedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the accepting user navigation.
    /// </summary>
    [ForeignKey(nameof(AcceptedByUserId))]
    public User? AcceptedByUser { get; set; }

    /// <summary>
    /// Gets or sets the invite revocation timestamp.
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}