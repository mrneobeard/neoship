using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NeoShip.Data.Model;

namespace NeoShip.Data.Runtime;

/// <summary>
/// Registers Ship database services for the configured database provider.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// builder.Services.AddShipData(builder.Configuration);
/// </code>
/// </remarks>
public static class ShipDataServiceCollectionExtensions
{
    /// <summary>
    /// Adds the configured <see cref="ShipDb"/> provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddShipData(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "sqlite").Trim().ToLowerInvariant();
        var connectionString = configuration["Database:ConnectionString"] ?? configuration.GetConnectionString("Default");

        services.AddDbContext<ShipDb>(options => Configure(options, provider, connectionString));
        return services;
    }

    /// <summary>
    /// Configures database context options for the selected provider.
    /// </summary>
    /// <param name="options">The database context options builder.</param>
    /// <param name="provider">The provider name.</param>
    /// <param name="connectionString">The provider connection string.</param>
    /// <returns>The configured <see cref="DbContextOptionsBuilder"/>.</returns>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string provider, string? connectionString)
    {
        switch (provider)
        {
            case "sqlite":
                options.UseSqlite(connectionString ?? "Data Source=neoship.db",
                    b => b.MigrationsAssembly("NeoShip.Data.Sqlite"));
                break;
            case "pgsql":
            case "postgres":
            case "postgresql":
                options.UseNpgsql(connectionString ?? throw MissingConnectionString(provider),
                    b => b.MigrationsAssembly("NeoShip.Data.Pgsql"));
                break;
            case "mssql":
            case "sqlserver":
                options.UseSqlServer(connectionString ?? throw MissingConnectionString(provider),
                    b => b.MigrationsAssembly("NeoShip.Data.Mssql"));
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{provider}'. Use sqlite, pgsql, or mssql.");
        }

        options.UseSnakeCaseNamingConvention();
        return options;
    }

    private static InvalidOperationException MissingConnectionString(string provider)
    {
        return new InvalidOperationException($"Database provider '{provider}' requires Database:ConnectionString or ConnectionStrings:Default.");
    }
}