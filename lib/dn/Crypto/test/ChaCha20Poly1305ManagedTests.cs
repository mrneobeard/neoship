using System.Text;

namespace NeoBeard.Crypto.Tests;

public static class ChaCha20Poly1305ManagedTests
{
    [Fact]
    public static void Encrypt_Matches_Rfc8439_Vector()
    {
        var key = Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f");
        var nonce = Convert.FromHexString("070000004041424344454647");
        var associatedData = Convert.FromHexString("50515253c0c1c2c3c4c5c6c7");
        var plaintext = Encoding.ASCII.GetBytes("Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it.");
        var expectedCiphertext = Convert.FromHexString(
            "d31a8d34648e60db7b86afbc53ef7ec2" +
            "a4aded51296e08fea9e2b5a736ee62d6" +
            "3dbea45e8ca9671282fafb69da92728b" +
            "1a71de0a9e060b2905d6a5b67ecd3b36" +
            "92ddbd7f2d778b8c9803aee328091b58" +
            "fab324e4fad675945585808b4831d7bc" +
            "3ff4def08e4b7a9de576d26586cec64b" +
            "6116");
        var expectedTag = Convert.FromHexString("1ae10b594f09e26a7e902ecbd0600691");
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[ChaCha20Poly1305Managed.TagSize];

        ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag, associatedData);

        Assert.Equal(expectedCiphertext, ciphertext);
        Assert.Equal(expectedTag, tag);
    }

    [Fact]
    public static void Decrypt_RoundTrips_Ciphertext()
    {
        var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Convert.FromHexString("000000000001020304050607");
        var associatedData = "metadata"u8.ToArray();
        var plaintext = "authenticated secret"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];
        var decrypted = new byte[plaintext.Length];
        var tag = new byte[ChaCha20Poly1305Managed.TagSize];

        ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag, associatedData);
        var authenticated = ChaCha20Poly1305Managed.Decrypt(key, nonce, ciphertext, tag, decrypted, associatedData);

        Assert.True(authenticated);
        Assert.Equal(plaintext, decrypted);
        Assert.NotEqual(plaintext, ciphertext);
    }

    [Fact]
    public static void Decrypt_With_Tampered_Tag_Returns_False_And_Clears_Plaintext()
    {
        var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Convert.FromHexString("000000000001020304050607");
        var plaintext = "authenticated secret"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];
        var decrypted = Enumerable.Repeat((byte)0xff, plaintext.Length).ToArray();
        var tag = new byte[ChaCha20Poly1305Managed.TagSize];

        ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag);
        tag[0] ^= 1;
        var authenticated = ChaCha20Poly1305Managed.Decrypt(key, nonce, ciphertext, tag, decrypted);

        Assert.False(authenticated);
        Assert.Equal(new byte[plaintext.Length], decrypted);
    }

    [Fact]
    public static void Decrypt_With_Tampered_Associated_Data_Returns_False()
    {
        var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var nonce = Convert.FromHexString("000000000001020304050607");
        var plaintext = "authenticated secret"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];
        var decrypted = new byte[plaintext.Length];
        var tag = new byte[ChaCha20Poly1305Managed.TagSize];

        ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag, "aad"u8);
        var authenticated = ChaCha20Poly1305Managed.Decrypt(key, nonce, ciphertext, tag, decrypted, "bad"u8);

        Assert.False(authenticated);
    }
}