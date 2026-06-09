using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Provides a managed RFC 8439 ChaCha20-Poly1305 authenticated encryption implementation.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var key = new byte[32];
/// var nonce = new byte[12];
/// var plaintext = "message"u8.ToArray();
/// var ciphertext = new byte[plaintext.Length];
/// var tag = new byte[16];
/// ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag);
/// </code>
/// </example>
/// </remarks>
public static class ChaCha20Poly1305Managed
{
    /// <summary>
    /// Gets the required key length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = new byte[ChaCha20Poly1305Managed.KeySize];
    /// Assert.Equal(32, key.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public const int KeySize = 32;

    /// <summary>
    /// Gets the required nonce length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var nonce = new byte[ChaCha20Poly1305Managed.NonceSize];
    /// Assert.Equal(12, nonce.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public const int NonceSize = 12;

    /// <summary>
    /// Gets the required authentication tag length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tag = new byte[ChaCha20Poly1305Managed.TagSize];
    /// Assert.Equal(16, tag.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public const int TagSize = 16;

    private const int BlockSize = 64;

    /// <summary>
    /// Encrypts plaintext and writes the ciphertext and authentication tag.
    /// </summary>
    /// <param name="key">The 32-byte ChaCha20 key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <param name="plaintext">The plaintext to encrypt.</param>
    /// <param name="ciphertext">The destination for the encrypted plaintext.</param>
    /// <param name="tag">The destination for the 16-byte authentication tag.</param>
    /// <param name="associatedData">The optional associated data to authenticate.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = new byte[32];
    /// var nonce = new byte[12];
    /// var plaintext = "data"u8.ToArray();
    /// var ciphertext = new byte[plaintext.Length];
    /// var tag = new byte[16];
    /// ChaCha20Poly1305Managed.Encrypt(key, nonce, plaintext, ciphertext, tag, "aad"u8);
    /// </code>
    /// </example>
    /// </remarks>
    public static void Encrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> plaintext,
        Span<byte> ciphertext,
        Span<byte> tag,
        ReadOnlySpan<byte> associatedData = default)
    {
        ValidateInputs(key, nonce, plaintext.Length, ciphertext.Length, tag.Length);

        ApplyChaCha20(key, nonce, plaintext, ciphertext, 1);
        WriteTag(key, nonce, associatedData, ciphertext, tag);
    }

    /// <summary>
    /// Decrypts ciphertext after verifying the supplied authentication tag.
    /// </summary>
    /// <param name="key">The 32-byte ChaCha20 key.</param>
    /// <param name="nonce">The 12-byte nonce.</param>
    /// <param name="ciphertext">The ciphertext to authenticate and decrypt.</param>
    /// <param name="tag">The 16-byte authentication tag.</param>
    /// <param name="plaintext">The destination for the decrypted plaintext.</param>
    /// <param name="associatedData">The optional associated data to authenticate.</param>
    /// <returns><see langword="true" /> when authentication succeeds; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = new byte[32];
    /// var nonce = new byte[12];
    /// var ciphertext = new byte[4];
    /// var tag = new byte[16];
    /// var plaintext = new byte[ciphertext.Length];
    /// var authenticated = ChaCha20Poly1305Managed.Decrypt(key, nonce, ciphertext, tag, plaintext);
    /// Assert.False(authenticated);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool Decrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        Span<byte> plaintext,
        ReadOnlySpan<byte> associatedData = default)
    {
        ValidateInputs(key, nonce, ciphertext.Length, plaintext.Length, tag.Length);

        Span<byte> expectedTag = stackalloc byte[TagSize];
        WriteTag(key, nonce, associatedData, ciphertext, expectedTag);
        if (!CryptographicOperations.FixedTimeEquals(tag, expectedTag))
        {
            plaintext.Clear();
            return false;
        }

        ApplyChaCha20(key, nonce, ciphertext, plaintext, 1);
        return true;
    }

    private static void ValidateInputs(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, int inputLength, int outputLength, int tagLength)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("ChaCha20-Poly1305 requires a 32-byte key.", nameof(key));
        }

        if (nonce.Length != NonceSize)
        {
            throw new ArgumentException("ChaCha20-Poly1305 requires a 12-byte nonce.", nameof(nonce));
        }

        if (inputLength != outputLength)
        {
            throw new ArgumentException("Input and output buffers must have the same length.");
        }

        if (tagLength != TagSize)
        {
            throw new ArgumentException("ChaCha20-Poly1305 requires a 16-byte authentication tag.", nameof(tagLength));
        }
    }

    private static void ApplyChaCha20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> input, Span<byte> output, int counter)
    {
        var keyBytes = key.ToArray();
        var nonceBytes = nonce.ToArray();
        var inputBytes = input.ToArray();
        var outputBytes = new byte[input.Length];

        using var transform = new ChaCha20Transform(keyBytes, nonceBytes, ChaChaRound.Twenty, false, counter);
        transform.TransformBlock(inputBytes, 0, inputBytes.Length, outputBytes, 0);
        outputBytes.CopyTo(output);

        CryptographicOperations.ZeroMemory(keyBytes);
        CryptographicOperations.ZeroMemory(inputBytes);
        CryptographicOperations.ZeroMemory(outputBytes);
    }

    private static void WriteTag(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> associatedData, ReadOnlySpan<byte> ciphertext, Span<byte> tag)
    {
        Span<byte> polyKey = stackalloc byte[32];
        Span<byte> block = stackalloc byte[BlockSize];
        ChaCha20Block(key, nonce, 0, block);
        block.Slice(0, 32).CopyTo(polyKey);

        var macData = new byte[GetPaddedLength(associatedData.Length) + GetPaddedLength(ciphertext.Length) + 16];
        var offset = 0;
        associatedData.CopyTo(macData.AsSpan(offset));
        offset += GetPaddedLength(associatedData.Length);
        ciphertext.CopyTo(macData.AsSpan(offset));
        offset += GetPaddedLength(ciphertext.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(macData.AsSpan(offset), (ulong)associatedData.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(macData.AsSpan(offset + 8), (ulong)ciphertext.Length);

        var poly = new Poly1305(polyKey);
        poly.Update(macData);
        poly.Finalize(tag);

        CryptographicOperations.ZeroMemory(polyKey);
        CryptographicOperations.ZeroMemory(block);
        CryptographicOperations.ZeroMemory(macData);
    }

    private static int GetPaddedLength(int length)
    {
        var remainder = length % 16;
        return remainder == 0 ? length : length + 16 - remainder;
    }

    private static void ChaCha20Block(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, int counter, Span<byte> output)
    {
        var keyBytes = key.ToArray();
        var nonceBytes = nonce.ToArray();
        var state = ChaCha20Transform.CreateState(keyBytes, nonceBytes, counter);
        var buffer = new uint[16];
        var outputBytes = new byte[BlockSize];

        ChaCha20Transform.AddXorRotate((int)ChaChaRound.Twenty, state, buffer, outputBytes);
        outputBytes.CopyTo(output);

        CryptographicOperations.ZeroMemory(outputBytes);
        CryptographicOperations.ZeroMemory(keyBytes);
        CryptographicOperations.ZeroMemory(nonceBytes);
        Array.Clear(state, 0, state.Length);
        Array.Clear(buffer, 0, buffer.Length);
    }

    private struct Poly1305
    {
        private readonly uint r0;
        private readonly uint r1;
        private readonly uint r2;
        private readonly uint r3;
        private readonly uint r4;
        private readonly uint s1;
        private readonly uint s2;
        private readonly uint s3;
        private readonly uint s4;
        private readonly uint pad0;
        private readonly uint pad1;
        private readonly uint pad2;
        private readonly uint pad3;

        private uint h0;
        private uint h1;
        private uint h2;
        private uint h3;
        private uint h4;

        public Poly1305(ReadOnlySpan<byte> key)
        {
            var t0 = BinaryPrimitives.ReadUInt32LittleEndian(key);
            var t1 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4));
            var t2 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8));
            var t3 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12));

            this.r0 = t0 & 0x3ffffff;
            this.r1 = ((t0 >> 26) | (t1 << 6)) & 0x3ffff03;
            this.r2 = ((t1 >> 20) | (t2 << 12)) & 0x3ffc0ff;
            this.r3 = ((t2 >> 14) | (t3 << 18)) & 0x3f03fff;
            this.r4 = (t3 >> 8) & 0x00fffff;

            this.s1 = this.r1 * 5;
            this.s2 = this.r2 * 5;
            this.s3 = this.r3 * 5;
            this.s4 = this.r4 * 5;

            this.pad0 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16));
            this.pad1 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20));
            this.pad2 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24));
            this.pad3 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28));

            this.h0 = 0;
            this.h1 = 0;
            this.h2 = 0;
            this.h3 = 0;
            this.h4 = 0;
        }

        public void Update(ReadOnlySpan<byte> data)
        {
            while (data.Length >= 16)
            {
                this.ProcessBlock(data.Slice(0, 16), 1 << 24);
                data = data.Slice(16);
            }

            if (data.Length > 0)
            {
                Span<byte> block = stackalloc byte[16];
                data.CopyTo(block);
                block[data.Length] = 1;
                this.ProcessBlock(block, 0);
            }
        }

        public void Finalize(Span<byte> tag)
        {
            var c = this.h1 >> 26;
            this.h1 &= 0x3ffffff;
            this.h2 += c;
            c = this.h2 >> 26;
            this.h2 &= 0x3ffffff;
            this.h3 += c;
            c = this.h3 >> 26;
            this.h3 &= 0x3ffffff;
            this.h4 += c;
            c = this.h4 >> 26;
            this.h4 &= 0x3ffffff;
            this.h0 += c * 5;
            c = this.h0 >> 26;
            this.h0 &= 0x3ffffff;
            this.h1 += c;

            var g0 = this.h0 + 5;
            c = g0 >> 26;
            g0 &= 0x3ffffff;
            var g1 = this.h1 + c;
            c = g1 >> 26;
            g1 &= 0x3ffffff;
            var g2 = this.h2 + c;
            c = g2 >> 26;
            g2 &= 0x3ffffff;
            var g3 = this.h3 + c;
            c = g3 >> 26;
            g3 &= 0x3ffffff;
            var g4 = this.h4 + c - (1 << 26);

            var mask = (g4 >> 31) - 1;
            g0 &= mask;
            g1 &= mask;
            g2 &= mask;
            g3 &= mask;
            g4 &= mask;
            mask = ~mask;
            this.h0 = (this.h0 & mask) | g0;
            this.h1 = (this.h1 & mask) | g1;
            this.h2 = (this.h2 & mask) | g2;
            this.h3 = (this.h3 & mask) | g3;
            this.h4 = (this.h4 & mask) | g4;

            var f0 = (ulong)(this.h0 | (this.h1 << 26)) + this.pad0;
            var f1 = (ulong)((this.h1 >> 6) | (this.h2 << 20)) + this.pad1 + (f0 >> 32);
            var f2 = (ulong)((this.h2 >> 12) | (this.h3 << 14)) + this.pad2 + (f1 >> 32);
            var f3 = (ulong)((this.h3 >> 18) | (this.h4 << 8)) + this.pad3 + (f2 >> 32);

            BinaryPrimitives.WriteUInt32LittleEndian(tag, (uint)f0);
            BinaryPrimitives.WriteUInt32LittleEndian(tag.Slice(4), (uint)f1);
            BinaryPrimitives.WriteUInt32LittleEndian(tag.Slice(8), (uint)f2);
            BinaryPrimitives.WriteUInt32LittleEndian(tag.Slice(12), (uint)f3);
        }

        private void ProcessBlock(ReadOnlySpan<byte> block, uint hibit)
        {
            var t0 = BinaryPrimitives.ReadUInt32LittleEndian(block);
            var t1 = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(4));
            var t2 = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(8));
            var t3 = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(12));

            this.h0 += t0 & 0x3ffffff;
            this.h1 += ((t0 >> 26) | (t1 << 6)) & 0x3ffffff;
            this.h2 += ((t1 >> 20) | (t2 << 12)) & 0x3ffffff;
            this.h3 += ((t2 >> 14) | (t3 << 18)) & 0x3ffffff;
            this.h4 += (t3 >> 8) | hibit;

            var d0 = ((ulong)this.h0 * this.r0) + ((ulong)this.h1 * this.s4) + ((ulong)this.h2 * this.s3) + ((ulong)this.h3 * this.s2) + ((ulong)this.h4 * this.s1);
            var d1 = ((ulong)this.h0 * this.r1) + ((ulong)this.h1 * this.r0) + ((ulong)this.h2 * this.s4) + ((ulong)this.h3 * this.s3) + ((ulong)this.h4 * this.s2);
            var d2 = ((ulong)this.h0 * this.r2) + ((ulong)this.h1 * this.r1) + ((ulong)this.h2 * this.r0) + ((ulong)this.h3 * this.s4) + ((ulong)this.h4 * this.s3);
            var d3 = ((ulong)this.h0 * this.r3) + ((ulong)this.h1 * this.r2) + ((ulong)this.h2 * this.r1) + ((ulong)this.h3 * this.r0) + ((ulong)this.h4 * this.s4);
            var d4 = ((ulong)this.h0 * this.r4) + ((ulong)this.h1 * this.r3) + ((ulong)this.h2 * this.r2) + ((ulong)this.h3 * this.r1) + ((ulong)this.h4 * this.r0);

            var c = (uint)(d0 >> 26);
            this.h0 = (uint)d0 & 0x3ffffff;
            d1 += c;
            c = (uint)(d1 >> 26);
            this.h1 = (uint)d1 & 0x3ffffff;
            d2 += c;
            c = (uint)(d2 >> 26);
            this.h2 = (uint)d2 & 0x3ffffff;
            d3 += c;
            c = (uint)(d3 >> 26);
            this.h3 = (uint)d3 & 0x3ffffff;
            d4 += c;
            c = (uint)(d4 >> 26);
            this.h4 = (uint)d4 & 0x3ffffff;
            this.h0 += c * 5;
            c = this.h0 >> 26;
            this.h0 &= 0x3ffffff;
            this.h1 += c;
        }
    }
}