using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents a scoped role assignment.
/// </summary>
/// <example>
/// <code>
/// var assignment = new RoleAssignment
/// {
///     OrgId = orgId,
///     UserId = userId,
///     RoleKey = "owner",
///     ScopeKind = PermissionScopeKind.Organization,
///     ScopeId = "default",
/// };
/// </code>
/// </example>
public class RoleAssignment
{
    /// <summary>
    /// Gets or sets the assignment identifier.
    /// </summary>
    /// <value>The assignment identifier.</value>
    public Guid Id { get; set; } = Factory.NewGuid();

    /// <summary>
    /// Gets or sets the organization identifier that owns the assignment.
    /// </summary>
    /// <value>The organization identifier.</value>
    public Guid OrgId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the organization navigation.
    /// </summary>
    /// <value>The organization navigation.</value>
    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    /// <summary>
    /// Gets or sets the assigned user identifier.
    /// </summary>
    /// <value>The assigned user identifier, when the assignment targets a user.</value>
    public Guid? UserId { get; set; } = null;

    /// <summary>
    /// Gets or sets the assigned user navigation.
    /// </summary>
    /// <value>The assigned user navigation.</value>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the assigned group identifier.
    /// </summary>
    /// <value>The assigned group identifier, when the assignment targets a group.</value>
    public Guid? GroupId { get; set; } = null;

    /// <summary>
    /// Gets or sets the assigned group navigation.
    /// </summary>
    /// <value>The assigned group navigation.</value>
    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    /// <summary>
    /// Gets or sets the role key.
    /// </summary>
    /// <value>The role key.</value>
    [StringLength(128)]
    public string RoleKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assignment scope kind.
    /// </summary>
    /// <value>The assignment scope kind.</value>
    public PermissionScopeKind ScopeKind { get; set; } = PermissionScopeKind.Unknown;

    /// <summary>
    /// Gets or sets the optional scope identifier.
    /// </summary>
    /// <value>The optional scope identifier.</value>
    [StringLength(256)]
    public string? ScopeId { get; set; } = null;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the creator user identifier.
    /// </summary>
    /// <value>The creator user identifier.</value>
    public Guid CreatedBy { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the creator navigation.
    /// </summary>
    /// <value>The creator navigation.</value>
    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedByUser { get; set; }
}
