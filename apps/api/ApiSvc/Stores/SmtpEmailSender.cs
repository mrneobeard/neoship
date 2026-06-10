using System.Net;
using System.Net.Mail;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Sends transactional email through SMTP.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// await sender.SendAsync(message, ct);
/// </code>
/// </remarks>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpEmailSender"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public SmtpEmailSender(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var host = this.configuration["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host is required for SMTP delivery.");
        var from = this.configuration["Email:From"] ?? "no-reply@localhost";
        var port = this.configuration.GetValue("Email:Smtp:Port", 25);
        var enableSsl = this.configuration.GetValue("Email:Smtp:EnableSsl", true);
        var username = this.configuration["Email:Smtp:Username"];
        var password = this.configuration["Email:Smtp:Password"];

        using var mail = new MailMessage(from, message.To, message.Subject, message.Body);
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password ?? string.Empty);
        }

        await client.SendMailAsync(mail, ct);
    }
}