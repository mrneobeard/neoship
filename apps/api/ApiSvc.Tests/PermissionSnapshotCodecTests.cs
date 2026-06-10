using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
public class PermissionSnapshotCodecTests
{
    [Fact]
    public void RoundTrip_PreservesGrants()
    {
        var codec = new PermissionSnapshotCodec();
        var set = new PermissionSet(new[]
        {
            new PermissionGrant(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization, "default"),
            new PermissionGrant(PermissionKey.Create("auth.sessions", "revoke"), PermissionScopeKind.Organization, "default"),
        });

        var json = codec.Serialize(set);
        var roundTripped = codec.Deserialize(json);

        Assert.True(roundTripped.Allows(PermissionKey.Create("org.roles", "read"), PermissionScopeKind.Organization, "default"));
        Assert.True(roundTripped.Allows(PermissionKey.Create("auth.sessions", "revoke"), PermissionScopeKind.Organization, "default"));
    }
}