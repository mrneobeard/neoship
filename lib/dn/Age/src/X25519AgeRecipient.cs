using System.Security.Cryptography;

namespace NeoBeard.Age;

internal sealed class X25519AgeRecipient(byte[] publicKey)
    : AgeRecipient
{
    public override string Kind => "X25519";

    internal override Stanza Wrap(byte[] fileKey)
    {
        var ephemeralSecret = RandomNumberGenerator.GetBytes(32);
        var ephemeralShare = X25519.ScalarMultBase(ephemeralSecret);
        var sharedSecret = X25519.ScalarMult(ephemeralSecret, publicKey);
        var salt = new byte[64];
        ephemeralShare.CopyTo(salt);
        publicKey.CopyTo(salt.AsSpan(32));
        var wrapKey = CryptoPrimitives.Hkdf(sharedSecret, salt, "age-encryption.org/v1/X25519");
        return new Stanza("X25519", [AgeBase64.Encode(ephemeralShare)], CryptoPrimitives.AeadEncrypt(wrapKey, fileKey));
    }
}