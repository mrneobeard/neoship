using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

internal static class TestUserStore
{
    public static UserStore Create(ShipDb db, RequestContext? ctx = null)
    {
        ctx ??= new RequestContext { IpAddress = "127.0.0.1", UserAgent = "tests" };
        var sessions = new SessionStore(db, ctx, new PermissionSnapshotCodec(), NullLogger<SessionStore>.Instance);
        var audit = new AuditStore(db, ctx, NullLogger<AuditStore>.Instance);
        var permissions = new PermissionResolver(db, new PermissionClaimCodec(new PermissionRegistry(CorePermissions.All)));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:IdentityProviders:EncryptionKey"] = "test-identity-provider-secret-key",
            ["Email:PublicBaseUrl"] = "https://localhost",
        }).Build();

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var secrets = new IdentityProviderSecretProtector(configuration);
        return new UserStore(db, TestFido2.Create(), memoryCache, secrets, sessions, audit, permissions, new TestEmailSender(), configuration, ctx, NullLogger<UserStore>.Instance);
    }
}