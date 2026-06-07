using System.Security.Cryptography;

namespace NeoBeard.Age;

/// <summary>
/// Represents a recipient that can receive an age encrypted file.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var key = AgeKey.Create();
/// var recipient = AgeRecipient.FromPublicKey(key.PublicKey);
/// Console.WriteLine(recipient.Kind);
/// </code>
/// </example>
/// </remarks>
public abstract class AgeRecipient
{
    private protected AgeRecipient()
    {
    }

    /// <summary>
    /// Gets the recipient kind.
    /// </summary>
    /// <value>The recipient kind.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// var recipient = AgeRecipient.FromPublicKey(key.PublicKey);
    /// Console.WriteLine(recipient.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public abstract string Kind { get; }

    /// <summary>
    /// Creates an age recipient from an age X25519 public key.
    /// </summary>
    /// <param name="publicKey">The public key encoded with the `age` Bech32 prefix.</param>
    /// <returns>An age X25519 recipient.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// var recipient = AgeRecipient.FromPublicKey(key.PublicKey);
    /// Console.WriteLine(recipient.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeRecipient FromPublicKey(string publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);
        var (hrp, bytes) = Bech32.Decode(publicKey);
        if (!string.Equals(hrp, "age", StringComparison.Ordinal) || bytes.Length != 32)
            throw new FormatException("The age public key is invalid.");

        return new X25519AgeRecipient(bytes);
    }

    /// <summary>
    /// Creates an SSH RSA recipient from an authorized_keys line.
    /// </summary>
    /// <param name="authorizedKey">The SSH authorized key text.</param>
    /// <returns>An SSH RSA recipient.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var rsa = RSA.Create(2048);
    /// var recipient = AgeRecipient.FromSshPublicKey(SshRsaKey.ExportPublicKey(rsa));
    /// Console.WriteLine(recipient.Kind);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeRecipient FromSshPublicKey(string authorizedKey)
    {
        ArgumentNullException.ThrowIfNull(authorizedKey);
        if (authorizedKey.TrimStart().StartsWith("ssh-ed25519 ", StringComparison.Ordinal))
        {
            var ed25519 = SshEd25519Key.ParsePublicKey(authorizedKey);
            return new SshEd25519AgeRecipient(ed25519.Curve25519PublicKey, ed25519.WireKey);
        }

        var parsed = SshRsaKey.ParsePublicKey(authorizedKey);
        return new SshRsaAgeRecipient(parsed.PublicKey, parsed.WireKey);
    }

    internal abstract Stanza Wrap(byte[] fileKey);
}