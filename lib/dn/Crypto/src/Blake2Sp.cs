using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Computes BLAKE2sp, the eight-way parallel BLAKE2s variant.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var blake = new Blake2Sp();
/// var digest = blake.ComputeHash("abc"u8.ToArray());
/// Assert.Equal(32, digest.Length);
/// </code>
/// </example>
/// </remarks>
public sealed class Blake2Sp : HashAlgorithm
{
    private const int Parallelism = 8;
    private const int OutBytes = 32;
    private const int StripeLength = 64;

    private readonly MemoryStream[] leaves = Enumerable.Range(0, Parallelism).Select(static _ => new MemoryStream()).ToArray();
    private readonly int hashLength;
    private long bytesWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2Sp"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2Sp();
    /// Assert.Equal(256, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2Sp()
        : this(OutBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2Sp"/> class.
    /// </summary>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2Sp(16);
    /// var digest = blake.ComputeHash("data"u8.ToArray());
    /// Assert.Equal(16, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2Sp(int hashLength)
    {
        if (hashLength is < 1 or > OutBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(hashLength), $"Hash length must be between 1 and {OutBytes} bytes.");
        }

        this.hashLength = hashLength;
        this.HashSizeValue = hashLength * 8;
    }

    /// <summary>
    /// Resets the hash state.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2Sp();
    /// blake.Initialize();
    /// Assert.Equal(256, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public override void Initialize()
    {
        foreach (var leaf in this.leaves)
        {
            leaf.SetLength(0);
        }

        this.bytesWritten = 0;
    }

    /// <summary>
    /// Routes input bytes into BLAKE2sp leaf streams.
    /// </summary>
    /// <param name="array">The input buffer.</param>
    /// <param name="ibStart">The starting offset in <paramref name="array"/>.</param>
    /// <param name="cbSize">The number of bytes to hash.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2Sp();
    /// blake.TransformBlock("abc"u8.ToArray(), 0, 3, null, 0);
    /// blake.TransformFinalBlock(Array.Empty&lt;byte&gt;(), 0, 0);
    /// Assert.Equal(32, blake.Hash?.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ArgumentNullException.ThrowIfNull(array);
        for (var i = 0; i < cbSize; i++)
        {
            var absolute = this.bytesWritten + i;
            var leaf = (int)((absolute / StripeLength) % Parallelism);
            this.leaves[leaf].WriteByte(array[ibStart + i]);
        }

        this.bytesWritten += cbSize;
    }

    /// <summary>
    /// Finalizes the BLAKE2sp digest.
    /// </summary>
    /// <returns>The computed digest.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2Sp();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override byte[] HashFinal()
    {
        var leafHashes = new byte[Parallelism * OutBytes];
        for (var i = 0; i < Parallelism; i++)
        {
            var config = new Blake2STreeConfig(Parallelism, 2, 0, i, 0, OutBytes, i == Parallelism - 1);
            using var leaf = new Blake2S(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, OutBytes, config);
            leaf.ComputeHash(this.leaves[i].ToArray()).CopyTo(leafHashes.AsSpan(i * OutBytes));
        }

        var rootConfig = new Blake2STreeConfig(Parallelism, 2, 0, 0, 1, OutBytes, true);
        using var root = new Blake2S(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, this.hashLength, rootConfig);
        return root.ComputeHash(leafHashes);
    }

    /// <summary>
    /// Releases resources used by this instance.
    /// </summary>
    /// <param name="disposing"><c>true</c> when disposing managed resources.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var blake = new Blake2Sp();
    /// blake.Dispose();
    /// Assert.True(true);
    /// </code>
    /// </example>
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var leaf in this.leaves)
            {
                leaf.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}