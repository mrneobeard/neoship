namespace NeoShip.Data.Model;

public class Constants
{
    public const string ApiVersion = "v1";

    public static Guid DefaultOrganizationId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static Guid DefaultUserId => Guid.Parse("00000000-0000-0000-0000-000000000002");
}