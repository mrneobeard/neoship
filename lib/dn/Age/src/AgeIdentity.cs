using System.Security.Cryptography;

namespace NeoBeard.Age;

/// <summary>
/// Represents an identity that can decrypt an age encrypted file.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var key = AgeKey.Create();
/// var identity = AgeIdentity.FromPrivateKey(key.PrivateKey);
/// Console.WriteLine(identity.Kind);
/// </code>
/// </example>
/// </remarks>
public abstract class AgeIdentity
{
    private protected AgeIdentity()
    {
    }

    /// <summary>
    /// Gets the identity kind.
    /// </summary>
    /// <value>The identity kind.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// var identity = AgeIdentity.FromPrivateKey(key.PrivateKey);
    /// Console.WriteLine(identity.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public abstract string Kind { get; }

    /// <summary>
    /// Creates an age identity from an age X25519 private key.
    /// </summary>
    /// <param name="privateKey">The private key encoded with the `AGE-SECRET-KEY-` Bech32 prefix.</param>
    /// <returns>An age X25519 identity.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// var identity = AgeIdentity.FromPrivateKey(key.PrivateKey);
    /// Console.WriteLine(identity.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeIdentity FromPrivateKey(string privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        var (hrp, bytes) = Bech32.Decode(privateKey);
        if (!string.Equals(hrp, "age-secret-key-", StringComparison.Ordinal) || bytes.Length != 32)
            throw new FormatException("The age private key is invalid.");

        return new X25519AgeIdentity(bytes);
    }

    /// <summary>
    /// Creates an SSH RSA identity from PEM private key text.
    /// </summary>
    /// <param name="pem">The RSA private key in PEM form.</param>
    /// <returns>An SSH RSA identity.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var rsa = RSA.Create(2048);
    /// var identity = AgeIdentity.FromSshPrivateKey(rsa.ExportRSAPrivateKeyPem());
    /// Console.WriteLine(identity.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeIdentity FromSshPrivateKey(string pem)
    {
        ArgumentNullException.ThrowIfNull(pem);
        if (pem.Contains("OPENSSH PRIVATE KEY", StringComparison.Ordinal))
        {
            var ed25519 = SshEd25519Key.ParsePrivateKey(pem);
            return new SshEd25519AgeIdentity(ed25519.Seed, ed25519.PublicKey, ed25519.WireKey);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return FromSshPrivateKey(rsa);
    }

    /// <summary>
    /// Creates an SSH RSA identity from an RSA private key.
    /// </summary>
    /// <param name="rsa">The RSA private key.</param>
    /// <returns>An SSH RSA identity.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var rsa = RSA.Create(2048);
    /// var identity = AgeIdentity.FromSshPrivateKey(rsa);
    /// Console.WriteLine(identity.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeIdentity FromSshPrivateKey(RSA rsa)
    {
        ArgumentNullException.ThrowIfNull(rsa);
        return new SshRsaAgeIdentity(rsa);
    }

    internal abstract byte[]? Unwrap(IReadOnlyList<Stanza> stanzas);
}