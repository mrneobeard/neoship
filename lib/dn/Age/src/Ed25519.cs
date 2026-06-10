using System.Numerics;
using System.Security.Cryptography;

namespace NeoBeard.Ssh;

internal static class Ed25519
{
    private static readonly BigInteger P = (BigInteger.One << 255) - 19;
    private static readonly BigInteger L = (BigInteger.One << 252) + BigInteger.Parse("27742317777372353535851937790883648493");
    private static readonly BigInteger D = Mod(-121665 * ModInverse(121666, P), P);
    private static readonly Point B = new(
        Mod(BigInteger.Parse("15112221349535400772501151409588531511454012693041857206046113283949847762202"), P),
        Mod(BigInteger.Parse("46316835694926478169428394003475163141307993866256225615783033603165251855960"), P));

    public static byte[] PublicKeyFromSeed(byte[] seed)
    {
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seeds must be 32 bytes.", nameof(seed));

        var h = SHA512.HashData(seed);
        return PublicKeyFromSeed(h.AsSpan(0, 32));
    }

    public static byte[] PublicKeyFromSeed(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seeds must be 32 bytes.", nameof(seed));

        var h = SHA512.HashData(seed);
        var a = Clamp(h.AsSpan(0, 32));
        return Encode(ScalarMult(a, B));
    }

    public static void PublicKeyFromSeed(ReadOnlySpan<byte> seed, Span<byte> publicKey)
    {
        if (publicKey.Length < 32)
            throw new ArgumentException("The public key output buffer is too small.", nameof(publicKey));

        PublicKeyFromSeed(seed).CopyTo(publicKey);
    }

    public static byte[] PublicKeyFromSeed(byte[] seed, int seedOffset)
    {
        return PublicKeyFromSeed(seed.AsSpan(seedOffset, 32));
    }

    public static void GeneratePublicKey(byte[] seed, int seedOffset, byte[] publicKey, int publicKeyOffset)
    {
        if (seed == null)
            throw new ArgumentNullException(nameof(seed));
        if (publicKey == null)
            throw new ArgumentNullException(nameof(publicKey));
        if (seedOffset < 0 || seedOffset > seed.Length - 32)
            throw new ArgumentOutOfRangeException(nameof(seedOffset), "The seed offset is out of range.");
        if (publicKeyOffset < 0 || publicKeyOffset > publicKey.Length - 32)
            throw new ArgumentOutOfRangeException(nameof(publicKeyOffset), "The public key offset is out of range.");

        var output = PublicKeyFromSeed(seed.AsSpan(seedOffset, 32));
        output.AsSpan().CopyTo(publicKey.AsSpan(publicKeyOffset, 32));
    }

    public static void GeneratePublicKey(ReadOnlySpan<byte> seed, Span<byte> publicKey)
    {
        PublicKeyFromSeed(seed, publicKey);
    }

    public static byte[] Sign(byte[] message, byte[] seed, byte[] publicKey)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));
        if (seed is null)
            throw new ArgumentNullException(nameof(seed));
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seeds must be 32 bytes.", nameof(seed));
        if (publicKey is null)
            throw new ArgumentNullException(nameof(publicKey));
        if (publicKey.Length != 32)
            throw new ArgumentException("Ed25519 public keys must be 32 bytes.", nameof(publicKey));

        var h = SHA512.HashData(seed);
        var a = Clamp(h.AsSpan(0, 32));
        var prefix = h.AsSpan(32, 32);
        var r = Hint(Concat(prefix, message));
        var rPoint = ScalarMult(r, B);
        var encodedR = Encode(rPoint);
        var k = Hint(Concat(encodedR, publicKey, message));
        var s = Mod(r + (k * a), L);

        var signature = new byte[64];
        encodedR.CopyTo(signature);
        ToLittleEndian(s, 32).CopyTo(signature.AsSpan(32));
        return signature;
    }

    private static BigInteger Clamp(ReadOnlySpan<byte> value)
    {
        if (value.Length != 32)
            throw new ArgumentException("Ed25519 scalar values must be 32 bytes.", nameof(value));

        Span<byte> bytes = stackalloc byte[32];
        value.CopyTo(bytes);
        bytes[0] &= 248;
        bytes[31] &= 63;
        bytes[31] |= 64;
        return FromLittleEndian(bytes);
    }

    private static BigInteger Hint(ReadOnlySpan<byte> value)
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

    private static BigInteger FromLittleEndian(ReadOnlySpan<byte> bytes)
    {
        var value = new byte[bytes.Length + 1];
        bytes.CopyTo(value);
        return new BigInteger(value, isUnsigned: true, isBigEndian: false);
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

    private static byte[] Concat(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        var output = new byte[first.Length + second.Length];
        first.CopyTo(output);
        second.CopyTo(output.AsSpan(first.Length));
        return output;
    }

    private static byte[] Concat(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second, ReadOnlySpan<byte> third)
    {
        var output = new byte[first.Length + second.Length + third.Length];
        first.CopyTo(output);
        var secondOffset = first.Length;
        second.CopyTo(output.AsSpan(secondOffset));
        third.CopyTo(output.AsSpan(secondOffset + second.Length));
        return output;
    }

    private readonly record struct Point(BigInteger xCoordinate, BigInteger yCoordinate)
    {
        public static Point Identity { get; } = new(BigInteger.Zero, BigInteger.One);

        public BigInteger XCoordinate { get; } = xCoordinate;

        public BigInteger YCoordinate { get; } = yCoordinate;
    }
}