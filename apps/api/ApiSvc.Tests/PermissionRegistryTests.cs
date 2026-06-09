using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
public class PermissionRegistryTests
{
    [Fact]
    public void Parse_PermissionKey_SplitsResourceAndAction()
    {
        var key = PermissionKey.Parse("org.roles.read");

        Assert.Equal("org.roles", key.Resource);
        Assert.Equal("read", key.Action);
        Assert.Equal("org.roles.read", key.ToString());
    }

    [Fact]
    public void Registry_KnownPermission_IsReturned()
    {
        var registry = new PermissionRegistry(CorePermissions.All);

        var found = registry[PermissionKey.Create("org.roles", "write")];

        Assert.Equal("Create and update roles", found.Description);
        Assert.True(found.Allows(PermissionScopeKind.Organization));
    }

    [Fact]
    public void Registry_DuplicateRegistration_Throws()
    {
        var registry = new PermissionRegistry();
        var definition = new PermissionDefinition(
            PermissionKey.Create("org.groups", "read"),
            "Read groups",
            PermissionScopeKind.Organization);

        registry.Register(definition);

        Assert.Throws<InvalidOperationException>(() => registry.Register(definition));
    }
}
