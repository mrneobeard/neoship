using System.Buffers.Binary;
using System.Security.Cryptography;

// SA1204: Static members should appear before non-static members.
// Helper methods are organized near their usage for clarity.
#pragma warning disable SA1204

// SCS0013: Potential usage of weak CipherMode.
// AES-CBC mode is used here with proper security mitigations:
// 1. Encrypt-then-MAC: HMAC is computed over ciphertext, verified before decryption
// 2. Constant-time HMAC comparison using FixedTimeEquals prevents timing attacks
// 3. Decryption only proceeds after HMAC validation succeeds
#pragma warning disable SCS0013

namespace NeoBeard.Crypto;

/// <summary>
/// Provides AES-CBC encryption with PBKDF2 key derivation and HMAC integrity verification.
/// The binary format is compatible with the Go aescbc implementation.
/// </summary>
/// <remarks>
/// <para>
/// The provider uses an encrypt-then-MAC approach where data is encrypted first,
/// and then an HMAC is computed over the ciphertext to ensure integrity.
/// HMAC verification uses constant-time comparison via <see cref="CryptographicOperations.FixedTimeEquals"/>
/// to prevent timing attacks before decryption proceeds.
/// </para>
/// <para>
/// Binary layout:
/// <list type="number">
///   <item>version (short) - 2 bytes</item>
///   <item>saltSize (short) - 2 bytes</item>
///   <item>keySize (short) - 2 bytes</item>
///   <item>kdfHashId (short) - 2 bytes</item>
///   <item>hmacHashId (short) - 2 bytes</item>
///   <item>iterations (int) - 4 bytes</item>
///   <item>metadataSize (int) - 4 bytes</item>
///   <item>salt (byte[]) - variable</item>
///   <item>iv (byte[]) - 16 bytes</item>
///   <item>metadata (byte[]) - variable</item>
///   <item>tag/hmac (byte[]) - variable</item>
///   <item>ciphertext (byte[]) - variable</item>
/// </list>
/// </para>
/// <example>
/// <code lang="csharp">
/// var provider = AesCbcEncryptionProvider.Aes256();
/// var key = "secret-password"u8;
/// var plaintext = "Hello, World!"u8;
/// var ciphertext = provider.Encrypt(plaintext, key);
/// var decrypted = provider.Decrypt(ciphertext, key);
/// Assert.Equal(plaintext.ToArray(), decrypted);
/// </code>
/// </example>
/// </remarks>
public sealed class AesCbcEncryptionProvider : IEncryptionProvider
{
    private const short Version = 1;
    private const int IvSize = 16;
    private const int FixedHeaderSize = 18;
    private const int AesBlockSize = 16;

    /// <summary>
    /// Gets the number of PBKDF2 iterations for key derivation.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = new AesCbcEncryptionProvider { Iterations = 100000 };
    /// Assert.Equal(100000, provider.Iterations);
    /// </code>
    /// </example>
    /// </remarks>
    public int Iterations { get; init; } = 60000;

    /// <summary>
    /// Gets the key size in bytes (16 for AES-128, 24 for AES-192, 32 for AES-256).
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// Assert.Equal(32, provider.KeySize);
    /// </code>
    /// </example>
    /// </remarks>
    public int KeySize { get; init; } = 32;

    /// <summary>
    /// Gets the salt size in bytes for PBKDF2.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = new AesCbcEncryptionProvider { SaltSize = 16 };
    /// Assert.Equal(16, provider.SaltSize);
    /// </code>
    /// </example>
    /// </remarks>
    public short SaltSize { get; init; } = 8;

    /// <summary>
    /// Gets the hash algorithm for PBKDF2 key derivation.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = new AesCbcEncryptionProvider { KdfHash = HashType.SHA512 };
    /// Assert.Equal(HashType.SHA512, provider.KdfHash);
    /// </code>
    /// </example>
    /// </remarks>
    public HashType KdfHash { get; init; } = HashType.SHA256;

    /// <summary>
    /// Gets the hash algorithm for HMAC integrity verification.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = new AesCbcEncryptionProvider { HmacHash = HashType.SHA512 };
    /// Assert.Equal(HashType.SHA512, provider.HmacHash);
    /// </code>
    /// </example>
    /// </remarks>
    public HashType HmacHash { get; init; } = HashType.SHA256;

    /// <summary>
    /// Creates an AES-256-CBC encryption provider with default settings.
    /// </summary>
    /// <returns>A new <see cref="AesCbcEncryptionProvider"/> configured for AES-256.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// Assert.Equal(32, provider.KeySize);
    /// </code>
    /// </example>
    /// </remarks>
    public static AesCbcEncryptionProvider Aes256() => new();

    /// <summary>
    /// Creates an AES-128-CBC encryption provider with default settings.
    /// </summary>
    /// <returns>A new <see cref="AesCbcEncryptionProvider"/> configured for AES-128.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes128();
    /// Assert.Equal(16, provider.KeySize);
    /// </code>
    /// </example>
    /// </remarks>
    public static AesCbcEncryptionProvider Aes128() => new() { KeySize = 16 };

    /// <summary>
    /// Encrypts data using the specified key.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <returns>The encrypted data.</returns>
    /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var key = "secret-password"u8.ToArray();
    /// var ciphertext = provider.Encrypt("Hello, World!"u8.ToArray());
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Encrypt(byte[] data) => this.Encrypt(data, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);

    /// <summary>
    /// Encrypts data using the specified key.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <returns>The encrypted data.</returns>
    /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var key = "secret-password"u8;
    /// var ciphertext = provider.Encrypt("Hello, World!"u8);
    /// </code>
    /// </example>
    /// </remarks>
    public ReadOnlySpan<byte> Encrypt(ReadOnlySpan<byte> data) => this.Encrypt(data, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);

    /// <summary>
    /// Encrypts data using the specified key and optional metadata.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="metadata">Optional metadata to include in the encrypted blob.</param>
    /// <returns>The encrypted data including header, metadata, HMAC tag, and ciphertext.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty.</exception>
    /// <exception cref="CryptographicException">Thrown when encryption fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the hash algorithm is not supported.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var key = "secret-password"u8;
    /// var metadata = "version=1"u8;
    /// var ciphertext = provider.Encrypt("Hello, World!"u8, key, metadata);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> metadata)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("Encryption key cannot be empty.", nameof(key));
        }

        if (!this.KdfHash.SupportsPbkdf2())
        {
            throw new InvalidOperationException($"PBKDF2 is not supported for {this.KdfHash.Name}");
        }

        if (!this.HmacHash.SupportsHmac())
        {
            throw new InvalidOperationException($"HMAC is not supported for {this.HmacHash.Name}");
        }

        using var rng = new Csrng();
        var salt = rng.NextBytes(this.SaltSize);
        var iv = rng.NextBytes(IvSize);

        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            key,
            salt,
            this.Iterations,
            this.KdfHash.ToHashAlgorithmName(),
            this.KeySize);

        byte[] ciphertext;
        try
        {
            ciphertext = EncryptAesCbc(plaintext, derivedKey, iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }

        byte[] hmacTag;
        try
        {
            derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                key,
                salt,
                this.Iterations,
                this.KdfHash.ToHashAlgorithmName(),
                this.KeySize);

            hmacTag = this.HmacHash.ComputeHmac(derivedKey, ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }

        var result = this.AssembleBlob(salt, iv, metadata, hmacTag, ciphertext);

        CryptographicOperations.ZeroMemory(ciphertext);
        CryptographicOperations.ZeroMemory(hmacTag);

        return result;
    }

    /// <summary>
    /// Decrypts encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="CryptographicException">Thrown when decryption fails or HMAC verification fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var ciphertext = provider.Encrypt("Hello, World!"u8.ToArray());
    /// var plaintext = provider.Decrypt(ciphertext);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Decrypt(byte[] encryptedData) => this.Decrypt(encryptedData.AsSpan(), ReadOnlySpan<byte>.Empty, out _).ToArray();

    /// <summary>
    /// Decrypts encrypted data.
    /// </summary>
    /// <param name="data">The encrypted data to decrypt.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="CryptographicException">Thrown when decryption fails or HMAC verification fails.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var ciphertext = provider.Encrypt("Hello, World!"u8);
    /// var plaintext = provider.Decrypt(ciphertext);
    /// </code>
    /// </example>
    /// </remarks>
    public ReadOnlySpan<byte> Decrypt(ReadOnlySpan<byte> data) => this.Decrypt(data, ReadOnlySpan<byte>.Empty, out _);

    /// <summary>
    /// Decrypts encrypted data using the specified key and extracts any metadata.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="key">The decryption key.</param>
    /// <param name="metadata">The extracted metadata, if any.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty.</exception>
    /// <exception cref="CryptographicException">Thrown when decryption fails or HMAC verification fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the hash algorithm is not supported.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var provider = AesCbcEncryptionProvider.Aes256();
    /// var key = "secret-password"u8;
    /// var metadata = "version=1"u8;
    /// var ciphertext = provider.Encrypt("Hello, World!"u8, key, metadata);
    /// var plaintext = provider.Decrypt(ciphertext, key, out var extractedMetadata);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> key, out ReadOnlySpan<byte> metadata)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("Decryption key cannot be empty.", nameof(key));
        }

        var header = ReadHeader(ciphertext);
        metadata = ReadOnlySpan<byte>.Empty;

        if (!header.KdfHash.SupportsPbkdf2())
        {
            throw new InvalidOperationException($"PBKDF2 is not supported for {header.KdfHash.Name}");
        }

        if (!header.HmacHash.SupportsHmac())
        {
            throw new InvalidOperationException($"HMAC is not supported for {header.HmacHash.Name}");
        }

        var offset = FixedHeaderSize;
        var salt = ciphertext.Slice(offset, header.SaltSize);
        offset += header.SaltSize;

        var iv = ciphertext.Slice(offset, IvSize);
        offset += IvSize;

        if (header.MetadataSize > 0)
        {
            metadata = ciphertext.Slice(offset, header.MetadataSize);
            offset += header.MetadataSize;
        }

        var storedTag = ciphertext.Slice(offset, header.HmacHash.Size);
        offset += header.HmacHash.Size;

        var encryptedData = ciphertext.Slice(offset);

        byte[] computedTag;
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            key,
            salt,
            header.Iterations,
            header.KdfHash.ToHashAlgorithmName(),
            header.KeySize);

        try
        {
            computedTag = header.HmacHash.ComputeHmac(derivedKey, encryptedData.ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }

        try
        {
            if (!CryptographicOperations.FixedTimeEquals(storedTag, computedTag))
            {
                throw new CryptographicException("HMAC verification failed. The data may have been tampered with.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(computedTag);
        }

        derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            key,
            salt,
            header.Iterations,
            header.KdfHash.ToHashAlgorithmName(),
            header.KeySize);

        byte[] plaintext;
        try
        {
            plaintext = DecryptAesCbc(encryptedData, derivedKey, iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }

        return plaintext;
    }

    private static byte[] EncryptAesCbc(ReadOnlySpan<byte> plaintext, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plaintext);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private static byte[] DecryptAesCbc(ReadOnlySpan<byte> ciphertext, byte[] key, ReadOnlySpan<byte> iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv.ToArray();

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
        {
            cs.Write(ciphertext);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    private byte[] AssembleBlob(byte[] salt, byte[] iv, ReadOnlySpan<byte> metadata, byte[] hmacTag, byte[] ciphertext)
    {
        var metadataSize = metadata.Length;
        var totalSize = FixedHeaderSize + salt.Length + iv.Length + metadataSize + hmacTag.Length + ciphertext.Length;
        var result = new byte[totalSize];

        var offset = 0;
        WriteHeader(result.AsSpan(), ref offset, (short)salt.Length, (short)this.KeySize, this.KdfHash.Id, this.HmacHash.Id, this.Iterations, metadataSize);
        salt.CopyTo(result.AsSpan(offset));
        offset += salt.Length;
        iv.CopyTo(result.AsSpan(offset));
        offset += iv.Length;

        if (metadataSize > 0)
        {
            metadata.CopyTo(result.AsSpan(offset));
            offset += metadataSize;
        }

        hmacTag.CopyTo(result.AsSpan(offset));
        offset += hmacTag.Length;
        ciphertext.CopyTo(result.AsSpan(offset));

        return result;
    }

    private static void WriteHeader(Span<byte> buffer, ref int offset, short saltSize, short keySize, short kdfHashId, short hmacHashId, int iterations, int metadataSize)
    {
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), Version);
        offset += sizeof(short);

        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), saltSize);
        offset += sizeof(short);

        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), keySize);
        offset += sizeof(short);

        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), kdfHashId);
        offset += sizeof(short);

        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(offset), hmacHashId);
        offset += sizeof(short);

        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), iterations);
        offset += sizeof(int);

        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), metadataSize);
        offset += sizeof(int);
    }

    private static (short SaltSize, int KeySize, HashType KdfHash, HashType HmacHash, int Iterations, int MetadataSize) ReadHeader(ReadOnlySpan<byte> data)
    {
        if (data.Length < FixedHeaderSize)
        {
            throw new CryptographicException($"Data too short to contain header. Expected at least {FixedHeaderSize} bytes, got {data.Length}.");
        }

        var offset = 0;

        var version = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        if (version != Version)
        {
            throw new CryptographicException($"Unsupported version: {version}. Expected {Version}.");
        }

        offset += sizeof(short);

        var saltSize = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        offset += sizeof(short);

        var keySize = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        offset += sizeof(short);

        var kdfHashId = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        offset += sizeof(short);

        var hmacHashId = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset));
        offset += sizeof(short);

        var iterations = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset));
        offset += sizeof(int);

        var metadataSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset));

        return (saltSize, keySize, HashType.FromId(kdfHashId), HashType.FromId(hmacHashId), iterations, metadataSize);
    }
}

#pragma warning restore SCS0013
#pragma warning restore SA1204