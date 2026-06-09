using System.Diagnostics;

using Fido2NetLib;

using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("app", OTelConstants.ServiceName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/iam-.log", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(sp => new Fido2(new Fido2Configuration
{
    ServerDomain = sp.GetRequiredService<IConfiguration>()["Auth:Passkeys:ServerDomain"] ?? "localhost",
    ServerName = sp.GetRequiredService<IConfiguration>()["Auth:Passkeys:ServerName"] ?? "NeoShip",
    Origins = sp.GetRequiredService<IConfiguration>().GetSection("Auth:Passkeys:Origins").Get<HashSet<string>>()
        ?? ["https://localhost", "http://localhost"],
}, metadataService: null));

builder.Services.AddDbContext<ShipDb>(options =>
    options.UseSqlite("Data Source=neoship.db",
        b => b.MigrationsAssembly("NeoShip.Data.Sqlite"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<RequestContext>();
builder.Services.AddSingleton(new PermissionRegistry(CorePermissions.All));
builder.Services.AddSingleton<PermissionClaimCodec>(sp => new PermissionClaimCodec(sp.GetRequiredService<PermissionRegistry>()));
builder.Services.AddSingleton<PermissionSnapshotCodec>();
builder.Services.AddScoped<PermissionResolver>();

builder.Services.AddSingleton<PasswordStore>();
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddSingleton<TokenExchangeStore>();

builder.Services.AddScoped<SessionStore>();
builder.Services.AddScoped<AuthStore>();
builder.Services.AddScoped<AuditStore>();
builder.Services.AddScoped<ApiKeyStore>();
builder.Services.AddScoped<ServiceAccountStore>();
builder.Services.AddScoped<RoleStore>();
builder.Services.AddScoped<GroupStore>();
builder.Services.AddScoped<OrganizationStore>();
builder.Services.AddSingleton<IdentityProviderSecretProtector>();
builder.Services.AddScoped<IdentityProviderStore>();
builder.Services.AddScoped<MfaStore>();
builder.Services.AddScoped<PasskeyStore>();
builder.Services.AddSingleton<PasskeyChallengeStore>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseMiddleware<NeoShip.ApiSvc.Middleware.TlsRequiredMiddleware>();
app.UseMiddleware<NeoShip.ApiSvc.Middleware.RequestContextMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipDb>();
    db.Database.Migrate();

    var defaultOrgId = Constants.DefaultOrganizationId;
    if (!db.Orgs.Any(o => o.Id == defaultOrgId))
    {
        db.Orgs.Add(new Organization
        {
            Id = defaultOrgId,
            Name = "Default",
            NameUpcase = "DEFAULT",
            Slug = "default",
            StatusId = 1,
            TenantModeId = 0,
            OrganizationPlanId = 1,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();
    }
}

app.MapGet("/", () => "NeoShip Identity API");

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapTenantEndpoints();
app.MapOrgEndpoints();

app.MapDefaultEndpoints();

app.Run();