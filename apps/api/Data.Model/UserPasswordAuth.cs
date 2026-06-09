using System.ComponentModel.DataAnnotations;

namespace NeoShip.Data.Model;

public class UserPasswordAuth
{
    public Guid UserId { get; set; } = Guid.Empty;

    public User? User { get; set; }

    [StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(128)]
    public string PasswordSalt { get; set; } = string.Empty;

    [StringLength(32)]
    public string HashAlgorithm { get; set; } = "argon2id";

    public int Iterations { get; set; } = 4;

    public int FailedAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; } = null;

    [StringLength(256)]
    public string? ResetTokenDigest { get; set; } = null;

    public DateTime? ResetTokenExpiresAt { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = null;

    public DateTime? PasswordChangedAt { get; set; } = null;
}
