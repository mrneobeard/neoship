using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using NeoShip.Data.Model;

namespace NeoShip.Data.Mssql;

/// <summary>
/// SQL Server-specific Ship database context.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = new MssqlShipDb(options);
/// </code>
/// </remarks>
public sealed class MssqlShipDb : ShipDb
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MssqlShipDb"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public MssqlShipDb(DbContextOptions<ShipDb> options)
        : base(options)
    {
    }
}

/// <summary>
/// Creates SQL Server Ship database contexts for EF Core tooling.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var db = factory.CreateDbContext(args);
/// </code>
/// </remarks>
public sealed class MssqlShipDbFactory : IDesignTimeDbContextFactory<ShipDb>
{
    /// <summary>
    /// Creates a SQL Server-backed Ship database context.
    /// </summary>
    /// <param name="args">The design-time arguments.</param>
    /// <returns>A <see cref="ShipDb"/> configured for SQL Server migrations.</returns>
    public ShipDb CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ShipDb>()
            .UseSqlServer("Server=localhost;Database=neoship;User Id=sa;Password=Password123!;TrustServerCertificate=true",
                b => b.MigrationsAssembly("NeoShip.Data.Mssql"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ShipDb(options);
    }
}