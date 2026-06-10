using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Computes BLAKE2b hashes with optional key, salt, personalization, and tree parameters.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var blake = new Blake2B(64);
/// var digest = blake.ComputeHash("abc"u8.ToArray());
/// Assert.Equal(64, digest.Length);
/// </code>
/// </example>
/// </remarks>
// https://blake2.net/blake2.pdf
[SuppressMessage("", "CA1819:", Justification = "Byte arrays are required by HashAlgorithm-style crypto APIs.")]
public sealed class Blake2B : HashAlgorithm
{
    private const int Rounds = 12;
    private const int BufferSize = 128;
    private const int OutBytes = 64;
    private const int SaltBytes = 16;
    private const int PersonalizationBytes = 16;

    private static readonly ulong[] IV =
    {
        0x6A09E667F3BCC908UL, 0xBB67AE8584CAA73BUL, 0x3C6EF372FE94F82BUL, 0xA54FF53A5F1D36F1UL,
        0x510E527FADE682D1UL, 0x9B05688C2B3E6C1FUL, 0x1F83D9ABFB41BD6BUL, 0x5BE0CD19137E2179UL,
    };

    private static readonly byte[,] Sigma =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
        { 14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3 },
        { 11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4 },
        { 7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8 },
        { 9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13 },
        { 2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9 },
        { 12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11 },
        { 13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10 },
        { 6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5 },
        { 10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0 },
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
        { 14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3 },
    };

    private readonly ulong[] h = new ulong[8];
    private readonly ulong[] t = new ulong[2];
    private readonly ulong[] f = new ulong[2];
    private readonly ulong[] m = new ulong[16];
    private readonly ulong[] v = new ulong[16];
    private readonly byte[] buffer = new byte[BufferSize];
    private readonly Blake2BTreeConfig treeConfig;
    private readonly int hashLength;

    private byte[]? personalization;
    private byte[]? key;
    private byte[]? salt;
    private int bufferOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2B"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(64, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2B()
        : this(OutBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2B"/> class.
    /// </summary>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B(32);
    /// var digest = blake.ComputeHash("data"u8.ToArray());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2B(int hashLength)
        : this(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, hashLength)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2B"/> class.
    /// </summary>
    /// <param name="key">The optional key bytes.</param>
    /// <param name="salt">The optional 16-byte salt.</param>
    /// <param name="personalization">The optional 16-byte personalization value.</param>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B("secret"u8, ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, 32);
    /// var digest = blake.ComputeHash("message"u8.ToArray());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2B(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> personalization,
        int hashLength = OutBytes)
        : this(key, salt, personalization, hashLength, Blake2BTreeConfig.Sequential)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2B"/> class.
    /// </summary>
    /// <param name="key">The optional key bytes.</param>
    /// <param name="salt">The optional 16-byte salt.</param>
    /// <param name="personalization">The optional 16-byte personalization value.</param>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <param name="treeConfig">The tree hashing configuration.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tree = new Blake2BTreeConfig(2, 2, 1024, 0, 0, 64, false);
    /// using var blake = new Blake2B(ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, 64, tree);
    /// Assert.Equal(512, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2B(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> personalization,
        int hashLength,
        Blake2BTreeConfig treeConfig)
    {
        ValidateHashLength(hashLength);

        if (key.Length > BufferSize)
        {
            throw new ArgumentOutOfRangeException(nameof(key), $"Key must not exceed {BufferSize} bytes.");
        }

        if (!salt.IsEmpty && salt.Length != SaltBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(salt), $"Salt must be {SaltBytes} bytes.");
        }

        if (!personalization.IsEmpty && personalization.Length != PersonalizationBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(personalization), $"Personalization must be {PersonalizationBytes} bytes.");
        }

        this.key = key.IsEmpty ? null : key.ToArray();
        this.salt = salt.IsEmpty ? null : salt.ToArray();
        this.personalization = personalization.IsEmpty ? null : personalization.ToArray();
        this.hashLength = hashLength;
        this.treeConfig = treeConfig;
        this.HashSizeValue = hashLength * 8;
        this.Initialize();
    }

    /// <summary>
    /// Gets or sets the optional 16-byte salt.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// blake.Salt = new byte[16];
    /// Assert.Equal(16, blake.Salt.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Salt
    {
        get => this.salt is null ? Array.Empty<byte>() : this.salt.ToArray();

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length != SaltBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Salt must be {SaltBytes} bytes.");
            }

            if (this.salt?.SequenceEqual(value) == true)
            {
                return;
            }

            this.salt = value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Gets or sets the optional key.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// blake.Key = "secret"u8.ToArray();
    /// Assert.Equal(6, blake.Key.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Key
    {
        get => this.key is null ? Array.Empty<byte>() : this.key.ToArray();

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length > BufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Key must not exceed {BufferSize} bytes.");
            }

            if (this.key?.SequenceEqual(value) == true)
            {
                return;
            }

            this.key = value.Length == 0 ? null : value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Gets or sets the optional 16-byte personalization value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// blake.Personalization = "personalization!"u8.ToArray();
    /// Assert.Equal(16, blake.Personalization.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Personalization
    {
        get => this.personalization is null ? Array.Empty<byte>() : this.personalization.ToArray();

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length != PersonalizationBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Personalization must be {PersonalizationBytes} bytes.");
            }

            if (this.personalization?.SequenceEqual(value) == true)
            {
                return;
            }

            this.personalization = value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Computes a two-level BLAKE2b tree hash.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="hashLength">The final digest length in bytes.</param>
    /// <param name="leafLength">The maximum leaf size in bytes.</param>
    /// <param name="fanOut">The maximum number of leaf hashes.</param>
    /// <returns>The computed tree digest.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var digest = Blake2B.ComputeTreeHash("message"u8, 64, 4, 2);
    /// Assert.Equal(64, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] ComputeTreeHash(ReadOnlySpan<byte> data, int hashLength, int leafLength, byte fanOut)
    {
        ValidateHashLength(hashLength);

        if (leafLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leafLength), "Leaf length must be positive.");
        }

        if (fanOut <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(fanOut), "Fan-out must be greater than one.");
        }

        var leafCount = Math.Max(1, (data.Length + leafLength - 1) / leafLength);
        if (leafCount > fanOut)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Data requires more leaves than the configured fan-out.");
        }

        var leafHashes = new byte[leafCount * OutBytes];
        for (var i = 0; i < leafCount; i++)
        {
            var offset = i * leafLength;
            var length = Math.Min(leafLength, data.Length - offset);
            var config = new Blake2BTreeConfig(fanOut, 2, leafLength, i, 0, OutBytes, i == leafCount - 1);
            using var leaf = new Blake2B(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, OutBytes, config);
            var hash = leaf.ComputeHash(data.Slice(offset, length).ToArray());
            hash.CopyTo(leafHashes.AsSpan(i * OutBytes));
        }

        var rootConfig = new Blake2BTreeConfig(fanOut, 2, leafLength, 0, 1, OutBytes, true);
        using var root = new Blake2B(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, hashLength, rootConfig);
        return root.ComputeHash(leafHashes);
    }

    /// <summary>
    /// Resets the hash state.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// blake.Initialize();
    /// Assert.Equal(512, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public override void Initialize()
    {
        Array.Copy(IV, this.h, IV.Length);
        Array.Clear(this.t);
        Array.Clear(this.f);
        Array.Clear(this.m);
        Array.Clear(this.v);
        Array.Clear(this.buffer);
        this.bufferOffset = 0;

        Span<byte> parameter = stackalloc byte[64];
        parameter[0] = (byte)this.hashLength;
        parameter[1] = (byte)(this.key?.Length ?? 0);
        parameter[2] = this.treeConfig.FanOut;
        parameter[3] = this.treeConfig.Depth;
        BinaryPrimitives.WriteUInt32LittleEndian(parameter[4..8], (uint)this.treeConfig.LeafLength);
        BinaryPrimitives.WriteUInt64LittleEndian(parameter[8..16], (ulong)this.treeConfig.NodeOffset);
        parameter[16] = this.treeConfig.NodeDepth;
        parameter[17] = (byte)this.treeConfig.InnerHashLength;

        if (this.salt is { Length: SaltBytes })
        {
            this.salt.CopyTo(parameter[32..48]);
        }

        if (this.personalization is { Length: PersonalizationBytes })
        {
            this.personalization.CopyTo(parameter[48..64]);
        }

        for (var i = 0; i < 8; i++)
        {
            this.h[i] ^= BinaryPrimitives.ReadUInt64LittleEndian(parameter[(i * 8)..]);
        }

        if (this.key is { Length: > 0 })
        {
            this.key.CopyTo(this.buffer, 0);
            this.bufferOffset = BufferSize;
        }
    }

    /// <summary>
    /// Routes input bytes into the BLAKE2b compression state.
    /// </summary>
    /// <param name="array">The input buffer.</param>
    /// <param name="ibStart">The starting offset in <paramref name="array"/>.</param>
    /// <param name="cbSize">The number of bytes to hash.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// blake.TransformBlock("abc"u8.ToArray(), 0, 3, null, 0);
    /// blake.TransformFinalBlock(Array.Empty&lt;byte&gt;(), 0, 0);
    /// Assert.Equal(64, blake.Hash?.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ArgumentNullException.ThrowIfNull(array);
        var remaining = cbSize;
        var offset = ibStart;

        if (this.bufferOffset != 0)
        {
            var fill = BufferSize - this.bufferOffset;
            if (remaining <= fill)
            {
                Buffer.BlockCopy(array, offset, this.buffer, this.bufferOffset, remaining);
                this.bufferOffset += remaining;
                return;
            }

            Buffer.BlockCopy(array, offset, this.buffer, this.bufferOffset, fill);
            this.IncrementCount(BufferSize);
            this.Compress(this.buffer, 0);
            this.bufferOffset = 0;
            offset += fill;
            remaining -= fill;
        }

        while (remaining > BufferSize)
        {
            this.IncrementCount(BufferSize);
            this.Compress(array, offset);
            offset += BufferSize;
            remaining -= BufferSize;
        }

        if (remaining > 0)
        {
            Buffer.BlockCopy(array, offset, this.buffer, 0, remaining);
            this.bufferOffset = remaining;
        }
    }

    /// <summary>
    /// Finalizes the BLAKE2b digest.
    /// </summary>
    /// <returns>The computed digest.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2B();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(64, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override byte[] HashFinal()
    {
        this.f[0] = ulong.MaxValue;
        if (this.treeConfig.IsLastNode)
        {
            this.f[1] = ulong.MaxValue;
        }

        Array.Clear(this.buffer, this.bufferOffset, BufferSize - this.bufferOffset);
        this.IncrementCount((ulong)this.bufferOffset);
        this.Compress(this.buffer, 0);

        var result = new byte[OutBytes];
        for (var i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(i * 8), this.h[i]);
        }

        return this.hashLength == OutBytes ? result : result.AsSpan(0, this.hashLength).ToArray();
    }

    private static void ValidateHashLength(int length)
    {
        if (length is < 1 or > OutBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"Hash length must be between 1 and {OutBytes} bytes.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementCount(ulong blocks)
    {
        this.t[0] += blocks;
        if (this.t[0] < blocks)
        {
            this.t[1]++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void G(int a, int b, int c, int d, int r, int i)
    {
        var p0 = Sigma[r, i];
        var p1 = Sigma[r, i + 1];
        var localV = this.v;
        var localM = this.m;

        localV[a] += localV[b] + localM[p0];
        localV[d] = BitOperations.RotateRight(localV[d] ^ localV[a], 32);
        localV[c] += localV[d];
        localV[b] = BitOperations.RotateRight(localV[b] ^ localV[c], 24);
        localV[a] += localV[b] + localM[p1];
        localV[d] = BitOperations.RotateRight(localV[d] ^ localV[a], 16);
        localV[c] += localV[d];
        localV[b] = BitOperations.RotateRight(localV[b] ^ localV[c], 63);
    }

    private void Compress(byte[] bytes, int offset)
    {
        for (var i = 0; i < 16; i++)
        {
            this.m[i] = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset + (i * 8)));
        }

        Array.Copy(this.h, this.v, 8);
        this.v[8] = IV[0];
        this.v[9] = IV[1];
        this.v[10] = IV[2];
        this.v[11] = IV[3];
        this.v[12] = IV[4] ^ this.t[0];
        this.v[13] = IV[5] ^ this.t[1];
        this.v[14] = IV[6] ^ this.f[0];
        this.v[15] = IV[7] ^ this.f[1];

        for (var r = 0; r < Rounds; r++)
        {
            this.G(0, 4, 8, 12, r, 0);
            this.G(1, 5, 9, 13, r, 2);
            this.G(2, 6, 10, 14, r, 4);
            this.G(3, 7, 11, 15, r, 6);
            this.G(0, 5, 10, 15, r, 8);
            this.G(1, 6, 11, 12, r, 10);
            this.G(2, 7, 8, 13, r, 12);
            this.G(3, 4, 9, 14, r, 14);
        }

        for (var i = 0; i < 8; i++)
        {
            this.h[i] ^= this.v[i] ^ this.v[i + 8];
        }
    }
}