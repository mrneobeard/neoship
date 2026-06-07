using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Crypto;

/// <summary>
/// Computes BLAKE3 hashes.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// using var blake = new Blake3();
/// var digest = blake.ComputeHash("abc"u8.ToArray());
/// Assert.Equal(32, digest.Length);
/// </code>
/// </example>
/// </remarks>
public sealed class Blake3 : HashAlgorithm
{
    private const int BlockLength = 64;
    private const int ChunkLength = 1024;
    private const int OutLength = 32;
    private const uint ChunkStart = 1;
    private const uint ChunkEnd = 2;
    private const uint Parent = 4;
    private const uint Root = 8;
    private const uint KeyedHashFlag = 16;
    private const uint DeriveKeyContextFlag = 32;
    private const uint DeriveKeyMaterialFlag = 64;

    private static readonly uint[] IV =
    {
        0x6A09E667U, 0xBB67AE85U, 0x3C6EF372U, 0xA54FF53AU,
        0x510E527FU, 0x9B05688CU, 0x1F83D9ABU, 0x5BE0CD19U,
    };

    private static readonly byte[] MessagePermutation =
    {
        2, 6, 3, 10, 7, 0, 4, 13, 1, 11, 12, 5, 9, 14, 15, 8,
    };

    private readonly MemoryStream data = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake3"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake3();
    /// Assert.Equal(256, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake3()
    {
        this.HashSizeValue = OutLength * 8;
    }

    /// <summary>
    /// Computes a BLAKE3 hash.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <param name="outputLength">The output length in bytes.</param>
    /// <returns>The computed hash bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var digest = Blake3.HashData("abc"u8, 32);
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] HashData(ReadOnlySpan<byte> data, int outputLength = OutLength)
        => HashInternal(data, IV, 0, outputLength);

    /// <summary>
    /// Computes a keyed BLAKE3 hash.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="data">The input data.</param>
    /// <param name="outputLength">The output length in bytes.</param>
    /// <returns>The computed keyed hash bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = "whats the Elvish word for friend"u8.ToArray();
    /// var digest = Blake3.KeyedHash(key, "abc"u8, 32);
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] KeyedHash(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, int outputLength = OutLength)
    {
        if (key.Length != OutLength)
        {
            throw new ArgumentOutOfRangeException(nameof(key), "BLAKE3 keys must be 32 bytes.");
        }

        Span<uint> keyWords = stackalloc uint[8];
        WordsFromBytes(key, keyWords);
        return HashInternal(data, keyWords, KeyedHashFlag, outputLength);
    }

    /// <summary>
    /// Derives key material with BLAKE3.
    /// </summary>
    /// <param name="context">The derivation context string.</param>
    /// <param name="keyMaterial">The input key material.</param>
    /// <param name="outputLength">The output length in bytes.</param>
    /// <returns>The derived key bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var digest = Blake3.DeriveKey("example context", "material"u8, 32);
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] DeriveKey(string context, ReadOnlySpan<byte> keyMaterial, int outputLength = OutLength)
    {
        ArgumentNullException.ThrowIfNull(context);
        var contextKey = HashInternal(Encoding.UTF8.GetBytes(context), IV, DeriveKeyContextFlag, OutLength);
        Span<uint> keyWords = stackalloc uint[8];
        WordsFromBytes(contextKey, keyWords);
        return HashInternal(keyMaterial, keyWords, DeriveKeyMaterialFlag, outputLength);
    }

    /// <summary>
    /// Resets the hash state.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake3();
    /// blake.Initialize();
    /// Assert.Equal(256, blake.HashSize);
    /// </code>
    /// </example>
    /// </remarks>
    public override void Initialize() => this.data.SetLength(0);

    /// <summary>
    /// Routes input bytes into the BLAKE3 state.
    /// </summary>
    /// <param name="array">The input buffer.</param>
    /// <param name="ibStart">The starting offset in <paramref name="array"/>.</param>
    /// <param name="cbSize">The number of bytes to hash.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake3();
    /// blake.TransformBlock("abc"u8.ToArray(), 0, 3, null, 0);
    /// blake.TransformFinalBlock(Array.Empty&lt;byte&gt;(), 0, 0);
    /// Assert.Equal(32, blake.Hash?.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override void HashCore(byte[] array, int ibStart, int cbSize) => this.data.Write(array, ibStart, cbSize);

    /// <summary>
    /// Finalizes the BLAKE3 digest.
    /// </summary>
    /// <returns>The computed digest.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var blake = new Blake3();
    /// var digest = blake.ComputeHash(Array.Empty&lt;byte&gt;());
    /// Assert.Equal(32, digest.Length);
    /// </code>
    /// </example>
    /// </remarks>
    protected override byte[] HashFinal() => HashData(this.data.ToArray());

    /// <summary>
    /// Releases resources used by this instance.
    /// </summary>
    /// <param name="disposing"><c>true</c> when disposing managed resources.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var blake = new Blake3();
    /// blake.Dispose();
    /// Assert.True(true);
    /// </code>
    /// </example>
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.data.Dispose();
        }

        base.Dispose(disposing);
    }

    private static byte[] HashInternal(ReadOnlySpan<byte> data, ReadOnlySpan<uint> key, uint flags, int outputLength)
    {
        if (outputLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(outputLength), "Output length must be positive.");
        }

        var chunkCount = Math.Max(1, (data.Length + ChunkLength - 1) / ChunkLength);
        var outputs = new Output[chunkCount];
        var cvs = new uint[chunkCount][];
        for (var i = 0; i < chunkCount; i++)
        {
            var offset = i * ChunkLength;
            var length = Math.Min(ChunkLength, data.Length - offset);
            outputs[i] = ChunkOutput(data.Slice(offset, length), key, (ulong)i, flags);
            cvs[i] = outputs[i].ChainingValue();
        }

        var root = chunkCount == 1 ? outputs[0] : ParentOutput(cvs, 0, chunkCount, key, flags);
        return root.RootBytes(outputLength);
    }

    private static Output ChunkOutput(ReadOnlySpan<byte> chunk, ReadOnlySpan<uint> key, ulong counter, uint flags)
    {
        Span<uint> cv = stackalloc uint[8];
        key.CopyTo(cv);
        Span<uint> blockWords = stackalloc uint[16];

        var blockCount = Math.Max(1, (chunk.Length + BlockLength - 1) / BlockLength);
        for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            var offset = blockIndex * BlockLength;
            var blockLength = Math.Min(BlockLength, chunk.Length - offset);
            blockWords.Clear();
            WordsFromBytes(chunk.Slice(offset, blockLength), blockWords);
            var blockFlags = flags;
            if (blockIndex == 0)
            {
                blockFlags |= ChunkStart;
            }

            if (blockIndex == blockCount - 1)
            {
                blockFlags |= ChunkEnd;
                return new Output(cv.ToArray(), blockWords.ToArray(), counter, (uint)blockLength, blockFlags);
            }

            Compress(cv, blockWords, counter, (uint)blockLength, blockFlags).AsSpan(0, 8).CopyTo(cv);
        }

        throw new CryptographicException("BLAKE3 chunk hashing failed.");
    }

    private static Output ParentOutput(uint[][] cvs, int start, int count, ReadOnlySpan<uint> key, uint flags)
    {
        var split = LargestPowerOfTwoLessThan(count);
        var left = SubtreeChainingValue(cvs, start, split, key, flags);
        var right = SubtreeChainingValue(cvs, start + split, count - split, key, flags);
        return ParentOutput(left, right, key, flags);
    }

    private static uint[] SubtreeChainingValue(uint[][] cvs, int start, int count, ReadOnlySpan<uint> key, uint flags)
    {
        if (count == 1)
        {
            return cvs[start];
        }

        return ParentOutput(cvs, start, count, key, flags).ChainingValue();
    }

    private static Output ParentOutput(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, ReadOnlySpan<uint> key, uint flags)
    {
        var block = new uint[16];
        left.CopyTo(block.AsSpan(0, 8));
        right.CopyTo(block.AsSpan(8, 8));
        return new Output(key.ToArray(), block, 0, BlockLength, flags | Parent);
    }

    private static int LargestPowerOfTwoLessThan(int value)
    {
        var power = 1;
        while ((power << 1) < value)
        {
            power <<= 1;
        }

        return power;
    }

    private static uint[] Compress(ReadOnlySpan<uint> cv, ReadOnlySpan<uint> block, ulong counter, uint blockLength, uint flags)
    {
        Span<uint> state = stackalloc uint[16];
        cv.CopyTo(state);
        IV.CopyTo(state[8..]);
        state[12] = (uint)counter;
        state[13] = (uint)(counter >> 32);
        state[14] = blockLength;
        state[15] = flags;
        Span<uint> message = stackalloc uint[16];
        block.CopyTo(message);

        for (var round = 0; round < 7; round++)
        {
            Round(state, message);
            if (round != 6)
            {
                Permute(message);
            }
        }

        var output = new uint[16];
        for (var i = 0; i < 8; i++)
        {
            output[i] = state[i] ^ state[i + 8];
            output[i + 8] = state[i + 8] ^ cv[i];
        }

        return output;
    }

    private static void Round(Span<uint> state, ReadOnlySpan<uint> message)
    {
        G(state, 0, 4, 8, 12, message[0], message[1]);
        G(state, 1, 5, 9, 13, message[2], message[3]);
        G(state, 2, 6, 10, 14, message[4], message[5]);
        G(state, 3, 7, 11, 15, message[6], message[7]);
        G(state, 0, 5, 10, 15, message[8], message[9]);
        G(state, 1, 6, 11, 12, message[10], message[11]);
        G(state, 2, 7, 8, 13, message[12], message[13]);
        G(state, 3, 4, 9, 14, message[14], message[15]);
    }

    private static void G(Span<uint> state, int a, int b, int c, int d, uint mx, uint my)
    {
        state[a] = state[a] + state[b] + mx;
        state[d] = BitOperations.RotateRight(state[d] ^ state[a], 16);
        state[c] += state[d];
        state[b] = BitOperations.RotateRight(state[b] ^ state[c], 12);
        state[a] = state[a] + state[b] + my;
        state[d] = BitOperations.RotateRight(state[d] ^ state[a], 8);
        state[c] += state[d];
        state[b] = BitOperations.RotateRight(state[b] ^ state[c], 7);
    }

    private static void Permute(Span<uint> message)
    {
        Span<uint> copy = stackalloc uint[16];
        message.CopyTo(copy);
        for (var i = 0; i < 16; i++)
        {
            message[i] = copy[MessagePermutation[i]];
        }
    }

    private static void WordsFromBytes(ReadOnlySpan<byte> bytes, Span<uint> words)
    {
        var fullWords = bytes.Length / sizeof(uint);
        for (var i = 0; i < fullWords; i++)
        {
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(i * sizeof(uint)) ..]);
        }

        var remaining = bytes.Length % sizeof(uint);
        if (remaining > 0)
        {
            Span<byte> last = stackalloc byte[sizeof(uint)];
            bytes[(fullWords * sizeof(uint)) ..].CopyTo(last);
            words[fullWords] = BinaryPrimitives.ReadUInt32LittleEndian(last);
        }
    }

    private readonly struct Output
    {
        private readonly uint[] inputChainingValue;
        private readonly uint[] blockWords;
        private readonly ulong counter;
        private readonly uint blockLength;
        private readonly uint flags;

        internal Output(uint[] inputChainingValue, uint[] blockWords, ulong counter, uint blockLength, uint flags)
        {
            this.inputChainingValue = inputChainingValue;
            this.blockWords = blockWords;
            this.counter = counter;
            this.blockLength = blockLength;
            this.flags = flags;
        }

        internal uint[] ChainingValue() => Compress(this.inputChainingValue, this.blockWords, this.counter, this.blockLength, this.flags)[..8];

        internal byte[] RootBytes(int outputLength)
        {
            var result = new byte[outputLength];
            var offset = 0;
            var outputBlock = 0UL;
            Span<byte> block = stackalloc byte[64];
            while (offset < outputLength)
            {
                var words = Compress(this.inputChainingValue, this.blockWords, outputBlock, this.blockLength, this.flags | Root);
                for (var i = 0; i < 16; i++)
                {
                 
                    BinaryPrimitives.WriteUInt32LittleEndian(block[(i * sizeof(uint)) ..], words[i]);
                }

                var take = Math.Min(64, outputLength - offset);
                block[..take].CopyTo(result.AsSpan(offset));
                offset += take;
                outputBlock++;
            }

            return result;
        }
    }
}