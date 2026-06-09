namespace NeoBeard.Secrets.Darwin;

/// <summary>
/// Provides access to the macOS Keychain.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var vault = new DarwinOsSecretVault();
/// vault.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public class DarwinOsSecretVault : IKeyring
{
    /// <summary>
    /// Gets a secret from the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// var secret = vault.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public string? GetSecret(string service, string account)
        => KeyChain.GetSecret(service, account);

    /// <summary>
    /// Gets a secret from the Keychain as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// var secretBytes = vault.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] GetSecretAsBytes(string service, string account)
        => KeyChain.GetSecretAsBytes(service, account);

    /// <summary>
    /// Sets a secret in the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public void SetSecret(string service, string account, string secret)
        => KeyChain.SetSecret(service, account, secret);

    /// <summary>
    /// Sets a secret in the Keychain as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", Encoding.UTF8.GetBytes("MySecret"));
    /// </code>
    /// </example>
    /// </remarks>
    public void SetSecret(string service, string account, byte[] secret)
        => KeyChain.SetSecret(service, account, secret);

    /// <summary>
    /// Deletes a secret from the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// vault.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public void DeleteSecret(string service, string account)
        => KeyChain.DeleteSecret(service, account);

    /// <summary>
    /// Lists all secrets for a given service in the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of secret records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new DarwinOsSecretVault();
    /// var secrets = vault.ListSecrets("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<KeyringRecord> ListSecrets(string service)
        => KeyChain.ListSecrets(service);
}