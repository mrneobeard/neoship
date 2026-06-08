using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NeoShip.Data.Model;

namespace NeoShip.Data.Sqlite;

public class ShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlite("Data Source=:memory:", b => b.MigrationsAssembly("NeoShip.Data.Sqlite"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}
