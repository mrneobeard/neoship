using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Computes BLAKE2s hashes with optional key, salt, personalization, and tree parameters.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var blake = new Blake2S(32);
/// var digest = blake.ComputeHash("abc"u8.ToArray());
/// Assert.Equal(32, digest.Length);
/// </code>
/// </example>
/// </remarks>
[SuppressMessage("", "CA1819:", Justification = "Byte arrays are required by HashAlgorithm-style crypto APIs.")]
public sealed class Blake2S : HashAlgorithm
{
    private const int Rounds = 10;
    private const int BufferSize = 64;
    private const int OutBytes = 32;
    private const int SaltBytes = 8;
    private const int PersonalizationBytes = 8;

    private static readonly uint[] IV =
    {
        0x6A09E667U, 0xBB67AE85U, 0x3C6EF372U, 0xA54FF53AU,
        0x510E527FU, 0x9B05688CU, 0x1F83D9ABU, 0x5BE0CD19U,
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
    };

    private readonly uint[] h = new uint[8];
    private readonly uint[] t = new uint[2];
    private readonly uint[] f = new uint[2];
    private readonly uint[] m = new uint[16];
    private readonly uint[] v = new uint[16];
    private readonly byte[] buffer = new byte[BufferSize];
    private readonly Blake2STreeConfig treeConfig;
    private readonly int hashLength;

    private byte[]? personalization;
    private byte[]? key;
    private byte[]? salt;
    private int bufferOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2S"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2S()
        : this(OutBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2S"/> class.
    /// </summary>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S(16);
    /// var digest = blake.ComputeHash("data"u8.ToArray());
    /// Assert.Equal(16, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2S(int hashLength)
        : this(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, hashLength)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2S"/> class.
    /// </summary>
    /// <param name="key">The optional key bytes.</param>
    /// <param name="salt">The optional 8-byte salt.</param>
    /// <param name="personalization">The optional 8-byte personalization value.</param>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S("secret"u8, ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, 32);
    /// var digest = blake.ComputeHash("message"u8.ToArray());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2S(ReadOnlySpan<byte> key, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> personalization, int hashLength = OutBytes)
        : this(key, salt, personalization, hashLength, Blake2STreeConfig.Sequential)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2S"/> class.
    /// </summary>
    /// <param name="key">The optional key bytes.</param>
    /// <param name="salt">The optional 8-byte salt.</param>
    /// <param name="personalization">The optional 8-byte personalization value.</param>
    /// <param name="hashLength">The digest length in bytes.</param>
    /// <param name="treeConfig">The tree hashing configuration.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tree = new Blake2STreeConfig(8, 2, 0, 0, 0, 32, false);
    /// using var blake = new Blake2S(ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, ReadOnlySpan&lt;byte&gt;.Empty, 32, tree);
    /// Assert.Equal(256, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2S(ReadOnlySpan<byte> key, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> personalization, int hashLength, Blake2STreeConfig treeConfig)
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
    /// Gets or sets the optional key.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
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

            this.key = value.Length == 0 ? null : value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Gets or sets the optional 8-byte salt.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// blake.Salt = new byte[8];
    /// Assert.Equal(8, blake.Salt.Length);
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

            this.salt = value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Gets or sets the optional 8-byte personalization value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// blake.Personalization = "personal!"u8.ToArray();
    /// Assert.Equal(8, blake.Personalization.Length);
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

            this.personalization = value.ToArray();
            this.Initialize();
        }
    }

    /// <summary>
    /// Resets the hash state.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// blake.Initialize();
    /// Assert.Equal(256, blake.HashSize);
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

        Span<byte> parameter = stackalloc byte[32];
        parameter[0] = (byte)this.hashLength;
        parameter[1] = (byte)(this.key?.Length ?? 0);
        parameter[2] = this.treeConfig.FanOut;
        parameter[3] = this.treeConfig.Depth;
        BinaryPrimitives.WriteUInt32LittleEndian(parameter[4..8], (uint)this.treeConfig.LeafLength);
        WriteUInt48LittleEndian(parameter[8..14], (ulong)this.treeConfig.NodeOffset);
        parameter[14] = this.treeConfig.NodeDepth;
        parameter[15] = (byte)this.treeConfig.InnerHashLength;
        this.salt?.CopyTo(parameter[16..24]);
        this.personalization?.CopyTo(parameter[24..32]);

        for (var i = 0; i < 8; i++)
        {
            this.h[i] ^= BinaryPrimitives.ReadUInt32LittleEndian(parameter[(i * 4)..]);
        }

        if (this.key is { Length: > 0 })
        {
            this.key.CopyTo(this.buffer, 0);
            this.bufferOffset = BufferSize;
        }
    }

    /// <summary>
    /// Routes input bytes into the BLAKE2s compression state.
    /// </summary>
    /// <param name="array">The input buffer.</param>
    /// <param name="ibStart">The starting offset in <paramref name="array"/>.</param>
    /// <param name="cbSize">The number of bytes to hash.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// blake.TransformBlock("abc"u8.ToArray(), 0, 3, null, 0);
    /// blake.TransformFinalBlock(Array.Empty&lt;byte&gt;(), 0, 0);
    /// Assert.Equal(32, blake.Hash?.Length);
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
    /// Finalizes the BLAKE2s digest.
    /// </summary>
    /// <returns>The computed digest.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake2S();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override byte[] HashFinal()
    {
        this.f[0] = uint.MaxValue;
        if (this.treeConfig.IsLastNode)
        {
            this.f[1] = uint.MaxValue;
        }

        Array.Clear(this.buffer, this.bufferOffset, BufferSize - this.bufferOffset);
        this.IncrementCount((uint)this.bufferOffset);
        this.Compress(this.buffer, 0);
        var result = new byte[OutBytes];
        for (var i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(i * 4), this.h[i]);
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

    private static void WriteUInt48LittleEndian(Span<byte> destination, ulong value)
    {
        destination[0] = (byte)value;
        destination[1] = (byte)(value >> 8);
        destination[2] = (byte)(value >> 16);
        destination[3] = (byte)(value >> 24);
        destination[4] = (byte)(value >> 32);
        destination[5] = (byte)(value >> 40);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IncrementCount(uint blocks)
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
        localV[d] = BitOperations.RotateRight(localV[d] ^ localV[a], 16);
        localV[c] += localV[d];
        localV[b] = BitOperations.RotateRight(localV[b] ^ localV[c], 12);
        localV[a] += localV[b] + localM[p1];
        localV[d] = BitOperations.RotateRight(localV[d] ^ localV[a], 8);
        localV[c] += localV[d];
        localV[b] = BitOperations.RotateRight(localV[b] ^ localV[c], 7);
    }

    private void Compress(byte[] bytes, int offset)
    {
        for (var i = 0; i < 16; i++)
        {
            this.m[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + (i * 4)));
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