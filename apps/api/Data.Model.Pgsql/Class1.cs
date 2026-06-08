using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NeoShip.Data.Model;

namespace NeoShip.Data.Pgsql;

public class ShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseNpgsql("Host=localhost;Database=neoship",
                b => b.MigrationsAssembly("NeoShip.Data.Pgsql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}
