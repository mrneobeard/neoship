using System.Runtime.InteropServices;
using System.Text;

namespace NeoBeard.Secrets.Win32;

/// <summary>
/// A safe handle for Windows credentials.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var handle = new CredentialHandle(ptr);
/// </code>
/// </example>
/// </remarks>
public class CredentialHandle : Microsoft.Win32.SafeHandles.CriticalHandleMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialHandle"/> class.
    /// </summary>
    /// <param name="handle">The native credential handle.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var handle = new CredentialHandle(ptr);
    /// </code>
    /// </example>
    /// </remarks>
    public CredentialHandle(IntPtr handle)
    {
        this.SetHandle(handle);
    }

    /// <summary>
    /// Allocates a <see cref="WinCredSecret"/> from the handle.
    /// </summary>
    /// <returns>A new <see cref="WinCredSecret"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = handle.AllocateCredential();
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredSecret AllocateCredential()
    {
        if (this.IsInvalid)
            throw new InvalidOperationException($"{typeof(CriticalHandle).FullName} handle is invalid");

        return AllocateCredentialFromHandle(this.handle);
    }

    /// <summary>
    /// Allocates an array of <see cref="WinCredSecret"/> instances from the handle.
    /// </summary>
    /// <param name="count">The number of credentials to allocate.</param>
    /// <returns>An array of <see cref="WinCredSecret"/> instances.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secrets = handle.AllocateCredentials(5);
    /// </code>
    /// </example>
    /// </remarks>
    public WinCredSecret[] AllocateCredentials(int count)
    {
        if (this.IsInvalid)
            throw new InvalidOperationException("Invalid CriticalHandle!");

        var credentials = new WinCredSecret[count];
        for (int i = 0; i < count; i++)
        {
            IntPtr nextPointer = Marshal.ReadIntPtr(this.handle, i * IntPtr.Size);
            var credential = AllocateCredentialFromHandle(nextPointer);
            var index = credential.Service.IndexOf(":target=", StringComparison.InvariantCulture);
            if (index > -1)
            {
                var key = credential.Service;
                key = key.Substring(index + 1);
                credentials[i] = new WinCredSecret(
                    credential.Type,
                    key,
                    credential.Account,
                    credential.Password,
                    credential.Comment);
                continue;
            }

            credentials[i] = credential;
        }

        return credentials;
    }

    /// <summary>
    /// Gets the secret blob as a byte array from the handle.
    /// </summary>
    /// <returns>The secret blob as a byte array.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var bytes = handle.GetSecretAsBytes();
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] GetSecretAsBytes()
    {
        if (this.IsInvalid)
            throw new InvalidOperationException("Invalid CriticalHandle!");

        var native = Marshal.PtrToStructure<NativeCredential>(this.handle);
        var data = new byte[native.CredentialBlobSize];
        Marshal.Copy(native.CredentialBlob, data, 0, native.CredentialBlobSize);
        return data;
    }

    /// <summary>
    /// Gets the secret blob as a string from the handle.
    /// </summary>
    /// <returns>The secret blob as a string.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var secret = handle.GetSecret();
    /// </code>
    /// </example>
    /// </remarks>
    public string GetSecret()
    {
        if (this.IsInvalid)
            throw new InvalidOperationException("Invalid CriticalHandle!");

        var native = Marshal.PtrToStructure<NativeCredential>(this.handle);
        var data = new byte[native.CredentialBlobSize];
        Marshal.Copy(native.CredentialBlob, data, 0, native.CredentialBlobSize);
        return Encoding.UTF8.GetString(data);
    }

    protected override bool ReleaseHandle()
    {
        if (this.IsInvalid)
            return false;

        NativeMethods.FreeCredential(this.handle);
        this.SetHandleAsInvalid();
        return true;
    }

    private static WinCredSecret AllocateCredentialFromHandle(IntPtr handle)
    {
        var native = Marshal.PtrToStructure<NativeCredential>(handle);

        // var fileTime = (((long)native.LastWritten.dwHighDateTime) << 32) + native.LastWritten.dwLowDateTime;
        byte[] data = Array.Empty<byte>();

        if (native.CredentialBlobSize > 0)
        {
            data = new byte[native.CredentialBlobSize];
            Marshal.Copy(native.CredentialBlob, data, 0, native.CredentialBlobSize);
        }

        return new WinCredSecret(
            native.Type,
            native.TargetName,
            native.UserName,
            data != null ? Encoding.Unicode.GetString(data) : null,
            native.Comment);
    }
}