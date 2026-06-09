using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using static NeoBeard.Secrets.Darwin.NativeMethods;

namespace NeoBeard.Secrets.Darwin;

/// <summary>
/// Provides a high-level API for interacting with the macOS Keychain.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// KeyChain.SetSecret("MyService", "MyAccount", "MySecret");
/// </code>
/// </example>
/// </remarks>
public static class KeyChain
{
    /// <summary>
    /// Lists all secrets for a given service in the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>A list of <see cref="KeyringRecord"/> instances.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secrets = KeyChain.ListSecrets("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    public static IReadOnlyList<KeyringRecord> ListSecrets(string service)
    {
        var records = new List<KeyringRecord>();
        IntPtr searchRef = IntPtr.Zero;
        IntPtr attrData = IntPtr.Zero;
        IntPtr attrPtr = IntPtr.Zero;
        IntPtr attrListPtr = IntPtr.Zero;

        try
        {
            var attributeList = new SecKeychainAttributeList
            {
                Count = 1,
            };

            attrData = Marshal.StringToHGlobalAnsi(service);
            var attribute = new SecKeychainAttribute
            {
                Tag = SecKeychainAttrType.ServiceItem,
                Length = (uint)service.Length,
                Data = attrData,
            };

            attrPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SecKeychainAttribute>());
            Marshal.StructureToPtr(attribute, attrPtr, false);
            attributeList.Attributes = attrPtr;

            attrListPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SecKeychainAttributeList>());
            Marshal.StructureToPtr(attributeList, attrListPtr, false);

            var error = SecKeychainSearchCreateFromAttributes(
                IntPtr.Zero,
                SecItemClassGenericPassword,
                attrListPtr,
                out searchRef);

            if (error == ErrorSecItemNotFound)
                return records;

            ThrowOnError(error);

            while (true)
            {
                IntPtr itemRef = IntPtr.Zero;
                error = SecKeychainSearchCopyNext(searchRef, out itemRef);

                if (error == ErrorSecItemNotFound)
                    break;

                ThrowOnError(error);

                if (itemRef == IntPtr.Zero)
                    break;

                try
                {
                    var record = GetItemRecord(itemRef, service);
                    records.Add(record);
                }
                finally
                {
                    if (itemRef != IntPtr.Zero)
                        CFRelease(itemRef);
                }
            }
        }
        finally
        {
            if (attrData != IntPtr.Zero)
                Marshal.FreeHGlobal(attrData);

            if (attrPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(attrPtr);

            if (attrListPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(attrListPtr);

            if (searchRef != IntPtr.Zero)
                CFRelease(searchRef);
        }

        return records;
    }

    /// <summary>
    /// Deletes a secret from the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>True if the secret was deleted, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// KeyChain.DeleteSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static bool DeleteSecret(string service, string account)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemDelete(itemRef));

                return true;
            }

            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    /// <summary>
    /// Deletes the first secret found for a given service in the Keychain.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <returns>True if a secret was deleted, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// KeyChain.DeleteSecret("MyService");
    /// </code>
    /// </example>
    /// </remarks>
    public static bool DeleteSecret(string service)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                0,
                null,
                out _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemDelete(itemRef));

                return true;
            }

            return false;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    /// <summary>
    /// Sets a secret in the Keychain as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="password">The password data as a byte array.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// KeyChain.SetSecret("MyService", "MyAccount", new byte[] { 1, 2, 3 });
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(string service, string account, byte[] password)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out uint _,
                out passwordData,
                out itemRef);

            if (itemRef != IntPtr.Zero)
            {
                ThrowOnError(
                    SecKeychainItemModifyAttributesAndData(
                        itemRef,
                        IntPtr.Zero,
                        (uint)password.Length,
                        password),
                    $"Could not update password for {service}/{account}");
            }
            else
            {
                ThrowOnError(
                    SecKeychainAddGenericPassword(
                        IntPtr.Zero,
                        (uint)service.Length,
                        service,
                        (uint)account.Length,
                        account,
                        (uint)password.Length,
                        password,
                        out itemRef),
                    $"Could not create key chain credential for {service}/{account}");
            }
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    /// <summary>
    /// Sets a secret in the Keychain as a string.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <param name="password">The password string.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// KeyChain.SetSecret("MyService", "MyAccount", "MySecret");
    /// </code>
    /// </example>
    /// </remarks>
    public static void SetSecret(string service, string account, string password)
        => SetSecret(service, account, Encoding.UTF8.GetBytes(password));

    /// <summary>
    /// Gets a secret from the Keychain as a string.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a string, or null if not found.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = KeyChain.GetSecret("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static string? GetSecret(string service, string account)
    {
        var data = GetSecretAsBytes(service, account);
        if (data.Length == 0)
            return string.Empty;

        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    /// Gets a secret from the Keychain as a byte array.
    /// </summary>
    /// <param name="service">The name of the service.</param>
    /// <param name="account">The account name.</param>
    /// <returns>The secret value as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secretBytes = KeyChain.GetSecretAsBytes("MyService", "MyAccount");
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] GetSecretAsBytes(string service, string account)
    {
        IntPtr passwordData = IntPtr.Zero;
        IntPtr itemRef = IntPtr.Zero;

        try
        {
            var error = SecKeychainFindGenericPassword(
                IntPtr.Zero,
                (uint)service.Length,
                service,
                (uint)account.Length,
                account,
                out uint passwordLength,
                out passwordData,
                out itemRef);

            if (error == NativeMethods.ErrorSecItemNotFound)
                return Array.Empty<byte>();

            ThrowOnError(error);

            return NativeMethods.ToByteArray(passwordData, passwordLength);
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }

            if (itemRef != IntPtr.Zero)
            {
                CFRelease(itemRef);
            }
        }
    }

    private static KeyringRecord GetItemRecord(IntPtr itemRef, string service)
    {
        string account = GetAccountAttribute(itemRef);
        return new KeyringRecord(service, account);
    }

    private static string GetAccountAttribute(IntPtr itemRef)
    {
        IntPtr tagArrayPtr = IntPtr.Zero;
        IntPtr formatArrayPtr = IntPtr.Zero;
        IntPtr attrListPtr = IntPtr.Zero;

        try
        {
            tagArrayPtr = Marshal.AllocHGlobal(sizeof(SecKeychainAttrType));
            Marshal.Copy(new[] { (int)SecKeychainAttrType.AccountItem }, 0, tagArrayPtr, 1);

            formatArrayPtr = Marshal.AllocHGlobal(sizeof(int));
            Marshal.Copy(new[] { (int)0 }, 0, formatArrayPtr, 1);

            var attributeInfo = new SecKeychainAttributeInfo
            {
                Count = 1,
                Tag = tagArrayPtr,
                Format = formatArrayPtr,
            };

            var error = SecKeychainItemCopyAttributesAndData(
                itemRef,
                ref attributeInfo,
                IntPtr.Zero,
                out attrListPtr,
                out var _,
                IntPtr.Zero);

            if (error != 0)
                return string.Empty;

            var attrList = Marshal.PtrToStructure<SecKeychainAttributeList>(attrListPtr);
            if (attrList.Count == 0)
                return string.Empty;

            var attrBytes = ToByteArray(attrList.Attributes, Marshal.SizeOf<SecKeychainAttribute>() * (int)attrList.Count);
            var attributes = ToStructArray<SecKeychainAttribute>(attrBytes);

            if (attributes.Length == 0 || attributes[0].Data == IntPtr.Zero)
                return string.Empty;

            var accountBytes = ToByteArray(attributes[0].Data, attributes[0].Length);
            return Encoding.UTF8.GetString(accountBytes);
        }
        finally
        {
            if (tagArrayPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(tagArrayPtr);

            if (formatArrayPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(formatArrayPtr);

            if (attrListPtr != IntPtr.Zero)
                SecKeychainItemFreeAttributesAndData(attrListPtr, IntPtr.Zero);
        }
    }
}