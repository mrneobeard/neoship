using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc;

/// <summary>
/// Applies scheduled deletion retention policy for soft-deleted IAM records.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// builder.Services.AddHostedService&lt;DeleteRetentionWorker&gt;();
/// </code>
/// </remarks>
internal sealed class DeleteRetentionWorker : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<DeleteRetentionWorker> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteRetentionWorker"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory used to create database scopes.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The worker logger.</param>
    public DeleteRetentionWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DeleteRetentionWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Executes the retention loop until the host shuts down.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token that signals host shutdown.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous worker lifetime.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.configuration.GetValue("Auth:Deletion:HardDeleteEnabled", false))
        {
            this.logger.LogInformation("Deletion hard-delete worker is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, this.configuration.GetValue("Auth:Deletion:PurgeIntervalMinutes", 60)));
        using var timer = new PeriodicTimer(interval);

        await this.PurgeAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await this.PurgeAsync(stoppingToken);
        }
    }

    private async Task PurgeAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await using var scope = this.scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShipDb>();

        var orgsDeleted = await db.Orgs
            .Where(x => x.StatusId == OrganizationStatus.PendingDeleted.Id && x.HardDeleteAt != null && x.HardDeleteAt <= now)
            .ExecuteDeleteAsync(ct);

        var usersDeleted = await db.Users
            .Where(x => x.StatusId == UserStatus.Deleted.Id && x.HardDeleteAt != null && x.HardDeleteAt <= now)
            .ExecuteDeleteAsync(ct);

        if (orgsDeleted > 0 || usersDeleted > 0)
        {
            this.logger.LogInformation("Hard-deleted {OrgCount} organizations and {UserCount} users.", orgsDeleted, usersDeleted);
        }
    }
}