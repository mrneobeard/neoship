namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Logs transactional email metadata without logging secret message bodies.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// await sender.SendAsync(message, ct);
/// </code>
/// </remarks>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingEmailSender"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        this.logger.LogInformation("Email accepted for logging delivery: to={To} subject={Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}
