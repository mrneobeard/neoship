using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NeoShip;
using NeoShip.Data.Model;
using NeoShip.Data.Runtime;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddShipData(builder.Configuration);

using var host = builder.Build();
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<ShipDb>();

await db.Database.MigrateAsync();
if (!await db.Orgs.AnyAsync(o => o.Id == Constants.DefaultOrganizationId))
{
    db.Orgs.Add(new Organization
    {
        Id = Constants.DefaultOrganizationId,
        Name = "Default",
        NameUpcase = "DEFAULT",
        Slug = "default",
        StatusId = OrganizationStatus.Active.Id,
        TenantModeId = TenantMode.None.Id,
        OrganizationPlanId = 1,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    });
    await db.SaveChangesAsync();
}

Console.WriteLine("Database migration completed.");