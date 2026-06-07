namespace NeoBeard.Age;

internal sealed class X25519AgeIdentity(byte[] privateKey)
    : AgeIdentity
{
    private readonly byte[] publicKey = X25519.ScalarMultBase(privateKey);

    public override string Kind => "X25519";

    internal override byte[]? Unwrap(IReadOnlyList<Stanza> stanzas)
    {
        var salt = new byte[64];

        foreach (var stanza in stanzas)
        {
            if (!string.Equals(stanza.Type, "X25519", StringComparison.Ordinal))
                continue;

            if (stanza.Args.Length != 1 || stanza.Body.Length != 32)
                throw new AgeException("The X25519 recipient stanza is invalid.");

            var ephemeralShare = AgeBase64.Decode(stanza.Args[0]);
            if (ephemeralShare.Length != 32)
                throw new AgeException("The X25519 ephemeral share is invalid.");

            var sharedSecret = X25519.ScalarMult(privateKey, ephemeralShare);
            if (sharedSecret.All(static b => b == 0))
                throw new AgeException("The X25519 shared secret is invalid.");

            ephemeralShare.CopyTo(salt);
            publicKey.CopyTo(salt.AsSpan(32));

            var wrapKey = CryptoPrimitives.Hkdf(sharedSecret, salt, "age-encryption.org/v1/X25519");
            var fileKey = CryptoPrimitives.AeadDecrypt(wrapKey, stanza.Body);
            if (fileKey is { Length: 16 })
                return fileKey;
        }

        return null;
    }
}
