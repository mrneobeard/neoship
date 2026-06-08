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

    public DbSet<UserClaim> UserClaims => Set<UserClaim>();

    public DbSet<UserEmail> UserEmails => Set<UserEmail>();

    public DbSet<UserPasswordAuth> UserPasswordAuths => Set<UserPasswordAuth>();

    public DbSet<UserMfaFactor> UserMfaFactors => Set<UserMfaFactor>();

    public DbSet<UserIdentityProvider> UserIdentityProviders => Set<UserIdentityProvider>();

    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();

    public DbSet<UserApiKeyClaim> UserApiKeyClaims => Set<UserApiKeyClaim>();

    public DbSet<UserKnownNetwork> UserKnownNetworks => Set<UserKnownNetwork>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();

    public DbSet<ServiceAccountClaim> ServiceAccountClaims => Set<ServiceAccountClaim>();

    public DbSet<ServiceAccountApiKey> ServiceAccountApiKeys => Set<ServiceAccountApiKey>();

    public DbSet<ServiceAccountApiKeyClaim> ServiceAccountApiKeyClaims => Set<ServiceAccountApiKeyClaim>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---- indexes ----

        modelBuilder.Entity<User>(u =>
        {
            u.HasIndex(x => x.Email).IsUnique();
            u.HasIndex(x => x.EmailUpcase);
        });

        modelBuilder.Entity<UserEmail>(e =>
        {
            e.HasIndex(x => x.EmailDigest);
        });

        modelBuilder.Entity<UserSession>(s =>
        {
            s.HasIndex(x => x.TokenDigest);
        });

        modelBuilder.Entity<UserIdentityProvider>(p =>
        {
            p.HasIndex(x => new { x.ProviderTypeId, x.UserId });
            p.HasIndex(x => new { x.ProviderTypeId, x.OrgId });
        });

        modelBuilder.Entity<UserApiKey>(k =>
        {
            k.HasIndex(x => x.KeyDigest);
        });

        modelBuilder.Entity<ServiceAccountApiKey>(k =>
        {
            k.HasIndex(x => x.KeyDigest);
        });

        modelBuilder.Entity<AuditEvent>(a =>
        {
            a.HasIndex(x => x.Timestamp);
            a.HasIndex(x => x.Type);
        });

        // ---- relationships ----

        // UserPasswordAuth: shared primary key with User
        modelBuilder.Entity<UserPasswordAuth>(p =>
        {
            p.HasKey(x => x.UserId);
            p.HasOne(x => x.User).WithOne().HasForeignKey<UserPasswordAuth>(x => x.UserId).IsRequired();
        });

        // User -> UserEmail
        modelBuilder.Entity<UserEmail>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // UserClaim: FK auto-detected by convention (UserId)

        // User -> UserMfaFactor
        modelBuilder.Entity<UserMfaFactor>(f =>
        {
            f.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // User -> UserIdentityProvider
        modelBuilder.Entity<UserIdentityProvider>(p =>
        {
            p.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        // User -> UserSession
        modelBuilder.Entity<UserSession>(s =>
        {
            s.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            s.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId);
        });

        // User -> UserApiKey
        modelBuilder.Entity<UserApiKey>(k =>
        {
            k.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // UserApiKey -> UserApiKeyClaim
        modelBuilder.Entity<UserApiKeyClaim>(c =>
        {
            c.HasOne(x => x.UserApiKey).WithMany().HasForeignKey(x => x.UserApiKeyId);
        });

        // User -> UserKnownNetwork
        modelBuilder.Entity<UserKnownNetwork>(n =>
        {
            n.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        // User <-> Role (M:N)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users);

        // Role -> RoleClaim
        modelBuilder.Entity<RoleClaim>(c =>
        {
            c.HasOne(x => x.Role).WithMany(r => r.Claims).HasForeignKey(x => x.RoleId);
        });

        // Group <-> Role (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Roles)
            .WithMany(r => r.Groups);

        // Group <-> User Members (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Members)
            .WithMany()
            .UsingEntity("group_members");

        // Group <-> User Owners (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Owners)
            .WithMany()
            .UsingEntity("group_owners");

        // Group <-> ServiceAccount Members (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.ServiceAccountMembers)
            .WithMany()
            .UsingEntity("group_service_account_members");

        // Group <-> ServiceAccount Owners (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.ServiceAccountOwners)
            .WithMany()
            .UsingEntity("group_service_account_owners");

        // ServiceAccount -> ServiceAccountClaim
        modelBuilder.Entity<ServiceAccountClaim>(c =>
        {
            c.HasOne(x => x.ServiceAccount).WithMany().HasForeignKey(x => x.ServiceAccountId);
        });

        // ServiceAccount -> ServiceAccountApiKey
        modelBuilder.Entity<ServiceAccountApiKey>(k =>
        {
            k.HasOne(x => x.ServiceAccount).WithMany().HasForeignKey(x => x.ServiceAccountId);
        });

        // ServiceAccountApiKeyClaim: FK auto-detected by convention
        // ServiceAccountApiKey <-> Role (M:N)
        modelBuilder.Entity<ServiceAccountApiKey>()
            .HasMany(k => k.Roles)
            .WithMany()
            .UsingEntity("service_account_api_key_roles");
    }
}
