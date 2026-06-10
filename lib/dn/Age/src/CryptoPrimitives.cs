using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Age;

internal static class CryptoPrimitives
{
    private static readonly byte[] Empty = [];
    private static readonly byte[] EmptyNonce = new byte[12];

    public static byte[] Hkdf(byte[] ikm, byte[] salt, string info, int length = 32)
    {
        return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, length, salt, Encoding.ASCII.GetBytes(info));
    }

    public static byte[] Hkdf(byte[] ikm, string info, int length = 32)
    {
        return Hkdf(ikm, Empty, info, length);
    }

    public static byte[] AeadEncrypt(byte[] key, byte[] plaintext)
    {
        var ciphertext = new byte[plaintext.Length + 16];
        using var aead = new ChaCha20Poly1305(key);
        aead.Encrypt(EmptyNonce, plaintext, ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length));
        return ciphertext;
    }

    public static byte[]? AeadDecrypt(byte[] key, byte[] ciphertext)
    {
        if (ciphertext.Length < 16)
            return null;

        var plaintext = new byte[ciphertext.Length - 16];
        using var aead = new ChaCha20Poly1305(key);
        try
        {
            aead.Decrypt(EmptyNonce, ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length), plaintext);
            return plaintext;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}