using System.Numerics;
using System.Security.Cryptography;

namespace NeoBeard.Age;

internal static class Ed25519Conversion
{
    private static readonly BigInteger Prime = (BigInteger.One << 255) - 19;

    public static byte[] PrivateSeedToX25519(byte[] seed)
    {
        var hash = SHA512.HashData(seed);
        hash[0] &= 248;
        hash[31] &= 127;
        hash[31] |= 64;
        return hash.AsSpan(0, 32).ToArray();
    }

    public static byte[] PublicKeyToX25519(ReadOnlySpan<byte> publicKey)
    {
        if (publicKey.Length != 32)
            throw new ArgumentException("Ed25519 public keys must be 32 bytes.", nameof(publicKey));

        Span<byte> yBytes = stackalloc byte[32];
        publicKey.CopyTo(yBytes);
        yBytes[31] &= 0x7f;
        var y = FromLittleEndian(yBytes);
        var u = Mod((BigInteger.One + y) * ModInverse(BigInteger.One - y));
        return ToLittleEndian(u);
    }

    private static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes)
    {
        var unsigned = new byte[bytes.Length + 1];
        bytes.CopyTo(unsigned);
        return new BigInteger(unsigned);
    }

    private static byte[] ToLittleEndian(BigInteger value)
    {
        var bytes = value.ToByteArray();
        Array.Resize(ref bytes, 32);
        return bytes;
    }

    private static BigInteger Mod(BigInteger value)
    {
        var result = value % Prime;
        return result.Sign < 0 ? result + Prime : result;
    }

    private static BigInteger ModInverse(BigInteger value)
    {
        return BigInteger.ModPow(Mod(value), Prime - 2, Prime);
    }
}