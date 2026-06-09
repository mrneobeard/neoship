using System.Runtime.Versioning;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// Provides access to the Linux secret service using libsecret.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var vault = new LinuxOsSecretVault();
/// vault.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public class LinuxOsSecretVault : IKeyring
{
    /// <summary>
    /// Gets a secret from the Linux secret service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// var secret = vault.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public string? GetSecret(string service, string account)
        => LibSecret.GetSecret(service, account);

    /// <summary>
    /// Gets a secret from the Linux secret service as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// var secretBytes = vault.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public byte[] GetSecretAsBytes(string service, string account)
        => LibSecret.GetSecretAsBytes(service, account);

    /// <summary>
    /// Sets a secret in the Linux secret service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public void SetSecret(string service, string account, string secret)
        => LibSecret.SetSecret(service, account, secret);

    /// <summary>
    /// Sets a secret in the Linux secret service as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", new byte[] { 1, 2, 3 });
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public void SetSecret(string service, string account, byte[] secret)
        => LibSecret.SetSecret(service, account, secret);

    /// <summary>
    /// Deletes a secret from the Linux secret service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// vault.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public void DeleteSecret(string service, string account)
        => LibSecret.DeleteSecret(service, account);

    /// <summary>
    /// Lists all secrets for a given service in the Linux secret service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of secret records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new LinuxOsSecretVault();
    /// var secrets = vault.ListSecrets("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public IReadOnlyList<KeyringRecord> ListSecrets(string service)
    {
        var records = LibSecret.ListSecrets(service);
        var result = new List<KeyringRecord>(records.Count);
        foreach (var record in records)
        {
            result.Add(new KeyringRecord(service, record.Account, record.Secret));
        }

        return result;
    }
}