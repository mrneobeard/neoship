using System.Security.Cryptography;

namespace NeoBeard.Age;

internal sealed class SshRsaAgeIdentity(RSA privateKey)
    : AgeIdentity
{
    private readonly byte[] wireKey = SshRsaKey.BuildWireKey(privateKey.ExportParameters(false));

    public override string Kind => "ssh-rsa";

    internal override byte[]? Unwrap(IReadOnlyList<Stanza> stanzas)
    {
        var fingerprint = SshRsaAgeRecipient.SshFingerprint(this.wireKey);
        foreach (var stanza in stanzas)
        {
            if (!string.Equals(stanza.Type, "ssh-rsa", StringComparison.Ordinal))
                continue;

            if (stanza.Args.Length != 1)
                throw new AgeException("The ssh-rsa recipient stanza is invalid.");

            if (!string.Equals(stanza.Args[0], fingerprint, StringComparison.Ordinal))
                continue;

            try
            {
                var fileKey = RsaOaep.Decrypt(privateKey, stanza.Body);
                return fileKey.Length == 16 ? fileKey : null;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        return null;
    }
}