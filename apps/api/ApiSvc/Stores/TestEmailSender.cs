using System.Collections.Concurrent;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Captures email messages in memory for tests.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var messages = sender.Messages;
/// </code>
/// </remarks>
public sealed class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> messages = new();

    /// <summary>
    /// Gets captured email messages.
    /// </summary>
    /// <value>The captured messages.</value>
    public IReadOnlyList<EmailMessage> Messages => this.messages.ToArray();

    /// <inheritdoc />
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        this.messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
