using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.Linux;

/// <summary>
/// Exception thrown when an error occurs while interacting with the operating system's secret vault.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// throw new OsSecretException("An error occurred.");
/// </code>
/// </example>
/// </remarks>
public class OsSecretException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OsSecretException"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new OsSecretException();
    /// </code>
    /// </example>
    /// </remarks>
    public OsSecretException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OsSecretException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new OsSecretException("Error message");
    /// </code>
    /// </example>
    /// </remarks>
    public OsSecretException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OsSecretException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = new OsSecretException("Error message", new Exception("Inner error"));
    /// </code>
    /// </example>
    /// </remarks>
    public OsSecretException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the domain of the error.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var domain = ex.Domain;
    /// </code>
    /// </example>
    /// </remarks>
    public int Domain { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var code = ex.Code;
    /// </code>
    /// </example>
    /// </remarks>
    public int Code { get; set; }

    /// <summary>
    /// Creates a new <see cref="OsSecretException"/> from a native GError pointer.
    /// </summary>
    /// <param name="error">A pointer to the GError structure.</param>
    /// <returns>A new <see cref="OsSecretException"/> instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var ex = OsSecretException.Create(errorPtr);
    /// </code>
    /// </example>
    /// </remarks>
    public static OsSecretException Create(IntPtr error)
    {
        try
        {
            var gerror = Marshal.PtrToStructure<GError>(error);
            var message = Marshal.PtrToStringAnsi(gerror.Message);
            return new OsSecretException(message ?? string.Empty)
            {
                Domain = (int)gerror.Domain,
                Code = gerror.Code,
            };
        }
        finally
        {
            NativeMethods.g_error_free(error);
        }
    }

    /// <summary>
    /// Throws an <see cref="OsSecretException"/> if the provided error pointer is not null.
    /// </summary>
    /// <param name="error">A pointer to the GError structure.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// OsSecretException.ThrowIfError(errorPtr);
    /// </code>
    /// </example>
    /// </remarks>
    public static void ThrowIfError(IntPtr error)
    {
        if (error == IntPtr.Zero)
            return;

        throw Create(error);
    }

    private static class NativeMethods
    {
        [DllImport(Libraries.Glib, EntryPoint = "g_error_free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_error_free(IntPtr error);
    }
}