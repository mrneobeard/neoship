namespace NeoShip.Data.Model;

public class Role
{
    public Guid Id { get; set; } = Guid.Empty;

    public string Name { get; set; } = string.Empty;

    public string NameUpcase { get; set; } = string.Empty;

    public string? Description { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public HashSet<User> Users { get; set; } = new();

    public HashSet<Group> Groups { get; set; } = new();

    public HashSet<RoleClaim> Claims { get; set; } = new();
}
