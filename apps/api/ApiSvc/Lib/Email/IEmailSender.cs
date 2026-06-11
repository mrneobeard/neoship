namespace NeoShip.ApiSvc.Lib.Email;

/// <summary>
/// Sends transactional email messages.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// await sender.SendAsync(message, ct);
/// </code>
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email message.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
