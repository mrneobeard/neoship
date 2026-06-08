using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NeoShip.Data.Model;

namespace NeoShip.Data.Mssql;

public class ShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlServer("Server=localhost;Database=neoship;TrustServerCertificate=true",
                b => b.MigrationsAssembly("NeoShip.Data.Mssql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}
