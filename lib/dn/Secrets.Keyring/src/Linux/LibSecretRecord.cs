namespace NeoBeard.Secrets.Linux;

/// <summary>
/// Represents a secret record from the Linux secret service.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var record = new LibSecretRecord("MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public readonly struct LibSecretRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibSecretRecord"/> struct.
    /// </summary>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var record = new LibSecretRecord("MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public LibSecretRecord(string? account, string? secret)
    {
        this.Account = account ?? string.Empty;
        this.Secret = secret ?? string.Empty;
    }

    /// <summary>
    /// Gets the account name.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var account = record.Account;
    /// </code>
    /// </example>
    /// </remarks>
    public string Account { get; }

    /// <summary>
    /// Gets the secret value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = record.Secret;
    /// </code>
    /// </example>
    /// </remarks>
    public string Secret { get; }
}