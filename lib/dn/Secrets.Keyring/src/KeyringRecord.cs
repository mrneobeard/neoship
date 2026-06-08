namespace NeoBeard.Secrets;

/// <summary>
/// Represents a secret record stored in the operating system's secret vault.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var record = new OsSecretRecord("MyService", "user@example.com", "secret123");
/// Console.WriteLine(record.Service);
/// </code>
/// </example>
/// </remarks>
public readonly struct KeyringRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyringRecord"/> struct.
    /// </summary>
    /// <param name="service">The name of the service associated with the secret.</param>
    /// <param name="account">The account name associated with the secret.</param>
    /// <param name="secret">The secret value, if available.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var record = new OsSecretRecord("MyService", "user@example.com", "secret123");
    /// </code>
    /// </example>
    /// </remarks>
    public KeyringRecord(string service, string account, string? secret = null)
    {
        this.Service = service ?? string.Empty;
        this.Account = account ?? string.Empty;
        this.Secret = secret;
    }

    /// <summary>
    /// Gets the name of the service associated with the secret.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var record = new OsSecretRecord("MyService", "user@example.com", "secret123");
    /// var service = record.Service;
    /// </code>
    /// </example>
    /// </remarks>
    public string Service { get; }

    /// <summary>
    /// Gets the account name associated with the secret.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var record = new OsSecretRecord("MyService", "user@example.com", "secret123");
    /// var account = record.Account;
    /// </code>
    /// </example>
    /// </remarks>
    public string Account { get; }

    /// <summary>
    /// Gets the secret value, if available.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var record = new OsSecretRecord("MyService", "user@example.com", "secret123");
    /// var secret = record.Secret;
    /// </code>
    /// </example>
    /// </remarks>
    public string? Secret { get; }
}