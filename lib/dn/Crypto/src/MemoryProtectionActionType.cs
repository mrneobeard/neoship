namespace NeoBeard.Crypto
{
    /// <summary>
    /// The type of action for the delegate <see cref="NeoBeard.Crypto.MemoryProtectionAction"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var action = MemoryProtectionActionType.Encrypt;
    /// Assert.Equal(MemoryProtectionActionType.Encrypt, action);
    /// </code>
    /// </example>
    /// </remarks>
    public enum MemoryProtectionActionType
    {
        /// <summary>Encrypt data.</summary>
        Encrypt,

        /// <summary>Decrypt data.</summary>
        Decrypt,
    }
}