using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents a reusable authorization role.
/// </summary>
public class Role
{
    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the organization identifier for the role.
    /// </summary>
    public Guid OrgId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the organization navigation.
    /// </summary>
    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the uppercased role name.
    /// </summary>
    [StringLength(128)]
    public string NameUpcase { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    [StringLength(512)]
    public string? Description { get; set; } = null;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the creator user identifier.
    /// </summary>
    public Guid CreatedBy { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the creator navigation.
    /// </summary>
    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// Gets or sets the users attached to the role.
    /// </summary>
    public HashSet<User> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the groups attached to the role.
    /// </summary>
    public HashSet<Group> Groups { get; set; } = new();

    /// <summary>
    /// Gets or sets the role claims.
    /// </summary>
    public HashSet<RoleClaim> Claims { get; set; } = new();
}
