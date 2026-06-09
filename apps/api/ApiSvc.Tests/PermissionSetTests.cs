using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
public class PermissionSetTests
{
    [Fact]
    public void Allows_GlobalGrant_MatchesAnyScope()
    {
        var set = new PermissionSet(new[]
        {
            new PermissionGrant(PermissionKey.Create("org.roles", "write"), PermissionScopeKind.Global),
        });

        Assert.True(set.Allows(PermissionKey.Create("org.roles", "write"), PermissionScopeKind.Organization, "org_1"));
        Assert.True(set.Allows(PermissionKey.Create("org.roles", "write"), PermissionScopeKind.Resource, "role_1"));
    }

    [Fact]
    public void Allows_ScopedGrant_MatchesExactScope()
    {
        var set = new PermissionSet(new[]
        {
            new PermissionGrant(PermissionKey.Create("org.groups", "read"), PermissionScopeKind.Organization, "org_1"),
        });

        Assert.True(set.Allows(PermissionKey.Create("org.groups", "read"), PermissionScopeKind.Organization, "org_1"));
        Assert.False(set.Allows(PermissionKey.Create("org.groups", "read"), PermissionScopeKind.Organization, "org_2"));
    }

    [Fact]
    public void ForResource_FiltersByResourcePrefix()
    {
        var set = new PermissionSet(new[]
        {
            new PermissionGrant(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization),
            new PermissionGrant(PermissionKey.Create("org.groups", "write"), PermissionScopeKind.Organization),
        });

        var grants = set.ForResource("org.roles").ToList();

        Assert.Single(grants);
        Assert.Equal("read", grants[0].Key.Action);
    }
}
