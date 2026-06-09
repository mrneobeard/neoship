namespace NeoBeard.Secrets;

/// <summary>
/// Defines a vault for storing and retrieving secrets from the operating system.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// IOsSecretVault vault = OsSecrets.Vault; // Example of obtaining a vault
/// vault.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public interface IKeyring
{
    /// <summary>
    /// Gets a secret from the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = vault.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    string? GetSecret(string service, string account);

    /// <summary>
    /// Gets a secret from the vault as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretBytes = vault.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    byte[] GetSecretAsBytes(string service, string account);

    /// <summary>
    /// Sets a secret in the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// vault.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    void SetSecret(string service, string account, string secret);

    /// <summary>
    /// Sets a secret in the vault as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// vault.SetSecret("MyService", "MyAccount", Encoding.UTF8.GetBytes("MySecret"));
    /// </code>
    /// </example>
    /// </remarks>
    void SetSecret(string service, string account, byte[] secret);

    /// <summary>
    /// Deletes a secret from the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// vault.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    void DeleteSecret(string service, string account);

    /// <summary>
    /// Lists all secrets for a given service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of secret records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secrets = vault.ListSecrets("MyService");
    /// foreach (var record in secrets)
    /// {
    ///     Console.WriteLine(record.Account);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    IReadOnlyList<KeyringRecord> ListSecrets(string service);
}