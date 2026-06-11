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

    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    public DbSet<OrganizationInvite> OrganizationInvites => Set<OrganizationInvite>();

    public DbSet<UserEmail> UserEmails => Set<UserEmail>();

    public DbSet<UserPasswordAuth> UserPasswordAuths => Set<UserPasswordAuth>();

    public DbSet<UserMfaFactor> UserMfaFactors => Set<UserMfaFactor>();

    public DbSet<UserIdentityProvider> UserIdentityProviders => Set<UserIdentityProvider>();

    public DbSet<UserExternalIdentity> UserExternalIdentities => Set<UserExternalIdentity>();

    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();

    public DbSet<UserApiKeyClaim> UserApiKeyClaims => Set<UserApiKeyClaim>();

    public DbSet<UserKnownNetwork> UserKnownNetworks => Set<UserKnownNetwork>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

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
            u.HasIndex(x => x.HardDeleteAt);
        });

        modelBuilder.Entity<AuditEvent>(a =>
        {
            a.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<UserApiKeyClaim>(c =>
        {
            c.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<UserClaim>(c =>
        {
            c.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<RoleClaim>(c =>
        {
            c.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<RoleAssignment>(a =>
        {
            a.HasIndex(x => new { x.OrgId, x.UserId, x.RoleKey, x.ScopeKind, x.ScopeId });
            a.HasIndex(x => new { x.OrgId, x.GroupId, x.RoleKey, x.ScopeKind, x.ScopeId });
            a.HasIndex(x => new { x.UserId, x.OrgId });
            a.HasIndex(x => new { x.GroupId, x.OrgId });
        });

        modelBuilder.Entity<ServiceAccountApiKeyClaim>(c =>
        {
            c.Property(x => x.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Organization>(o =>
        {
            o.HasIndex(x => x.HardDeleteAt);
        });

        modelBuilder.Entity<OrganizationMembership>(m =>
        {
            m.HasIndex(x => new { x.OrgId, x.UserId }).IsUnique();
            m.HasIndex(x => new { x.UserId, x.DeletedAt });
        });

        modelBuilder.Entity<OrganizationInvite>(i =>
        {
            i.HasIndex(x => x.TokenDigest).IsUnique();
            i.HasIndex(x => new { x.OrgId, x.EmailUpcase });
        });

        modelBuilder.Entity<UserEmail>(e =>
        {
            e.HasIndex(x => x.EmailDigest);
            e.HasIndex(x => x.VerificationTokenDigest);
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

        modelBuilder.Entity<UserExternalIdentity>(p =>
        {
            p.HasIndex(x => new { x.OrgId, x.ProviderId, x.SubjectDigest }).IsUnique();
            p.HasIndex(x => new { x.UserId, x.ProviderId });
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
            p.HasIndex(x => x.ResetTokenDigest);
        });

        // User -> UserEmail
        modelBuilder.Entity<UserEmail>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // UserClaim: FK auto-detected by convention (UserId)

        modelBuilder.Entity<OrganizationMembership>(m =>
        {
            m.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId).OnDelete(DeleteBehavior.Restrict);
            m.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationInvite>(i =>
        {
            i.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId).OnDelete(DeleteBehavior.Restrict);
            i.HasOne(x => x.InvitedByUser).WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
            i.HasOne(x => x.AcceptedByUser).WithMany().HasForeignKey(x => x.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // User -> UserMfaFactor
        modelBuilder.Entity<UserMfaFactor>(f =>
        {
            f.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            f.HasIndex(x => x.WebAuthnCredentialIdDigest);
        });

        // User -> UserIdentityProvider
        modelBuilder.Entity<UserIdentityProvider>(p =>
        {
            p.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<UserExternalIdentity>(p =>
        {
            p.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId).OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.Provider).WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Role>(r =>
        {
            r.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId);
            r.HasIndex(x => new { x.OrgId, x.NameUpcase }).IsUnique();
            r.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy);
        });

        modelBuilder.Entity<RoleAssignment>(a =>
        {
            a.HasOne(x => x.Org).WithMany().HasForeignKey(x => x.OrgId).OnDelete(DeleteBehavior.Restrict);
            a.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            a.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Restrict);
            a.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // Role -> RoleClaim
        modelBuilder.Entity<RoleClaim>(c =>
        {
            c.HasOne(x => x.Role).WithMany(r => r.Claims).HasForeignKey(x => x.RoleId);
            c.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy);
        });

        // Group <-> Role (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Roles)
            .WithMany(r => r.Groups);

        // Group <-> User Members (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Members)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_members",
                r => r.HasOne<User>().WithMany().OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<Group>().WithMany().OnDelete(DeleteBehavior.Restrict));

        // Group <-> User Owners (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.Owners)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_owners",
                r => r.HasOne<User>().WithMany().OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<Group>().WithMany().OnDelete(DeleteBehavior.Restrict));

        // Group <-> ServiceAccount Members (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.ServiceAccountMembers)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_service_account_members",
                r => r.HasOne<ServiceAccount>().WithMany().OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<Group>().WithMany().OnDelete(DeleteBehavior.Restrict));

        // Group <-> ServiceAccount Owners (M:N)
        modelBuilder.Entity<Group>()
            .HasMany(g => g.ServiceAccountOwners)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_service_account_owners",
                r => r.HasOne<ServiceAccount>().WithMany().OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<Group>().WithMany().OnDelete(DeleteBehavior.Restrict));

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
