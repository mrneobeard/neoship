namespace NeoBeard.Ssh;

/// <summary>
/// Defines supported SSH authentication methods.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var method = SshAuthMethod.Password;
/// Console.WriteLine(method);
/// </code>
/// </example>
/// </remarks>
public enum SshAuthMethod
{
    /// <summary>
    /// Password-based authentication.
    /// </summary>
    Password,

    /// <summary>
    /// Public-key authentication using an identity key.
    /// </summary>
    PublicKey,
}