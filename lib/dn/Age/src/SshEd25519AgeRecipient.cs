using System.Security.Cryptography;

namespace NeoBeard.Age;

internal sealed class SshEd25519AgeRecipient(byte[] curve25519PublicKey, byte[] wireKey)
    : AgeRecipient
{
    private const string Label = "age-encryption.org/v1/ssh-ed25519";

    internal static byte[] TweakSharedSecret(byte[] sharedSecret, byte[] wireKey)
    {
        var tweak = CryptoPrimitives.Hkdf([], wireKey, Label);
        return X25519.ScalarMult(tweak, sharedSecret);
    }

    public override string Kind => "ssh-ed25519";

    internal override Stanza Wrap(byte[] fileKey)
    {
        var ephemeralSecret = RandomNumberGenerator.GetBytes(32);
        var ephemeralShare = X25519.ScalarMultBase(ephemeralSecret);
        var sharedSecret = X25519.ScalarMult(ephemeralSecret, curve25519PublicKey);
        sharedSecret = TweakSharedSecret(sharedSecret, wireKey);

        var salt = new byte[64];
        ephemeralShare.CopyTo(salt);
        curve25519PublicKey.CopyTo(salt.AsSpan(32));
        var wrapKey = CryptoPrimitives.Hkdf(sharedSecret, salt, Label);

        return new Stanza(
            "ssh-ed25519",
            [SshRsaAgeRecipient.SshFingerprint(wireKey), AgeBase64.Encode(ephemeralShare)],
            CryptoPrimitives.AeadEncrypt(wrapKey, fileKey));
    }
}
