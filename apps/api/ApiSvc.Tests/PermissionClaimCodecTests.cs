using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
public class PermissionClaimCodecTests
{
    [Fact]
    public void Encode_ProducesCompactClaimFields()
    {
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));
        var grant = new PermissionGrant(
            PermissionKey.Create("org.roles", "write"),
            PermissionScopeKind.Organization,
            "org_1");

        var (type, value) = codec.Encode(grant);

        Assert.Equal("org.roles.write", type);
        Assert.Equal("organization:org_1", value);
    }

    [Fact]
    public void Decode_KnownPermissionClaim_ReturnsGrant()
    {
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));

        var success = codec.TryDecode("org.groups.read", "organization:org_1", out var grant);

        Assert.True(success);
        Assert.Equal(PermissionKey.Create("org.groups", "read"), grant.Key);
        Assert.Equal(PermissionScopeKind.Organization, grant.ScopeKind);
        Assert.Equal("org_1", grant.ScopeId);
    }

    [Fact]
    public void Decode_UnknownPermissionClaim_ReturnsFalse()
    {
        var codec = new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All));

        var success = codec.TryDecode("custom.feature.read", "global", out _);

        Assert.False(success);
    }
}