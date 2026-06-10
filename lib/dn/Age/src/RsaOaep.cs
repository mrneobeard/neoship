using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Age;

internal static class RsaOaep
{
    private static readonly byte[] Label = Encoding.ASCII.GetBytes("age-encryption.org/v1/ssh-rsa");

    public static byte[] Encrypt(RSA rsa, byte[] message)
    {
        var parameters = rsa.ExportParameters(false);
        var modulus = parameters.Modulus ?? throw new AgeException("RSA modulus is missing.");
        var exponent = parameters.Exponent ?? throw new AgeException("RSA exponent is missing.");
        var encoded = Encode(message, modulus.Length);
        var encrypted = BigInteger.ModPow(FromBigEndian(encoded), FromBigEndian(exponent), FromBigEndian(modulus));
        return ToBigEndian(encrypted, modulus.Length);
    }

    public static byte[] Decrypt(RSA rsa, byte[] ciphertext)
    {
        var parameters = rsa.ExportParameters(true);
        var modulus = parameters.Modulus ?? throw new AgeException("RSA modulus is missing.");
        var exponent = parameters.D ?? throw new AgeException("RSA private exponent is missing.");
        if (ciphertext.Length != modulus.Length)
            throw new CryptographicException("The RSA ciphertext has invalid length.");

        var encoded = BigInteger.ModPow(FromBigEndian(ciphertext), FromBigEndian(exponent), FromBigEndian(modulus));
        return Decode(ToBigEndian(encoded, modulus.Length));
    }

    private static byte[] Encode(byte[] message, int length)
    {
        const int hashLength = 32;
        if (message.Length > length - (2 * hashLength) - 2)
            throw new CryptographicException("The RSA OAEP message is too long.");

        var labelHash = SHA256.HashData(Label);
        var ps = new byte[length - message.Length - (2 * hashLength) - 2];
        var db = labelHash.Concat(ps).Concat(new byte[] { 1 }).Concat(message).ToArray();
        var seed = RandomNumberGenerator.GetBytes(hashLength);
        var dbMask = Mgf1(seed, length - hashLength - 1);
        var maskedDb = Xor(db, dbMask);
        var seedMask = Mgf1(maskedDb, hashLength);
        var maskedSeed = Xor(seed, seedMask);
        return [0, .. maskedSeed, .. maskedDb];
    }

    private static byte[] Decode(byte[] encoded)
    {
        const int hashLength = 32;
        if (encoded.Length < (2 * hashLength) + 2 || encoded[0] != 0)
            throw new CryptographicException("The RSA OAEP block is invalid.");

        var maskedSeed = encoded.AsSpan(1, hashLength).ToArray();
        var maskedDb = encoded.AsSpan(hashLength + 1).ToArray();
        var seed = Xor(maskedSeed, Mgf1(maskedDb, hashLength));
        var db = Xor(maskedDb, Mgf1(seed, encoded.Length - hashLength - 1));
        var labelHash = SHA256.HashData(Label);
        if (!CryptographicOperations.FixedTimeEquals(db.AsSpan(0, hashLength), labelHash))
            throw new CryptographicException("The RSA OAEP label is invalid.");

        var index = hashLength;
        while (index < db.Length && db[index] == 0)
            index++;

        if (index >= db.Length || db[index] != 1)
            throw new CryptographicException("The RSA OAEP padding is invalid.");

        return db[(index + 1)..];
    }

    private static byte[] Mgf1(byte[] seed, int length)
    {
        using var hash = SHA256.Create();
        var output = new byte[length];
        var counter = 0u;
        var offset = 0;
        var c = new byte[4];
        while (offset < output.Length)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(c, counter++);
            hash.Initialize();
            hash.TransformBlock(seed, 0, seed.Length, null, 0);
            hash.TransformFinalBlock(c, 0, c.Length);
            var digest = hash.Hash ?? throw new CryptographicException("SHA-256 failed.");
            var take = Math.Min(digest.Length, output.Length - offset);
            digest.AsSpan(0, take).CopyTo(output.AsSpan(offset));
            offset += take;
        }

        return output;
    }

    private static byte[] Xor(byte[] left, byte[] right)
    {
        var result = new byte[left.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = (byte)(left[i] ^ right[i]);

        return result;
    }

    private static BigInteger FromBigEndian(byte[] value)
    {
        return new BigInteger(value, isUnsigned: true, isBigEndian: true);
    }

    private static byte[] ToBigEndian(BigInteger value, int length)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length > length)
            throw new CryptographicException("The RSA value is too large.");

        if (bytes.Length == length)
            return bytes;

        var result = new byte[length];
        bytes.CopyTo(result.AsSpan(length - bytes.Length));
        return result;
    }
}