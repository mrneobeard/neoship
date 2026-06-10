using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return 0;
}

if (args[0] != "bootstrap-admin")
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    PrintUsage();
    return 2;
}

var options = ParseOptions(args.Skip(1).ToArray());
var email = Required(options, "email");
var password = Required(options, "password");
var name = options.GetValueOrDefault("name") ?? email;
var orgSlug = options.GetValueOrDefault("org") ?? "default";
var orgName = options.GetValueOrDefault("org-name") ?? "Default";
var dbPath = options.GetValueOrDefault("db") ?? "neoship.db";

if (!IsValidEmail(email))
{
    Console.Error.WriteLine("Email must be valid.");
    return 2;
}

if (password.Length is < 12 or > 256)
{
    Console.Error.WriteLine("Password must be 12 to 256 characters.");
    return 2;
}

await using var db = CreateDatabase(dbPath);
await db.Database.MigrateAsync();

var org = await db.Orgs.FirstOrDefaultAsync(x => x.Slug == orgSlug);
if (org is null)
{
    org = new Organization
    {
        Id = orgSlug == "default" ? Constants.DefaultOrganizationId : Guid.CreateVersion7(),
        Name = orgName.Trim(),
        NameUpcase = orgName.Trim().ToUpperInvariant(),
        Slug = OrganizationStore.NormalizeSlug(orgSlug),
        StatusId = OrganizationStatus.Active.Id,
        TenantModeId = TenantMode.Multi.Id,
        OrganizationPlanId = 1,
        CreatedAt = DateTime.UtcNow,
    };
    db.Orgs.Add(org);
}

var emailUpcase = email.ToUpperInvariant();
var user = await db.Users.FirstOrDefaultAsync(x => x.EmailUpcase == emailUpcase);
if (user is null)
{
    user = new User(Guid.CreateVersion7(), email, name)
    {
        OrgId = org.Id,
        StatusId = UserStatus.Active.Id,
    };
    db.Users.Add(user);
    db.UserEmails.Add(new UserEmail
    {
        Id = Guid.CreateVersion7(),
        UserId = user.Id,
        Email = email,
        EmailUpcase = emailUpcase,
        EmailDigest = TokenStore.ComputeDigestBase64(email),
        StatusId = UserEmailStatus.Active.Id,
        CreatedBy = user.Id,
        CreatedAt = DateTime.UtcNow,
        VerifiedAt = DateTime.UtcNow,
    });
}

user.Name = name.Trim();
user.NameUpcase = user.Name.ToUpperInvariant();
user.OrgId = org.Id;
user.StatusId = UserStatus.Active.Id;

var passwordStore = new PasswordStore();
var passwordAuth = await db.UserPasswordAuths.FirstOrDefaultAsync(x => x.UserId == user.Id);
if (passwordAuth is null)
{
    db.UserPasswordAuths.Add(new UserPasswordAuth
    {
        UserId = user.Id,
        PasswordHash = passwordStore.Hash(password),
        CreatedAt = DateTime.UtcNow,
    });
}
else
{
    passwordAuth.PasswordHash = passwordStore.Hash(password);
    passwordAuth.PasswordChangedAt = DateTime.UtcNow;
    passwordAuth.FailedAttempts = 0;
    passwordAuth.LockedUntil = null;
}

var membership = await db.OrganizationMemberships.FirstOrDefaultAsync(x => x.OrgId == org.Id && x.UserId == user.Id);
if (membership is null)
{
    db.OrganizationMemberships.Add(new OrganizationMembership
    {
        OrgId = org.Id,
        UserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        AcceptedAt = DateTime.UtcNow,
    });
}
else
{
    membership.DeletedAt = null;
    membership.AcceptedAt = membership.AcceptedAt == default ? DateTime.UtcNow : membership.AcceptedAt;
}

foreach (var permission in BootstrapPermissions())
{
    if (!await db.UserClaims.AnyAsync(x => x.UserId == user.Id && x.Type == permission && x.Value == $"organization:{org.Slug}"))
    {
        db.UserClaims.Add(new UserClaim
        {
            UserId = user.Id,
            Type = permission,
            Value = $"organization:{org.Slug}",
        });
    }
}

await db.SaveChangesAsync();
Console.WriteLine($"Bootstrapped admin user {email} for organization {org.Slug}.");
return 0;

static ShipDb CreateDatabase(string dbPath)
{
    var options = new DbContextOptionsBuilder<ShipDb>()
        .UseSqlite($"Data Source={dbPath}", sqlite => sqlite.MigrationsAssembly("NeoShip.Data.Sqlite"))
        .UseSnakeCaseNamingConvention()
        .Options;
    return new ShipDb(options);
}

static Dictionary<string, string?> ParseOptions(string[] values)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < values.Length; i++)
    {
        var value = values[i];
        if (!value.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = value[2..];
        result[key] = i + 1 < values.Length && !values[i + 1].StartsWith("--", StringComparison.Ordinal) ? values[++i] : null;
    }

    return result;
}

static string Required(Dictionary<string, string?> options, string key)
{
    if (!options.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
    {
        Console.Error.WriteLine($"Missing required option --{key}.");
        Environment.Exit(2);
    }

    return value!;
}

static bool IsValidEmail(string email)
{
    if (email.Length is < 3 or > 320)
    {
        return false;
    }

    var at = email.IndexOf('@', StringComparison.Ordinal);
    return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && email[(at + 1)..].Contains('.', StringComparison.Ordinal);
}

static IReadOnlyList<string> BootstrapPermissions()
{
    return
    [
        "org.settings.read",
        "org.settings.write",
        "org.members.read",
        "org.members.write",
        "org.roles.read",
        "org.roles.write",
        "org.groups.read",
        "org.groups.write",
        "org.service_accounts.read",
        "org.service_accounts.write",
        "org.identity_providers.read",
        "org.identity_providers.write",
    ];
}

static void PrintUsage()
{
    Console.WriteLine("NeoShip Admin CLI");
    Console.WriteLine("Usage:");
    Console.WriteLine("  bootstrap-admin --email <email> --password <password> [--name <name>] [--org default] [--org-name Default] [--db neoship.db]");
}
