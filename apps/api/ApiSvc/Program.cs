using Microsoft.EntityFrameworkCore;
using NeoShip.ApiSvc.Endpoints;
using NeoShip.ApiSvc.Services;
using NeoShip.Data.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ShipDb>(options =>
    options.UseSqlite("Data Source=neoship.db")
           .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShipDb>();
    db.Database.Migrate();
}

app.MapGet("/", () => "NeoShip Identity API");

app.MapAuthEndpoints();
app.MapMeEndpoints();

app.MapDefaultEndpoints();

app.Run();
