
namespace NeoShip.Data.Model;

public class UserPasswordAuth
{
    public Guid UserId { get; set; } = Guid.Empty;

    public User? User { get; set; }

}