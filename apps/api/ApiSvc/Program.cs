using Microsoft.EntityFrameworkCore;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Services;
using NeoShip.Data.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ShipDb>(options =>
    options.UseSqlite("Data Source=neoship.db",
        b => b.MigrationsAssembly("NeoShip.Data.Sqlite"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<TokenExchangeService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<ServiceAccountService>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseMiddleware<NeoShip.ApiSvc.Middleware.TlsRequiredMiddleware>();

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
app.MapOrgEndpoints();

app.MapDefaultEndpoints();

app.Run();
