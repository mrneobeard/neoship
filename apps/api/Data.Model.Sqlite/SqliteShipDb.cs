using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NeoShip.Data.Model;

namespace NeoShip.Data.Sqlite;

/// <summary>
/// SQLite-specific Ship database context.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = new SqliteShipDb(options);
/// </code>
/// </remarks>
public sealed class SqliteShipDb : ShipDb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteShipDb"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public SqliteShipDb(DbContextOptions<ShipDb> options)
        : base(options)
    {
    }
}

/// <summary>
/// Creates SQLite Ship database contexts for EF Core tooling.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = factory.CreateDbContext(args);
/// </code>
/// </remarks>
public sealed class SqliteShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    /// <summary>
    /// Creates a SQLite-backed Ship database context.
    /// </summary>
    /// <param name="args">The design-time arguments.</param>
    /// <returns>A <see cref="ShipDb"/> configured for SQLite migrations.</returns>
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlite("Data Source=neoship.db", b => b.MigrationsAssembly("NeoShip.Data.Sqlite"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}