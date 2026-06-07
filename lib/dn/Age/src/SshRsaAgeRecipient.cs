using System.Security.Cryptography;

namespace NeoBeard.Age;

internal sealed class SshRsaAgeRecipient(RSA publicKey, byte[] wireKey)
    : AgeRecipient
{
    internal static string SshFingerprint(byte[] wireKey)
    {
        return AgeBase64.Encode(SHA256.HashData(wireKey).AsSpan(0, 4));
    }

    public override string Kind => "ssh-rsa";

    internal override Stanza Wrap(byte[] fileKey)
    {
        var fingerprint = SshFingerprint(wireKey);
        var body = RsaOaep.Encrypt(publicKey, fileKey);
        return new Stanza("ssh-rsa", [fingerprint], body);
    }
}
