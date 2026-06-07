using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Splits and combines secrets using Shamir secret sharing over GF(256).
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var shares = ShamirSecretSharing.Split("secret"u8.ToArray(), 3, 5);
/// var recovered = ShamirSecretSharing.Combine(shares.Take(3));
/// Assert.Equal("secret"u8.ToArray(), recovered);
/// </code>
/// </example>
/// </remarks>
public static class ShamirSecretSharing
{
    /// <summary>
    /// Splits a secret into shares.
    /// </summary>
    /// <param name="secret">The secret bytes.</param>
    /// <param name="threshold">The number of shares required for recovery.</param>
    /// <param name="shareCount">The number of shares to create.</param>
    /// <returns>The generated shares.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var shares = ShamirSecretSharing.Split("secret"u8.ToArray(), 2, 3);
    /// Assert.Equal(3, shares.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static ShamirShare[] Split(ReadOnlySpan<byte> secret, int threshold, int shareCount)
    {
        ValidateSplit(secret, threshold, shareCount);
        var coefficients = new byte[threshold];
        var shares = new ShamirShare[shareCount];
        for (var index = 0; index < shares.Length; index++)
        {
            shares[index] = new ShamirShare((byte)(index + 1), new byte[secret.Length]);
        }

        for (var byteIndex = 0; byteIndex < secret.Length; byteIndex++)
        {
            coefficients[0] = secret[byteIndex];
            RandomNumberGenerator.Fill(coefficients.AsSpan(1));
            foreach (var share in shares)
            {
                share.Value[byteIndex] = Evaluate(coefficients, share.Index);
            }
        }

        CryptographicOperations.ZeroMemory(coefficients);
        return shares;
    }

    /// <summary>
    /// Combines shares to recover the original secret.
    /// </summary>
    /// <param name="shares">The shares to combine.</param>
    /// <returns>The recovered secret bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var shares = ShamirSecretSharing.Split("secret"u8.ToArray(), 2, 3);
    /// var recovered = ShamirSecretSharing.Combine(shares.Take(2));
    /// Assert.Equal("secret"u8.ToArray(), recovered);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] Combine(IEnumerable<ShamirShare> shares)
    {
        ArgumentNullException.ThrowIfNull(shares);
        var shareArray = shares.ToArray();
        ValidateCombine(shareArray);
        var secret = new byte[shareArray[0].Value.Length];

        for (var byteIndex = 0; byteIndex < secret.Length; byteIndex++)
        {
            var value = 0;
            for (var i = 0; i < shareArray.Length; i++)
            {
                var xi = shareArray[i].Index;
                var basis = 1;
                for (var j = 0; j < shareArray.Length; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    var xj = shareArray[j].Index;
                    basis = Multiply(basis, Divide(xj, xi ^ xj));
                }

                value ^= Multiply(shareArray[i].Value[byteIndex], basis);
            }

            secret[byteIndex] = (byte)value;
        }

        return secret;
    }

    private static void ValidateSplit(ReadOnlySpan<byte> secret, int threshold, int shareCount)
    {
        if (secret.IsEmpty)
        {
            throw new ArgumentException("Secret must not be empty.", nameof(secret));
        }

        if (threshold < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be at least two.");
        }

        if (shareCount < threshold)
        {
            throw new ArgumentOutOfRangeException(nameof(shareCount), "Share count must be at least the threshold.");
        }

        if (shareCount > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(shareCount), "Share count must not exceed 255.");
        }
    }

    private static void ValidateCombine(ShamirShare[] shares)
    {
        if (shares.Length < 2)
        {
            throw new ArgumentException("At least two shares are required.", nameof(shares));
        }

        var length = shares[0].Value.Length;
        var seen = new bool[256];
        foreach (var share in shares)
        {
            if (share.Index == 0)
            {
                throw new ArgumentException("Share index must be non-zero.", nameof(shares));
            }

            if (seen[share.Index])
            {
                throw new ArgumentException("Duplicate share index.", nameof(shares));
            }

            seen[share.Index] = true;

            if (share.Value.Length != length)
            {
                throw new ArgumentException("All shares must have the same length.", nameof(shares));
            }
        }
    }

    private static byte Evaluate(ReadOnlySpan<byte> coefficients, byte x)
    {
        var result = 0;
        for (var i = coefficients.Length - 1; i >= 0; i--)
        {
            result = Multiply(result, x) ^ coefficients[i];
        }

        return (byte)result;
    }

    private static int Divide(int left, int right)
    {
        if (right == 0)
        {
            throw new DivideByZeroException();
        }

        return left == 0 ? 0 : Exp((Log(left) + 255 - Log(right)) % 255);
    }

    private static int Multiply(int left, int right) => left == 0 || right == 0 ? 0 : Exp((Log(left) + Log(right)) % 255);

    private static int Exp(int exponent)
    {
        var value = 1;
        for (var i = 0; i < exponent; i++)
        {
            value = MultiplyNoLut(value, 0x03);
        }

        return value;
    }

    private static int Log(int value)
    {
        var current = 1;
        for (var i = 0; i < 255; i++)
        {
            if (current == value)
            {
                return i;
            }

            current = MultiplyNoLut(current, 0x03);
        }

        throw new ArgumentOutOfRangeException(nameof(value), "Value is not in GF(256).");
    }

    private static int MultiplyNoLut(int left, int right)
    {
        var product = 0;
        var a = left;
        var b = right;
        while (b > 0)
        {
            if ((b & 1) != 0)
            {
                product ^= a;
            }

            a <<= 1;
            if ((a & 0x100) != 0)
            {
                a ^= 0x11B;
            }

            b >>= 1;
        }

        return product & 0xFF;
    }
}