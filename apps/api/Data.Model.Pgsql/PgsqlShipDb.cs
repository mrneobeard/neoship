using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NeoShip.Data.Model;

namespace NeoShip.Data.Pgsql;

/// <summary>
/// PostgreSQL-specific Ship database context.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = new PgsqlShipDb(options);
/// </code>
/// </remarks>
public sealed class PgsqlShipDb : ShipDb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PgsqlShipDb"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public PgsqlShipDb(DbContextOptions<ShipDb> options)
        : base(options)
    {
    }
}

/// <summary>
/// Creates PostgreSQL Ship database contexts for EF Core tooling.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = factory.CreateDbContext(args);
/// </code>
/// </remarks>
public sealed class PgsqlShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    /// <summary>
    /// Creates a PostgreSQL-backed Ship database context.
    /// </summary>
    /// <param name="args">The design-time arguments.</param>
    /// <returns>A <see cref="ShipDb"/> configured for PostgreSQL migrations.</returns>
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseNpgsql("Host=localhost;Database=neoship;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly("NeoShip.Data.Pgsql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}