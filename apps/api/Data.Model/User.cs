using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NeoShip.Data.Model;

public class User
{
    public User()
    {
        this.Id = Factory.NewGuid();
    }

    public User(Guid id, string email)
    {
        this.Id = id;
        this.Email = email;
        this.EmailUpcase = email.ToUpperInvariant();
        this.Name = email;
        this.NameUpcase = this.EmailUpcase;
    }

    public User(Guid id, string email, string name)
    {
        this.Id = id;
        this.Email = email;
        this.EmailUpcase = email.ToUpperInvariant();
        this.Name = name;
        this.NameUpcase = name.ToUpperInvariant();
    }

    public Guid Id { get; set; } = Guid.CreateVersion7();

    [StringLength(256)]
    [Required]
    public string Email { get; set; } = string.Empty;

    [StringLength(256)]
    [Required]
    public string EmailUpcase { get; set; } = string.Empty;


    /// <summary>
    /// The user's username or display name. This is not necessarily unique, but should be treated as a human-friendly
    /// identifier for the user. If not initially provided, it will default to the user's email address. 
    /// It is recommended to allow users to change this value
    /// </summary>
    [StringLength(256)]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    [StringLength(256)]
    [Required]
    public string NameUpcase { get; set; } = string.Empty;

    public ushort StatusId { get; set; } = UserStatus.Active.Id;

    [NotMapped]
    public UserStatus Status
    {
        get => this.StatusId;
        set => this.StatusId = value;
    }

    [StringLength(1024)]
    public string? AvatarUrl { get; set; } = null;


    [StringLength(39)]
    public string? LastLoginIp { get; set; } = null;

    public DateTime? LastLoginAt { get; set; } = null;

    /// <summary>
    /// Gets or sets the UTC timestamp when this user was soft-deleted.
    /// </summary>
    /// <value>The UTC soft-deletion timestamp.</value>
    public DateTime? DeletedAt { get; set; } = null;

    /// <summary>
    /// Gets or sets the UTC timestamp when this user becomes eligible for hard deletion.
    /// </summary>
    /// <value>The UTC hard-deletion eligibility timestamp.</value>
    public DateTime? HardDeleteAt { get; set; } = null;

    [ForeignKey(nameof(OrgId))]
    public Organization? Org { get; set; }

    public Guid OrgId { get; set; } = Guid.Empty;

    public HashSet<Role> Roles { get; set; } = new();
}