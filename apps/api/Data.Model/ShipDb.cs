using Microsoft.EntityFrameworkCore;

namespace NeoShip.Data.Model;

public class ShipDb : DbContext
{
    public ShipDb(DbContextOptions<ShipDb> options) 
        : base(options)
    {
    }

    public DbSet<Organization> Orgs => Set<Organization>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();

    public DbSet<ServiceAccountApiKey> ServiceAccountApiKeys => Set<ServiceAccountApiKey>();

    public DbSet<ServiceAccountApiKeyClaim> ServiceAccountApiKeyClaims => Set<ServiceAccountApiKeyClaim>();

    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();

    public DbSet<UserApiKeyClaim> UserApiKeyClaims => Set<UserApiKeyClaim>();

    public DbSet<UserKnownNetwork> UserKnownNetworks => Set<UserKnownNetwork>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
}
