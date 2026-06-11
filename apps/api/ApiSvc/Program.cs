using System.Diagnostics;
using System.Threading.RateLimiting;

using Fido2NetLib;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;
using NeoShip.Data.Runtime;

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
var cacheBackend = builder.Configuration["Cache:Backend"] ?? builder.Configuration["Auth:Cache:Backend"] ?? "memory";
if (string.Equals(cacheBackend, "redis", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration["Cache:Redis:ConnectionString"]
        ?? builder.Configuration["Auth:Cache:Redis:ConnectionString"]
        ?? builder.Configuration.GetConnectionString("redis");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Redis cache backend requires Cache:Redis:ConnectionString or ConnectionStrings:redis.");
    }

    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorEnvelope(
                new ApiError("rate_limited", "Too many requests."),
                ApiMeta.FromHttpContext(context.HttpContext)),
            ct);
    };

    options.AddPolicy(AuthEndpoints.LoginRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));

    options.AddPolicy(AuthEndpoints.SensitiveRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(httpContext),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});
builder.Services.AddSingleton(sp => new Fido2(new Fido2Configuration
{
    ServerDomain = sp.GetRequiredService<IConfiguration>()["Auth:Passkeys:ServerDomain"] ?? "localhost",
    ServerName = sp.GetRequiredService<IConfiguration>()["Auth:Passkeys:ServerName"] ?? "NeoShip",
    Origins = sp.GetRequiredService<IConfiguration>().GetSection("Auth:Passkeys:Origins").Get<HashSet<string>>()
        ?? ["https://localhost", "http://localhost"],
}, metadataService: null));

builder.Services.AddShipData(builder.Configuration);

builder.Services.AddScoped<RequestContext>();
builder.Services.AddSingleton(new PermissionRegistry(CorePermissions.All));
builder.Services.AddSingleton<PermissionClaimCodec>(sp => new PermissionClaimCodec(sp.GetRequiredService<PermissionRegistry>()));
builder.Services.AddSingleton<PermissionSnapshotCodec>();
builder.Services.AddScoped<PermissionResolver>();

builder.Services.AddSingleton<PasswordStore>();
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddSingleton<TokenExchangeStore>();
builder.Services.AddSingleton<IEmailSender>(sp =>
{
    var provider = sp.GetRequiredService<IConfiguration>()["Email:Provider"] ?? "logging";
    return provider.Trim().ToLowerInvariant() switch
    {
        "smtp" => ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp),
        "test" => ActivatorUtilities.CreateInstance<TestEmailSender>(sp),
        _ => ActivatorUtilities.CreateInstance<LoggingEmailSender>(sp),
    };
});

builder.Services.AddScoped<SessionStore>();
builder.Services.AddScoped<AuthStore>();
builder.Services.AddScoped<AuditStore>();
builder.Services.AddScoped<ApiKeyStore>();
builder.Services.AddScoped<ServiceAccountStore>();
builder.Services.AddScoped<RoleStore>();
builder.Services.AddScoped<GroupStore>();
builder.Services.AddScoped<OrganizationStore>();
builder.Services.AddScoped<OrganizationInviteStore>();
builder.Services.AddSingleton<IdentityProviderSecretProtector>();
builder.Services.AddScoped<IdentityProviderStore>();
builder.Services.AddScoped<MfaStore>();
builder.Services.AddScoped<PasskeyStore>();
builder.Services.AddSingleton<PasskeyChallengeStore>();
builder.Services.AddSingleton<SsoChallengeStore>();
builder.Services.AddHttpClient<ISsoTokenClient, SsoTokenClient>();
builder.Services.AddHttpClient<ISsoTokenValidator, SsoTokenValidator>();
builder.Services.AddHttpClient<ISsoOAuth2ProfileClient, SsoOAuth2ProfileClient>();
builder.Services.AddScoped<SsoStore>();
builder.Services.AddHostedService<DeleteRetentionWorker>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseMiddleware<NeoShip.ApiSvc.Middleware.TlsRequiredMiddleware>();
app.UseMiddleware<NeoShip.ApiSvc.Middleware.RequestContextMiddleware>();
app.UseRateLimiter();

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
app.MapAdminEndpoints();
app.MapMeEndpoints();
app.MapTenantEndpoints();
app.MapOrgEndpoints();
app.MapRoleEndpoints();
app.MapGroupEndpoints();
app.MapServiceAccountEndpoints();

app.MapDefaultEndpoints();

app.Run();

static string PartitionKey(HttpContext httpContext)
{
    var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "unknown";
    }

    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
