using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Win32;

/// <summary>
/// Native methods for interacting with the Windows Credential Manager.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var result = NativeMethods.ReadCredential("target", WinCredType.Generic, 0, out var ptr);
/// </code>
/// </example>
/// </remarks>
internal static class NativeMethods
{
    /// <summary>
    /// Reads a credential from the Windows Credential Manager.
    /// </summary>
    /// <param name="target">The target name of the credential.</param>
    /// <param name="type">The type of the credential.</param>
    /// <param name="reservedFlag">Reserved flag, must be 0.</param>
    /// <param name="credentialPtr">The resulting pointer to the credential structure.</param>
    /// <returns>True if the credential was read successfully, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = NativeMethods.ReadCredential("target", WinCredType.Generic, 0, out var ptr);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool ReadCredential([In] string target, [In] WinCredType type, [In] int reservedFlag, out IntPtr credentialPtr);

    /// <summary>
    /// Writes a credential to the Windows Credential Manager.
    /// </summary>
    /// <param name="userCredential">The credential structure to write.</param>
    /// <param name="flags">Flags for the operation.</param>
    /// <returns>True if the credential was written successfully, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = NativeMethods.WriteCredential(ref cred, 0);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool WriteCredential([In] ref NativeCredential userCredential, [In] uint flags);

    /// <summary>
    /// Frees the memory allocated for a credential.
    /// </summary>
    /// <param name="credentialPointer">The pointer to the credential memory.</param>
    /// <returns>True if the memory was freed successfully, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// NativeMethods.FreeCredential(ptr);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    internal static extern bool FreeCredential([In] IntPtr credentialPointer);

    /// <summary>
    /// Deletes a credential from the Windows Credential Manager.
    /// </summary>
    /// <param name="target">The target name of the credential to delete.</param>
    /// <param name="type">The type of the credential.</param>
    /// <param name="reservedFlag">Reserved flag, must be 0.</param>
    /// <returns>True if the credential was deleted successfully, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = NativeMethods.DeleteCredential("target", WinCredType.Generic, 0);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool DeleteCredential([In] string target, [In] WinCredType type, [In] int reservedFlag);

    /// <summary>
    /// Enumerates credentials from the Windows Credential Manager.
    /// </summary>
    /// <param name="filter">A filter string, or null for all credentials.</param>
    /// <param name="flags">Flags for the operation.</param>
    /// <param name="count">The resulting number of credentials.</param>
    /// <param name="credentialPtrs">The resulting pointer to the array of credential pointers.</param>
    /// <returns>True if credentials were enumerated successfully, otherwise false.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var result = NativeMethods.EnumerateCredentials(null, 0, out var count, out var ptrs);
    /// </code>
    /// </example>
    /// </remarks>
    [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool EnumerateCredentials([In] string? filter, [In] int flags, out int count, out IntPtr credentialPtrs);
}