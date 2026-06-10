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

    /// <summary>
    /// Verifies core permissions expose role editor metadata.
    /// </summary>
    [Fact]
    public void CorePermissions_IncludeRoleEditorPermissions()
    {
        var registry = new PermissionRegistry(CorePermissions.All);

        Assert.True(registry.TryGet(PermissionKey.Create("org.roles", "read"), out var read));
        Assert.True(registry.TryGet(PermissionKey.Create("org.roles", "write"), out var write));
        Assert.Equal("Read roles", read!.Description);
        Assert.Equal("Create and update roles", write!.Description);
        Assert.Contains(PermissionScopeKind.Organization, read.AllowedScopes);
        Assert.Contains(PermissionScopeKind.Organization, write.AllowedScopes);
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