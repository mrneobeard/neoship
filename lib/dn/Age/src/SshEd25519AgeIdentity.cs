namespace NeoBeard.Age;

internal sealed class SshEd25519AgeIdentity(byte[] seed, byte[] publicKey, byte[] wireKey)
    : AgeIdentity
{
    private const string Label = "age-encryption.org/v1/ssh-ed25519";

    private readonly byte[] privateKey = Ed25519Conversion.PrivateSeedToX25519(seed);

    private readonly byte[] curve25519PublicKey = Ed25519Conversion.PublicKeyToX25519(publicKey);

    public override string Kind => "ssh-ed25519";

    internal override byte[]? Unwrap(IReadOnlyList<Stanza> stanzas)
    {
        var fingerprint = SshRsaAgeRecipient.SshFingerprint(wireKey);
        foreach (var stanza in stanzas)
        {
            if (!string.Equals(stanza.Type, "ssh-ed25519", StringComparison.Ordinal))
                continue;

            if (stanza.Args.Length != 2 || stanza.Body.Length != 32)
                throw new AgeException("The ssh-ed25519 recipient stanza is invalid.");

            if (!string.Equals(stanza.Args[0], fingerprint, StringComparison.Ordinal))
                continue;

            var ephemeralShare = AgeBase64.Decode(stanza.Args[1]);
            if (ephemeralShare.Length != 32)
                throw new AgeException("The ssh-ed25519 ephemeral share is invalid.");

            var sharedSecret = X25519.ScalarMult(this.privateKey, ephemeralShare);
            if (sharedSecret.All(static b => b == 0))
                throw new AgeException("The ssh-ed25519 shared secret is invalid.");

            sharedSecret = SshEd25519AgeRecipient.TweakSharedSecret(sharedSecret, wireKey);
            var salt = new byte[64];
            ephemeralShare.CopyTo(salt);
            this.curve25519PublicKey.CopyTo(salt.AsSpan(32));

            var wrapKey = CryptoPrimitives.Hkdf(sharedSecret, salt, Label);
            var fileKey = CryptoPrimitives.AeadDecrypt(wrapKey, stanza.Body);
            if (fileKey is { Length: 16 })
                return fileKey;
        }

        return null;
    }
}
