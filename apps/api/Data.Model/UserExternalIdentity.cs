using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Links a local user to an external identity provider subject.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var link = new UserExternalIdentity { UserId = userId, ProviderId = providerId, Subject = subject };
/// </code>
/// </remarks>
public class UserExternalIdentity
{
    /// <summary>
    /// Gets or sets the link identifier.
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
    /// Gets or sets the local user identifier.
    /// </summary>
    public Guid UserId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the local user navigation.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the identity provider configuration identifier.
    /// </summary>
    public long ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the identity provider navigation.
    /// </summary>
    [ForeignKey(nameof(ProviderId))]
    public UserIdentityProvider? Provider { get; set; }

    /// <summary>
    /// Gets or sets the provider subject identifier.
    /// </summary>
    [StringLength(512)]
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the lookup digest for the exact provider subject identifier.
    /// </summary>
    [StringLength(512)]
    [Required]
    public string SubjectDigest { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last verified email received from the provider.
    /// </summary>
    [StringLength(256)]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last-used timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
