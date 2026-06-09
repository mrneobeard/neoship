using System;

namespace NeoBeard.Crypto
{
    /// <summary>
    /// A delegate for encrypting or decrypting binary data, so that encryption methods can
    /// be swapped out.
    /// </summary>
    /// <param name="bytes">The binary data that will be encrtyped or decrypted.</param>
    /// <param name="state">state that helps with the encryption / decryption process.</param>
    /// <param name="action">The type of action for the delegate to perform.</param>
    /// <returns>Binary data that is encrypted or decrypted.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// MemoryProtectionAction protection = (bytes, state, action) =>
    /// {
    ///     return action == MemoryProtectionActionType.Encrypt
    ///         ? ProtectedMemory.Protect(bytes, MemoryProtectionScope.SameLogon)
    ///         : ProtectedMemory.Unprotect(bytes, MemoryProtectionScope.SameLogon);
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public delegate ReadOnlySpan<byte> MemoryProtectionAction(
        ReadOnlySpan<byte> bytes,
        object state,
        MemoryProtectionActionType action);
}