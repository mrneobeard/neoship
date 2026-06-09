namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Provides access to the Windows Credential Manager.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var vault = new WinOsSecretVault();
/// vault.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public class WinOsSecretVault : IKeyring
{
    /// <summary>
    /// Gets a secret from the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// var secret = vault.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public string? GetSecret(string service, string account)
        => WinCredManager.GetSecret(service, account);

    /// <summary>
    /// Gets a secret from the Windows Credential Manager as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// var secretBytes = vault.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] GetSecretAsBytes(string service, string account)
        => WinCredManager.GetSecretAsBytes(service, account);

    /// <summary>
    /// Sets a secret in the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public void SetSecret(string service, string account, string secret)
        => WinCredManager.SetSecret(service, account, secret);

    /// <summary>
    /// Sets a secret in the Windows Credential Manager as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// vault.SetSecret("MyService", "MyAccount", new byte[] { 1, 2, 3 });
    /// </code>
    /// </example>
    /// </remarks>
    public void SetSecret(string service, string account, byte[] secret)
        => WinCredManager.SetSecret(service, account, secret);

    /// <summary>
    /// Deletes a secret from the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// vault.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public void DeleteSecret(string service, string account)
        => WinCredManager.DeleteSecret(service, account);

    /// <summary>
    /// Lists all secrets for a given service in the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of secret records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = new WinOsSecretVault();
    /// var secrets = vault.ListSecrets("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    public IReadOnlyList<KeyringRecord> ListSecrets(string service)
    {
        var credentials = WinCredManager.EnumerateCredentials();
        var result = new List<KeyringRecord>();

        foreach (var cred in credentials)
        {
            if (cred.Service == service || cred.Service.StartsWith(service + "/"))
            {
                var account = cred.Account ?? ExtractAccountFromTarget(cred.Service, service);
                result.Add(new KeyringRecord(service, account, cred.Password));
            }
        }

        return result;
    }

    private static string ExtractAccountFromTarget(string targetName, string service)
    {
        if (targetName.StartsWith(service + "/"))
            return targetName.Substring(service.Length + 1);

        return targetName;
    }
}