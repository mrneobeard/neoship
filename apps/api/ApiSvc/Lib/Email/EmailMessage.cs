namespace NeoShip.ApiSvc.Lib.Email;

/// <summary>
/// Represents an email message to deliver.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var message = new EmailMessage("user@example.com", "Reset", "Use this link...");
/// </code>
/// </remarks>
/// <param name="To">The recipient email address.</param>
/// <param name="Subject">The email subject.</param>
/// <param name="Body">The plain text email body.</param>
public sealed record EmailMessage(string To, string Subject, string Body);
