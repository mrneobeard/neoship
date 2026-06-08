using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using NeoBeard.Secrets.Darwin;
using NeoBeard.Secrets.Linux;
using NeoBeard.Secrets.Win32;

namespace NeoBeard.Secrets;

/// <summary>
/// Provides access to the operating system's secret vault.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// OsSecrets.SetSecret("MyService", "MyAccount", "MySecret");
/// var secret = OsSecrets.GetSecret("MyService", "MyAccount");
/// </code>
/// </example>
/// </remarks>
public static class Keyring
{
    private static readonly Lazy<bool> s_isOsSupported = new(() =>
    {
        // on .net core, there are more platforms supported like freebsd, browser, wasm, wasm, mobile.
        // not all of them are supported or have been tested yet.
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
               || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
               || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    });

    private static readonly Lazy<IKeyring> s_vault = new(() =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WinOsSecretVault();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new DarwinOsSecretVault();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new LinuxOsSecretVault();

        throw new PlatformNotSupportedException("Only Windows, MacOs, and Linux are currently supported");
    });

    /// <summary>
    /// Gets a value indicating whether the current operating system is supported.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// if (OsSecrets.IsOsSupported)
    /// {
    ///     // Access secrets
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public static bool IsOsSupported => s_isOsSupported.Value;

    /// <summary>
    /// Gets the operating system's secret vault.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var vault = OsSecrets.Vault;
    /// </code>
    /// </example>
    /// </remarks>
    public static IKeyring Vault => s_vault.Value;

    /// <summary>
    /// Gets a secret from the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = OsSecrets.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static string? GetSecret(string service, string account)
        => s_vault.Value.GetSecret(service, account);

    /// <summary>
    /// Gets a secret from the vault as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretBytes = OsSecrets.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] GetSecretAsBytes(string service, string account)
        => s_vault.Value.GetSecretAsBytes(service, account);

    /// <summary>
    /// Gets a secret from the vault as a character array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a character array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretChars = OsSecrets.GetSecretAsChars("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static char[] GetSecretAsChars(string service, string account)
    {
        var bytes = s_vault.Value.GetSecretAsBytes(service, account);
        var chars = Encoding.UTF8.GetChars(bytes);
        Array.Clear(bytes, 0, bytes.Length);
        return chars;
    }

    /// <summary>
    /// Gets a secret from the vault as a <see cref="SecureString"/>.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a <see cref="SecureString"/>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var secureSecret = OsSecrets.GetSecretAsSecureString("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static unsafe SecureString GetSecretAsSecureString(string service, string account)
    {
        var bytes = s_vault.Value.GetSecretAsBytes(service, account);
        var utf8Chars = Encoding.UTF8.GetChars(bytes);
        try
        {
            fixed (char* chars = utf8Chars)
            {
                var ss = new SecureString(chars, utf8Chars.Length);
                return ss;
            }
        }
        finally
        {
            Array.Clear(utf8Chars, 0, utf8Chars.Length);
        }
    }

    /// <summary>
    /// Sets a secret in the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// OsSecrets.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(string service, string account, string secret)
        => s_vault.Value.SetSecret(service, account, secret);

    /// <summary>
    /// Sets a secret in the vault as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// OsSecrets.SetSecret("MyService", "MyAccount", Encoding.UTF8.GetBytes("MySecret"));
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(string service, string account, byte[] secret)
        => s_vault.Value.SetSecret(service, account, secret);

    /// <summary>
    /// Deletes a secret from the vault.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// OsSecrets.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static void DeleteSecret(string service, string account)
            => s_vault.Value.DeleteSecret(service, account);

    /// <summary>
    /// Lists all secrets for a given service.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of secret records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secrets = OsSecrets.ListSecrets("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<KeyringRecord> ListSecrets(string service)
        => s_vault.Value.ListSecrets(service);
}