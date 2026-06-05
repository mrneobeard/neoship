using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

/// <summary>
/// Represents an email address record associated with a user. This allows tracking
/// email addresses over time for the purposes of auditing and historical reference. 
/// 
/// The user may ask to be forgotten, and we will delete the e-mail, but can still search
/// by the hash, but only if someone knows which e-mail address to search for. 
/// </summary>
public class UserEmail
{
    public Guid Id { get; set; } = Guid.Empty;

    public Guid UserId { get; set; } = Guid.Empty;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [StringLength(512)]
    [Required]
    public string EmailDigest { get; set; } = string.Empty;

    [StringLength(256)]
    [Required]
    public string EmailUpcase { get; set; } = string.Empty;

    [StringLength(256)]
    [Required]
    public string Email { get; set; } = string.Empty;

    public Guid CreatedBy { get; set; } = Guid.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? VerifiedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? ErasedAt { get; set; }

    public ushort StatusId { get; set; } = UserEmailStatus.Active.Id;

    [NotMapped]
    public UserEmailStatus Status { get; set; } = UserEmailStatus.Active;
}

public readonly struct UserEmailStatus
{
    public ushort Id { get; init; }

    public string Name { get; init; }

    public UserEmailStatus(ushort id, string name)
    {
        this.Id = id;
        this.Name = string.Intern(name);
    }

    public static UserEmailStatus Active => new(1, "current");

    public static UserEmailStatus Previous => new(10, "previous");

    /// <summary>
    /// Pending verification. This status is used when a user is changing their email
    /// address and we need to verify the new email before making it active.
    /// </summary>
    public static UserEmailStatus Pending => new(20, "pending"); 


    public static UserEmailStatus Deleted => new(100, "deleted");


    public static implicit operator ushort(UserEmailStatus status) => status.Id;

    public static implicit operator UserEmailStatus(ushort id) => id switch
    {
        1 => Active,
        10 => Previous,
        20 => Pending,
        100 => Deleted,
        _ => throw new InvalidOperationException($"Invalid user email status id: {id}")
    };

    public static implicit operator string(UserEmailStatus status) => status.Name;
}