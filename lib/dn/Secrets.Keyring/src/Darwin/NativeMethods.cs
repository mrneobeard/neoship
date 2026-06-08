using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Darwin;

// most of the source is based upon: https://github.com/mjcheetham/securestorage-dotnet/tree/master/src/Mjcheetham.SecureStorage
// which is under the MIT license.

/// <summary>
/// Native methods and structures for interacting with the macOS Keychain.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = NativeMethods.CFRelease(ptr);
/// </code>
/// </example>
/// </remarks>
internal static class NativeMethods
{
    /// <summary>
    /// Error code for "item not found".
    /// </summary>
    public const int ErrorSecItemNotFound = -25300;

    private const string CoreFoundationFrameworkLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string SecurityFrameworkLib = "/System/Library/Frameworks/Security.framework/Security";

    private const int OK = 0;
    private const int ErrorSecNoSuchKeychain = -25294;
    private const int ErrorSecInvalidKeychain = -25295;
    private const int ErrorSecAuthFailed = -25293;
    private const int ErrorSecDuplicateItem = -25299;

    private const int ErrorSecInteractionNotAllowed = -25308;
    private const int ErrorSecInteractionRequired = -25315;
    private const int ErrorSecNoSuchAttr = -25303;

    /// <summary>
    /// Specifies the type of an attribute in the Keychain.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var type = SecKeychainAttrType.AccountItem;
    /// </code>
    /// </example>
    /// </remarks>
    public enum SecKeychainAttrType : uint
    {
        /// <summary>
        /// Account item attribute.
        /// </summary>
        AccountItem = 1633903476,

        /// <summary>
        /// Service item attribute.
        /// </summary>
        ServiceItem = 1936553063,
    }

    /// <summary>
    /// The class for generic password items.
    /// </summary>
    public const int SecItemClassGenericPassword = 0x67656E70; // 'genp'

    /// <summary>
    /// Throws an exception based on the provided error code.
    /// </summary>
    /// <param name="error">The error code returned by a native Keychain function.</param>
    /// <param name="defaultErrorMessage">The default error message to use if the error code is not recognized.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// NativeMethods.ThrowOnError(errorCode);
    /// </code>
    /// </example>
    /// </remarks>
    public static void ThrowOnError(int error, string defaultErrorMessage = "Unknown error.")
    {
        switch (error)
        {
            case OK:
                return;
            case ErrorSecNoSuchKeychain:
                throw new InvalidOperationException($"The keychain does not exist. ({ErrorSecNoSuchKeychain})");
            case ErrorSecInvalidKeychain:
                throw new InvalidOperationException($"The keychain is not valid. ({ErrorSecInvalidKeychain})");
            case ErrorSecAuthFailed:
                throw new InvalidOperationException($"Authorization/Authentication failed. ({ErrorSecAuthFailed})");
            case ErrorSecDuplicateItem:
                throw new ArgumentException($"The key chain item already exists. ({ErrorSecDuplicateItem})");
            case ErrorSecItemNotFound:
                throw new KeyNotFoundException($"The key chain item cannot be found. ({ErrorSecItemNotFound})");
            case ErrorSecInteractionNotAllowed:
                throw new InvalidOperationException($"Interaction with the Security Server is not allowed. ({ErrorSecInteractionNotAllowed})");
            case ErrorSecInteractionRequired:
                throw new InvalidOperationException($"User interaction is required. ({ErrorSecInteractionRequired})");
            case ErrorSecNoSuchAttr:
                throw new InvalidOperationException($"The attribute does not exist. ({ErrorSecNoSuchAttr})");
            default:
                throw new InvalidOperationException($"{defaultErrorMessage} with error code {error}");
        }
    }

    /// <summary>
    /// Releases a CoreFoundation object.
    /// </summary>
    /// <param name="cf">The pointer to the CoreFoundation object.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// NativeMethods.CFRelease(ptr);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(CoreFoundationFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CFRelease(IntPtr cf);

    /// <summary>
    /// Adds a generic password to the Keychain.
    /// </summary>
    /// <param name="keychain">The keychain to add the password to, or IntPtr.Zero for the default keychain.</param>
    /// <param name="serviceNameLength">The length of the service name.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="accountNameLength">The length of the account name.</param>
    /// <param name="accountName">The account name.</param>
    /// <param name="passwordLength">The length of the password data.</param>
    /// <param name="passwordData">The password data as a byte array.</param>
    /// <param name="itemRef">The resulting item reference.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainAddGenericPassword(IntPtr.Zero, service.Length, service, account.Length, account, password.Length, password, out var itemRef);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainAddGenericPassword(
        IntPtr keychain,
        uint serviceNameLength,
        string serviceName,
        uint accountNameLength,
        string accountName,
        uint passwordLength,
        byte[] passwordData,
        out IntPtr itemRef);

    /// <summary>
    /// Finds a generic password in the Keychain.
    /// </summary>
    /// <param name="keychainOrArray">The keychain or array of keychains to search, or IntPtr.Zero for the default search list.</param>
    /// <param name="serviceNameLength">The length of the service name.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="accountNameLength">The length of the account name.</param>
    /// <param name="accountName">The account name.</param>
    /// <param name="passwordLength">The resulting password length.</param>
    /// <param name="passwordData">The resulting password data pointer.</param>
    /// <param name="itemRef">The resulting item reference.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainFindGenericPassword(IntPtr.Zero, service.Length, service, account.Length, account, out var length, out var data, out var itemRef);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainFindGenericPassword(
        IntPtr keychainOrArray,
        uint serviceNameLength,
        string serviceName,
        uint accountNameLength,
        string? accountName,
        out uint passwordLength,
        out IntPtr passwordData,
        out IntPtr itemRef);

    /// <summary>
    /// Copies attributes and data from a Keychain item.
    /// </summary>
    /// <param name="itemRef">The item reference.</param>
    /// <param name="info">The attribute info to copy.</param>
    /// <param name="itemClass">The item class.</param>
    /// <param name="attrList">The resulting attribute list pointer.</param>
    /// <param name="dataLength">The resulting data length.</param>
    /// <param name="data">The pointer to the data buffer.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainItemCopyAttributesAndData(itemRef, ref info, IntPtr.Zero, out var attrList, out var dataLength, IntPtr.Zero);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainItemCopyAttributesAndData(
        IntPtr itemRef,
        ref SecKeychainAttributeInfo info,
        IntPtr itemClass, // SecItemClass*
        out IntPtr attrList, // SecKeychainAttributeList*
        out uint dataLength,
        IntPtr data);

    /// <summary>
    /// Modifies attributes and data of a Keychain item.
    /// </summary>
    /// <param name="itemRef">The item reference.</param>
    /// <param name="attrList">The attribute list to modify, or IntPtr.Zero.</param>
    /// <param name="length">The length of the new data.</param>
    /// <param name="data">The new data.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainItemModifyAttributesAndData(itemRef, IntPtr.Zero, data.Length, data);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainItemModifyAttributesAndData(
        IntPtr itemRef,
        IntPtr attrList, // SecKeychainAttributeList*
        uint length,
        byte[] data);

    /// <summary>
    /// Deletes a Keychain item.
    /// </summary>
    /// <param name="itemRef">The item reference to delete.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainItemDelete(itemRef);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainItemDelete(
        IntPtr itemRef);

    /// <summary>
    /// Frees the content of a Keychain item.
    /// </summary>
    /// <param name="attrList">The attribute list pointer, or IntPtr.Zero.</param>
    /// <param name="data">The data pointer, or IntPtr.Zero.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, dataPtr);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainItemFreeContent(
        IntPtr attrList, // SecKeychainAttributeList*
        IntPtr data);

    /// <summary>
    /// Frees the attributes and data of a Keychain item.
    /// </summary>
    /// <param name="attrList">The attribute list pointer.</param>
    /// <param name="data">The data pointer.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// NativeMethods.SecKeychainItemFreeAttributesAndData(attrListPtr, IntPtr.Zero);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainItemFreeAttributesAndData(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

    /// <summary>
    /// Creates a search for Keychain items based on attributes.
    /// </summary>
    /// <param name="keychainOrArray">The keychain or array of keychains to search.</param>
    /// <param name="itemClass">The class of items to search for.</param>
    /// <param name="attrList">The attribute list to match.</param>
    /// <param name="searchRef">The resulting search reference.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainSearchCreateFromAttributes(IntPtr.Zero, SecItemClassGenericPassword, attrListPtr, out var searchRef);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainSearchCreateFromAttributes(
        IntPtr keychainOrArray,
        int itemClass, // SecItemClass
        IntPtr attrList, // SecKeychainAttributeList*
        out IntPtr searchRef);

    /// <summary>
    /// Copies the next item from a Keychain search.
    /// </summary>
    /// <param name="searchRef">The search reference.</param>
    /// <param name="itemRef">The resulting item reference.</param>
    /// <returns>A status code.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var status = NativeMethods.SecKeychainSearchCopyNext(searchRef, out var itemRef);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SecKeychainSearchCopyNext(
        IntPtr searchRef,
        out IntPtr itemRef);

    /// <summary>
    /// Converts a byte array to an array of structs.
    /// </summary>
    /// <typeparam name="T">The type of struct.</typeparam>
    /// <param name="source">The source byte array.</param>
    /// <returns>The resulting array of structs.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var structs = NativeMethods.ToStructArray&lt;MyStruct&gt;(bytes);
    /// </code>
    /// </example>
    /// </remarks>
    public static T[] ToStructArray<T>(byte[] source)
        where T : struct
    {
        var destination = new T[source.Length / Marshal.SizeOf<T>()];
        GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
        try
        {
            IntPtr pointer = handle.AddrOfPinnedObject();
            Marshal.Copy(source, 0, pointer, source.Length);
            return destination;
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    /// <summary>
    /// Converts a pointer to a byte array.
    /// </summary>
    /// <param name="ptr">The pointer to the native memory.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <returns>The resulting byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var bytes = NativeMethods.ToByteArray(ptr, count);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] ToByteArray(IntPtr ptr, long count)
    {
        var destination = new byte[count];
        Marshal.Copy(ptr, destination, 0, destination.Length);
        return destination;
    }

    /// <summary>
    /// Information about Keychain attributes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var info = new SecKeychainAttributeInfo();
    /// </code>
    /// </example>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttributeInfo
    {
        /// <summary>
        /// The number of attributes.
        /// </summary>
        public uint Count;

        /// <summary>
        /// A pointer to the attribute tags.
        /// </summary>
        public IntPtr Tag; // uint type of SecKeychainAttrType

        /// <summary>
        /// A pointer to the attribute formats.
        /// </summary>
        public IntPtr Format; // uint type of CssmDbAttributeFormat
    }

    /// <summary>
    /// A list of Keychain attributes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var list = new SecKeychainAttributeList();
    /// </code>
    /// </example>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttributeList
    {
        /// <summary>
        /// The number of attributes in the list.
        /// </summary>
        public uint Count;

        /// <summary>
        /// A pointer to the attributes array.
        /// </summary>
        public IntPtr Attributes; // type of SecKeychainAttribute*
    }

    /// <summary>
    /// A single Keychain attribute.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var attr = new SecKeychainAttribute();
    /// </code>
    /// </example>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SecKeychainAttribute
    {
        /// <summary>
        /// The attribute tag.
        /// </summary>
        public SecKeychainAttrType Tag;

        /// <summary>
        /// The length of the attribute data.
        /// </summary>
        public uint Length;

        /// <summary>
        /// A pointer to the attribute data.
        /// </summary>
        public IntPtr Data;
    }
}