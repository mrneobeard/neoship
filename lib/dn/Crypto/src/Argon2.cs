using System.Buffers.Binary;
using System.Numerics;

namespace NeoBeard.Crypto;

/// <summary>
/// Provides Argon2 key derivation helpers.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var parameters = new Argon2Parameters { MemorySizeKiB = 8192, Iterations = 2, DegreeOfParallelism = 1, TagLength = 32 };
/// var key = Argon2.DeriveKey("password"u8, "1234567890123456"u8, parameters);
/// Assert.Equal(32, key.Length);
/// </code>
/// </example>
/// </remarks>
public static class Argon2
{
    private const int Version = 0x13;
    private const int SyncPoints = 4;
    private const int BlockBytes = 1024;
    private const int BlockWords = 128;

    /// <summary>
    /// Derives key material from a password and salt using Argon2.
    /// </summary>
    /// <param name="password">The password bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="parameters">The derivation parameters.</param>
    /// <returns>The derived key bytes.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { MemorySizeKiB = 8192, Iterations = 2, DegreeOfParallelism = 1, TagLength = 32 };
    /// var key = Argon2.DeriveKey("password"u8, "1234567890123456"u8, parameters);
    /// Assert.Equal(32, key.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static byte[] DeriveKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Argon2Parameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Validate(parameters, salt);

        var lanes = parameters.DegreeOfParallelism;
        var memoryBlocks = parameters.MemorySizeKiB / (SyncPoints * lanes) * (SyncPoints * lanes);
        var laneLength = memoryBlocks / lanes;
        var segmentLength = laneLength / SyncPoints;
        var memory = new ulong[memoryBlocks * BlockWords];
        var h0 = InitialHash(password, salt, parameters, memoryBlocks);

        InitializeMemory(memory, h0, lanes, laneLength);
        FillMemory(memory, lanes, laneLength, segmentLength, parameters);
        return Finalize(memory, lanes, laneLength, parameters.TagLength);
    }

    private static void Validate(Argon2Parameters parameters, ReadOnlySpan<byte> salt)
    {
        if (!Enum.IsDefined(parameters.Variant))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Unsupported Argon2 variant.");
        }

        if (parameters.Iterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Iterations must be positive.");
        }

        if (parameters.DegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Degree of parallelism must be positive.");
        }

        if (parameters.MemorySizeKiB < 8 * parameters.DegreeOfParallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Memory size must be at least 8 KiB per lane.");
        }

        if (parameters.TagLength < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Tag length must be at least 4 bytes.");
        }

        if (salt.IsEmpty)
        {
            throw new ArgumentException("Salt must not be empty.", nameof(salt));
        }
    }

    private static byte[] InitialHash(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Argon2Parameters parameters, int memoryBlocks)
    {
        var associatedData = parameters.AssociatedData ?? Array.Empty<byte>();
        var knownSecret = parameters.KnownSecret ?? Array.Empty<byte>();
        var length = (10 * sizeof(int)) + password.Length + salt.Length + knownSecret.Length + associatedData.Length;
        var input = new byte[length];
        var offset = 0;

        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), parameters.DegreeOfParallelism);
        offset += sizeof(int);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), parameters.TagLength);
        offset += sizeof(int);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), memoryBlocks);
        offset += sizeof(int);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), parameters.Iterations);
        offset += sizeof(int);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), Version);
        offset += sizeof(int);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(offset), (int)parameters.Variant);
        offset += sizeof(int);
        offset = WriteLengthPrefixed(password, input, offset);
        offset = WriteLengthPrefixed(salt, input, offset);
        offset = WriteLengthPrefixed(knownSecret, input, offset);
        WriteLengthPrefixed(associatedData, input, offset);

        using var blake = new Blake2B();
        return blake.ComputeHash(input);
    }

    private static int WriteLengthPrefixed(ReadOnlySpan<byte> value, byte[] destination, int offset)
    {
        BinaryPrimitives.WriteInt32LittleEndian(destination.AsSpan(offset), value.Length);
        offset += sizeof(int);
        value.CopyTo(destination.AsSpan(offset));
        return offset + value.Length;
    }

    private static void InitializeMemory(ulong[] memory, byte[] h0, int lanes, int laneLength)
    {
        var blockInput = new byte[h0.Length + (2 * sizeof(int))];
        h0.CopyTo(blockInput.AsSpan());

        for (var lane = 0; lane < lanes; lane++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(blockInput.AsSpan(h0.Length), 0);
            BinaryPrimitives.WriteInt32LittleEndian(blockInput.AsSpan(h0.Length + sizeof(int)), lane);
            LoadBlock(Hash(blockInput, BlockBytes), memory, (lane * laneLength) + 0);

            BinaryPrimitives.WriteInt32LittleEndian(blockInput.AsSpan(h0.Length), 1);
            BinaryPrimitives.WriteInt32LittleEndian(blockInput.AsSpan(h0.Length + sizeof(int)), lane);
            LoadBlock(Hash(blockInput, BlockBytes), memory, (lane * laneLength) + 1);
        }
    }

    private static void FillMemory(ulong[] memory, int lanes, int laneLength, int segmentLength, Argon2Parameters parameters)
    {
        var zero = new ulong[BlockWords];
        var inputBlock = new ulong[BlockWords];
        var addressBlock = new ulong[BlockWords];

        for (var pass = 0; pass < parameters.Iterations; pass++)
        {
            for (var slice = 0; slice < SyncPoints; slice++)
            {
                for (var lane = 0; lane < lanes; lane++)
                {
                    FillSegment(memory, zero, inputBlock, addressBlock, lane, slice, pass, lanes, laneLength, segmentLength, parameters);
                }
            }
        }
    }

    private static void FillSegment(
        ulong[] memory,
        ulong[] zero,
        ulong[] inputBlock,
        ulong[] addressBlock,
        int lane,
        int slice,
        int pass,
        int lanes,
        int laneLength,
        int segmentLength,
        Argon2Parameters parameters)
    {
        var startIndex = pass == 0 && slice == 0 ? 2 : 0;
        var useDataIndependentAddressing = parameters.Variant == Argon2Variant.Argon2i
            || (parameters.Variant == Argon2Variant.Argon2id && pass == 0 && slice < 2);

        if (useDataIndependentAddressing)
        {
            InitializeAddressInput(inputBlock, pass, lane, slice, lanes * laneLength, parameters.Iterations, parameters.Variant);
        }

        for (var index = startIndex; index < segmentLength; index++)
        {
            ulong pseudoRandom;
            if (useDataIndependentAddressing)
            {
                if ((index - startIndex) % BlockWords == 0)
                {
                    inputBlock[6]++;
                    Compress(zero, inputBlock, addressBlock);
                    Compress(zero, addressBlock, addressBlock);
                }

                pseudoRandom = addressBlock[index % BlockWords];
            }
            else
            {
                var currentColumn = (slice * segmentLength) + index;
                var previousColumn = currentColumn == 0 ? laneLength - 1 : currentColumn - 1;
                pseudoRandom = memory[BlockOffset((lane * laneLength) + previousColumn)];
            }

            FillBlock(memory, lane, slice, pass, index, lanes, laneLength, segmentLength, pseudoRandom);
        }
    }

    private static void InitializeAddressInput(
        ulong[] inputBlock,
        int pass,
        int lane,
        int slice,
        int memoryBlocks,
        int iterations,
        Argon2Variant variant)
    {
        Array.Clear(inputBlock);
        inputBlock[0] = (ulong)pass;
        inputBlock[1] = (ulong)lane;
        inputBlock[2] = (ulong)slice;
        inputBlock[3] = (ulong)memoryBlocks;
        inputBlock[4] = (ulong)iterations;
        inputBlock[5] = (ulong)variant;
    }

    private static void FillBlock(
        ulong[] memory,
        int lane,
        int slice,
        int pass,
        int index,
        int lanes,
        int laneLength,
        int segmentLength,
        ulong pseudoRandom)
    {
        var currentColumn = (slice * segmentLength) + index;
        var previousColumn = currentColumn == 0 ? laneLength - 1 : currentColumn - 1;
        var referenceLane = pass == 0 && slice == 0 ? lane : (int)((pseudoRandom >> 32) % (uint)lanes);
        var referenceColumn = GetReferenceColumn(pseudoRandom, referenceLane == lane, pass, slice, index, laneLength, segmentLength);
        var previousBlock = (lane * laneLength) + previousColumn;
        var referenceBlock = (referenceLane * laneLength) + referenceColumn;
        var currentBlock = (lane * laneLength) + currentColumn;

        Compress(memory, previousBlock, referenceBlock, currentBlock, pass != 0);
    }

    private static int GetReferenceColumn(ulong pseudoRandom, bool sameLane, int pass, int slice, int index, int laneLength, int segmentLength)
    {
        var referenceAreaSize = GetReferenceAreaSize(sameLane, pass, slice, index, laneLength, segmentLength);
        var relativePosition = pseudoRandom & uint.MaxValue;
        relativePosition = (relativePosition * relativePosition) >> 32;
        relativePosition = (ulong)referenceAreaSize - 1 - (((ulong)referenceAreaSize * relativePosition) >> 32);

        var startPosition = pass != 0 && slice != SyncPoints - 1 ? (slice + 1) * segmentLength : 0;
        return (int)(((ulong)startPosition + relativePosition) % (ulong)laneLength);
    }

    private static int GetReferenceAreaSize(bool sameLane, int pass, int slice, int index, int laneLength, int segmentLength)
    {
        if (pass == 0)
        {
            if (slice == 0)
            {
                return index - 1;
            }

            return sameLane ? (slice * segmentLength) + index - 1 : (slice * segmentLength) + (index == 0 ? -1 : 0);
        }

        return sameLane ? laneLength - segmentLength + index - 1 : laneLength - segmentLength + (index == 0 ? -1 : 0);
    }

    private static byte[] Finalize(ulong[] memory, int lanes, int laneLength, int tagLength)
    {
        var finalBlock = new ulong[BlockWords];
        for (var lane = 0; lane < lanes; lane++)
        {
            XorBlock(memory, (lane * laneLength) + laneLength - 1, finalBlock);
        }

        var bytes = new byte[BlockBytes];
        StoreBlock(finalBlock, bytes);
        return Hash(bytes, tagLength);
    }

    private static byte[] Hash(ReadOnlySpan<byte> input, int outputLength)
    {
        var firstInput = new byte[input.Length + sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(firstInput, outputLength);
        input.CopyTo(firstInput.AsSpan(sizeof(int)));

        if (outputLength <= 64)
        {
            using var blake = new Blake2B(outputLength);
            return blake.ComputeHash(firstInput);
        }

        var result = new byte[outputLength];
        using var first = new Blake2B();
        var current = first.ComputeHash(firstInput);
        current.AsSpan(0, 32).CopyTo(result);
        var bytesWritten = 32;
        var remaining = outputLength - bytesWritten;

        while (remaining > 64)
        {
            using var blake = new Blake2B();
            current = blake.ComputeHash(current);
            current.AsSpan(0, 32).CopyTo(result.AsSpan(bytesWritten));
            bytesWritten += 32;
            remaining -= 32;
        }

        using var last = new Blake2B(remaining);
        last.ComputeHash(current).CopyTo(result.AsSpan(bytesWritten));
        return result;
    }

    private static void Compress(ulong[] memory, int leftBlock, int rightBlock, int destinationBlock, bool xorWithDestination)
    {
        var input = new ulong[BlockWords];
        var output = new ulong[BlockWords];
        var leftOffset = BlockOffset(leftBlock);
        var rightOffset = BlockOffset(rightBlock);

        for (var i = 0; i < BlockWords; i++)
        {
            input[i] = memory[leftOffset + i] ^ memory[rightOffset + i];
        }

        Compress(input, output);
        var destinationOffset = BlockOffset(destinationBlock);
        for (var i = 0; i < BlockWords; i++)
        {
            var value = output[i];
            memory[destinationOffset + i] = xorWithDestination ? memory[destinationOffset + i] ^ value : value;
        }
    }

    private static void Compress(ulong[] left, ulong[] right, ulong[] destination)
    {
        Span<ulong> input = stackalloc ulong[BlockWords];
        for (var i = 0; i < BlockWords; i++)
        {
            input[i] = left[i] ^ right[i];
            destination[i] = input[i];
        }

        Permute(destination);
        for (var i = 0; i < BlockWords; i++)
        {
            destination[i] ^= input[i];
        }
    }

    private static void Compress(ulong[] input, ulong[] destination)
    {
        input.CopyTo(destination.AsSpan());
        Permute(destination);
        for (var i = 0; i < BlockWords; i++)
        {
            destination[i] ^= input[i];
        }
    }

    private static void Permute(ulong[] block)
    {
        for (var row = 0; row < 8; row++)
        {
            Round(block.AsSpan(row * 16, 16));
        }

        Span<ulong> column = stackalloc ulong[16];
        for (var col = 0; col < 8; col++)
        {
            for (var row = 0; row < 8; row++)
            {
                var registerOffset = ((row * 8) + col) * 2;
                column[row * 2] = block[registerOffset];
                column[(row * 2) + 1] = block[registerOffset + 1];
            }

            Round(column);

            for (var row = 0; row < 8; row++)
            {
                var registerOffset = ((row * 8) + col) * 2;
                block[registerOffset] = column[row * 2];
                block[registerOffset + 1] = column[(row * 2) + 1];
            }
        }
    }

    private static void Round(Span<ulong> v)
    {
        G(v, 0, 4, 8, 12);
        G(v, 1, 5, 9, 13);
        G(v, 2, 6, 10, 14);
        G(v, 3, 7, 11, 15);
        G(v, 0, 5, 10, 15);
        G(v, 1, 6, 11, 12);
        G(v, 2, 7, 8, 13);
        G(v, 3, 4, 9, 14);
    }

    private static void G(Span<ulong> v, int a, int b, int c, int d)
    {
        v[a] = Add(v[a], v[b]);
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 32);
        v[c] = Add(v[c], v[d]);
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 24);
        v[a] = Add(v[a], v[b]);
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 16);
        v[c] = Add(v[c], v[d]);
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 63);
    }

    private static ulong Add(ulong left, ulong right)
        => left + right + (((ulong)(uint)left * (uint)right) << 1);

    private static int BlockOffset(int blockIndex) => blockIndex * BlockWords;

    private static void LoadBlock(byte[] source, ulong[] destination, int blockIndex)
    {
        var offset = BlockOffset(blockIndex);
        for (var i = 0; i < BlockWords; i++)
        {
            destination[offset + i] = BinaryPrimitives.ReadUInt64LittleEndian(source.AsSpan(i * sizeof(ulong)));
        }
    }

    private static void StoreBlock(ulong[] source, byte[] destination)
    {
        for (var i = 0; i < BlockWords; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination.AsSpan(i * sizeof(ulong)), source[i]);
        }
    }

    private static void XorBlock(ulong[] memory, int blockIndex, ulong[] destination)
    {
        var offset = BlockOffset(blockIndex);
        for (var i = 0; i < BlockWords; i++)
        {
            destination[i] ^= memory[offset + i];
        }
    }
}