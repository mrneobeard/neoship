using System.Numerics;
using System.Security.Cryptography;

namespace NeoBeard.Ssh;

internal static class Ed25519
{
    private static readonly BigInteger P = (BigInteger.One << 255) - 19;
    private static readonly BigInteger L = (BigInteger.One << 252) + BigInteger.Parse("27742317777372353535851937790883648493");
    private static readonly BigInteger D = Mod(-121665 * ModInverse(121666, P), P);
    private static readonly BigInteger I = BigInteger.ModPow(2, (P - 1) / 4, P);
    private static readonly Point B = new(
        Mod(BigInteger.Parse("15112221349535400772501151409588531511454012693041857206046113283949847762202"), P),
        Mod(BigInteger.Parse("46316835694926478169428394003475163141307993866256225615783033603165251855960"), P));

    public static byte[] PublicKeyFromSeed(byte[] seed)
    {
        var h = SHA512.HashData(seed);
        var a = Clamp(h[..32]);
        return Encode(ScalarMult(a, B));
    }

    public static byte[] Sign(byte[] message, byte[] seed, byte[] publicKey)
    {
        var h = SHA512.HashData(seed);
        var a = Clamp(h[..32]);
        var prefix = h[32..64];
        var r = Hint(Concat(prefix, message));
        var rPoint = ScalarMult(r, B);
        var encodedR = Encode(rPoint);
        var k = Hint(Concat(encodedR, publicKey, message));
        var s = Mod(r + (k * a), L);
        return Concat(encodedR, ToLittleEndian(s, 32));
    }

    private static BigInteger Clamp(byte[] value)
    {
        var bytes = value.ToArray();
        bytes[0] &= 248;
        bytes[31] &= 63;
        bytes[31] |= 64;
        return FromLittleEndian(bytes);
    }

    private static BigInteger Hint(byte[] value)
    {
        return Mod(FromLittleEndian(SHA512.HashData(value)), L);
    }

    private static Point ScalarMult(BigInteger scalar, Point point)
    {
        var result = Point.Identity;
        var addend = point;
        while (scalar > 0)
        {
            if (!scalar.IsEven)
                result = Add(result, addend);

            addend = Add(addend, addend);
            scalar >>= 1;
        }

        return result;
    }

    private static Point Add(Point p, Point q)
    {
        var x1 = p.XCoordinate;
        var y1 = p.YCoordinate;
        var x2 = q.XCoordinate;
        var y2 = q.YCoordinate;
        var xyxy = Mod(x1 * x2 * y1 * y2, P);
        var x = Mod(((x1 * y2) + (x2 * y1)) * ModInverse(1 + (D * xyxy), P), P);
        var y = Mod(((y1 * y2) + (x1 * x2)) * ModInverse(1 - (D * xyxy), P), P);
        return new Point(x, y);
    }

    private static byte[] Encode(Point point)
    {
        var bytes = ToLittleEndian(point.YCoordinate, 32);
        if (!point.XCoordinate.IsEven)
            bytes[31] |= 0x80;

        return bytes;
    }

    private static BigInteger FromLittleEndian(byte[] bytes)
    {
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }

    private static byte[] ToLittleEndian(BigInteger value, int length)
    {
        var raw = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        var output = new byte[length];
        raw.AsSpan(0, Math.Min(raw.Length, length)).CopyTo(output);
        return output;
    }

    private static BigInteger ModInverse(BigInteger value, BigInteger modulus)
    {
        return BigInteger.ModPow(Mod(value, modulus), modulus - 2, modulus);
    }

    private static BigInteger Mod(BigInteger value, BigInteger modulus)
    {
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        var length = arrays.Sum(static a => a.Length);
        var output = new byte[length];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, output, offset, array.Length);
            offset += array.Length;
        }

        return output;
    }

    private readonly record struct Point(BigInteger xCoordinate, BigInteger yCoordinate)
    {
        public static Point Identity { get; } = new(BigInteger.Zero, BigInteger.One);

        public BigInteger XCoordinate { get; } = xCoordinate;

        public BigInteger YCoordinate { get; } = yCoordinate;
    }
}