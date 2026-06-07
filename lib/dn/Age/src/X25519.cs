using System.Numerics;

namespace NeoBeard.Age;

internal static class X25519
{
    private static readonly BigInteger Prime = (BigInteger.One << 255) - 19;

    public static byte[] ScalarMultBase(byte[] scalar)
    {
        Span<byte> basePoint = stackalloc byte[32];
        basePoint[0] = 9;
        return ScalarMult(scalar, basePoint);
    }

    public static byte[] ScalarMult(byte[] scalar, ReadOnlySpan<byte> point)
    {
        if (scalar.Length != 32 || point.Length != 32)
            throw new ArgumentException("X25519 inputs must be 32 bytes.");

        Span<byte> e = stackalloc byte[32];
        scalar.CopyTo(e);
        e[0] &= 248;
        e[31] &= 127;
        e[31] |= 64;

        Span<byte> uBytes = stackalloc byte[32];
        point.CopyTo(uBytes);
        uBytes[31] &= 127;
        var x1 = FromLittleEndian(uBytes);
        var x2 = BigInteger.One;
        var z2 = BigInteger.Zero;
        var x3 = x1;
        var z3 = BigInteger.One;
        var swap = 0;

        for (var t = 254; t >= 0; t--)
        {
            var kt = (e[t >> 3] >> (t & 7)) & 1;
            swap ^= kt;
            ConditionalSwap(swap, ref x2, ref x3);
            ConditionalSwap(swap, ref z2, ref z3);
            swap = kt;

            var a = Mod(x2 + z2);
            var aa = Mod(a * a);
            var b = Mod(x2 - z2);
            var bb = Mod(b * b);
            var eValue = Mod(aa - bb);
            var c = Mod(x3 + z3);
            var d = Mod(x3 - z3);
            var da = Mod(d * a);
            var cb = Mod(c * b);
            x3 = Mod((da + cb) * (da + cb));
            z3 = Mod(x1 * Mod((da - cb) * (da - cb)));
            x2 = Mod(aa * bb);
            z2 = Mod(eValue * Mod(aa + (new BigInteger(121665) * eValue)));
        }

        ConditionalSwap(swap, ref x2, ref x3);
        ConditionalSwap(swap, ref z2, ref z3);
        return ToLittleEndian(Mod(x2 * ModInverse(z2)));
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
        return BigInteger.ModPow(value, Prime - 2, Prime);
    }

    private static void ConditionalSwap(int swap, ref BigInteger left, ref BigInteger right)
    {
        if (swap == 0)
            return;

        (left, right) = (right, left);
    }
}