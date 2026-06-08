using System.Runtime.InteropServices;
using System.Text;

namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Provides a high-level API for interacting with the Windows Credential Manager.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// WinCredManager.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public static class WinCredManager
{
    /// <summary>
    /// Sets a secret in the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// WinCredManager.SetSecret("MyService", "MyAccount", new byte[] { 1, 2, 3 });
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(
        string service,
        string account,
        byte[] secret)
    {
        SetSecret(
            service,
            account,
            secret,
            false,
            (string?)null,
            WinCredPersistence.Enterprise);
    }

    /// <summary>
    /// Sets a secret in the Windows Credential Manager with advanced options.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value as a byte array.</param>
    /// <param name="serviceAsKey">Whether to use only the service name as the credential target name.</param>
    /// <param name="comment">An optional comment for the credential.</param>
    /// <param name="persistence">The persistence level of the credential.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// WinCredManager.SetSecret("MyService", "MyAccount", new byte[] { 1, 2, 3 }, false, "My comment", WinCredPersistence.LocalMachine);
    /// </code>
    /// </example>
    /// </remarks>
    [CLSCompliant(false)]
    public static void SetSecret(
        string service,
        string account,
        byte[] secret,
        bool serviceAsKey,
        string? comment,
        WinCredPersistence persistence)
    {
        var targetName = serviceAsKey ? service : $"{service}/{account}";
        IntPtr data = Marshal.AllocCoTaskMem(secret.Length);
        try
        {
            if (secret.Length > 0)
            {
                Marshal.Copy(secret, 0, data, secret.Length);
            }

            var nativeCredential = new NativeCredential
            {
                AttributeCount = 0,
                Type = WinCredType.Generic,
                TargetName = targetName,
                CredentialBlob = data,
                CredentialBlobSize = secret.Length,
                Persist = persistence,
                UserName = account,
            };

            var isSet = NativeMethods.WriteCredential(
                ref nativeCredential,
                0);

            int errorCode = Marshal.GetLastWin32Error();
            if (isSet)
                return;

            throw new InvalidOperationException($"WriteCredential failed with error code {errorCode}");
        }
        finally
        {
            if (data != IntPtr.Zero)
                Marshal.FreeCoTaskMem(data);
        }
    }

    /// <summary>
    /// Sets a secret in the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value string.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// WinCredManager.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(
        string service,
        string account,
        string secret)
    {
        SetSecret(
            service,
            account,
            secret,
            false,
            null,
            WinCredPersistence.Enterprise);
    }

    /// <summary>
    /// Sets a secret in the Windows Credential Manager with advanced options.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="secret">The secret value string.</param>
    /// <param name="serviceAsKey">Whether to use only the service name as the credential target name.</param>
    /// <param name="comment">An optional comment for the credential.</param>
    /// <param name="persistence">The persistence level of the credential.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// WinCredManager.SetSecret("MyService", "MyAccount", "MySecret", false, "My comment", WinCredPersistence.LocalMachine);
    /// </code>
    /// </example>
    /// </remarks>
    [CLSCompliant(false)]
    public static void SetSecret(
        string service,
        string account,
        string secret,
        bool serviceAsKey,
        string? comment,
        WinCredPersistence persistence)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        SetSecret(service, account, bytes, serviceAsKey, comment, persistence);
    }

    /// <summary>
    /// Gets a secret from the Windows Credential Manager as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretBytes = WinCredManager.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] GetSecretAsBytes(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isRead)
            throw new InvalidOperationException($"ReadCredential failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.GetSecretAsBytes();
    }

    /// <summary>
    /// Gets a secret from the Windows Credential Manager as a string.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a string, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = WinCredManager.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static string? GetSecret(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        if (!isRead)
            return null;

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.GetSecret();
    }

    /// <summary>
    /// Gets a full credential record from the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The <see cref="WinCredSecret"/> record.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var credential = WinCredManager.GetCredential("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static WinCredSecret GetCredential(string service, string account)
    {
        var targetName = $"{service}/{account}";
        var isRead = NativeMethods.ReadCredential(targetName, WinCredType.Generic, 0, out var credentialPtr);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isRead)
            throw new InvalidOperationException($"ReadCredential failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtr);
        return credentialHandle.AllocateCredential();
    }

    /// <summary>
    /// Deletes a secret from the Windows Credential Manager.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="serviceAsKey">Whether the service name was used as the full target name.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// WinCredManager.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static void DeleteSecret(string service, string account, bool serviceAsKey = false)
    {
        var targetName = serviceAsKey ? service : $"{service}/{account}";
        var isDeleted = NativeMethods.DeleteCredential(targetName, WinCredType.Generic, 0);
        int errorCode = Marshal.GetLastWin32Error();
        if (isDeleted)
            return;

        throw new InvalidOperationException($"DeleteCredential failed with error code {errorCode}");
    }

    /// <summary>
    /// Enumerates all generic credentials from the Windows Credential Manager.
    /// </summary>
    /// <returns>An array of <see cref="WinCredSecret"/> records.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var credentials = WinCredManager.EnumerateCredentials();
    /// </code>
    /// </example>
    /// </remarks>
    public static WinCredSecret[] EnumerateCredentials()
    {
        if (Environment.OSVersion.Version.Major <= 6)
        {
            string message = "Retrieving all credentials is only possible on Windows version Vista or later.";
            throw new NotSupportedException(message);
        }

        var isEnumerated = NativeMethods.EnumerateCredentials(null, 0, out var count, out var credentialPtrs);
        int errorCode = Marshal.GetLastWin32Error();
        if (!isEnumerated)
            throw new InvalidOperationException($"EnumerateCredentials failed with error code {errorCode}");

        using var credentialHandle = new CredentialHandle(credentialPtrs);
        return credentialHandle.AllocateCredentials(count);
    }
}