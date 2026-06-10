using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents a user's accepted membership in an organization.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var membership = new OrganizationMembership { OrgId = orgId, UserId = userId };
/// </code>
/// </remarks>
public sealed class OrganizationMembership
{
    /// <summary>
    /// Gets or sets the membership identifier.
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
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the user navigation.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the membership creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the accepted timestamp.
    /// </summary>
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}