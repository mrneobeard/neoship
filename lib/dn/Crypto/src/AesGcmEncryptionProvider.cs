using System.Buffers.Binary;
using System.Security.Cryptography;

namespace NeoBeard.Crypto;

public class AesGcmEncryptionProvider : IEncryptionProvider
{
    private readonly AesGcmEncryptionProviderOptions options;

    public AesGcmEncryptionProvider(AesGcmEncryptionProviderOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Encrypt(byte[] data)
    {
        return this.Encrypt(data.AsSpan()).ToArray();
    }

    public ReadOnlySpan<byte> Encrypt(ReadOnlySpan<byte> data)
    {
        var salt = Csrng.GetBytes(this.options.SaltSize);

        short nonceSize = (short)AesGcm.NonceByteSizes.MaxSize;
        short tagSize = (short)AesGcm.TagByteSizes.MaxSize;
        short version = this.options.Version;
        short saltSize = this.options.SaltSize;

        int dataLength =
            2 + // version
            2 + // salt size
            2 + // key size
            2 + // pdk2 hash algorithm
            2 + // nonce size
            2 + // tag size
            4 + // iterations
            saltSize + // salt size
            nonceSize + // nonce size
            tagSize + // tag size
            data.Length; // data size

        // https://stackoverflow.com/questions/60889345/using-the-aesgcm-class
        // Get parameter sizes
        int cipherSize = data.Length;
        Span<byte> encryptedData = new byte[dataLength];

        int index = 0;

        // Copy parameters
        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), version);
        index += 2;

        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), saltSize);
        index += 2;

        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), (short)this.options.KeyByteSize);
        index += 2;

        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), (short)this.options.Hash.Id);
        index += 2;

        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), (short)nonceSize);
        index += 2;

        BinaryPrimitives.WriteInt16LittleEndian(encryptedData.Slice(index, 2), (short)tagSize);
        index += 2;

        BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(index, 4), this.options.Iterations);
        index += 4;

        salt.CopyTo(encryptedData.Slice(index, saltSize));
        index += saltSize;

        var nonce = encryptedData.Slice(index, nonceSize);
        index += nonceSize;

        var tag = encryptedData.Slice(index, tagSize);
        index += tagSize;

        var cipherBytes = encryptedData.Slice(index, cipherSize);

        // Generate secure nonce
        RandomNumberGenerator.Fill(nonce);

        var key = Rfc2898DeriveBytes.Pbkdf2(
                    this.options.Key,
                    salt,
                    this.options.Iterations,
                    this.options.Hash.ToHashAlgorithmName(),
                    this.options.KeyByteSize);

        // Encrypt
        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, data, cipherBytes, tag);

        Array.Clear(key, 0, key.Length);

        return encryptedData;
    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        return this.Decrypt(encryptedData.AsSpan()).ToArray();
    }

    public ReadOnlySpan<byte> Decrypt(ReadOnlySpan<byte> data)
    {
        var encryptedData = data;

        /*

           int dataLength =
            2 + // version
            2 + // salt size
            2 + // key size
            2 + // pdk2 hash algorithm
            2 + // nonce size
            2 + // tag size
            4 + // iterations
            saltSize + // salt size
            nonceSize + // nonce size
            tagSize + // tag size
            data.Length; // data size
            */

        var index = 0;
        var version = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        if (version != 1)
        {
            throw new InvalidOperationException($"Unsupported version: {version}. Expected: {this.options.Version}.");
        }

        // Check salt size
        var saltSize = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        var keySize = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        var hashAlgorithmId = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        var nonceSize = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        var tagSize = BinaryPrimitives.ReadInt16LittleEndian(encryptedData.Slice(index, 2));
        index += 2;

        var iterations = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(index, 4));
        index += 4;

        var salt = encryptedData.Slice(index, saltSize);
        index += saltSize;

        // ignore values from this.options on decrypt, only use the values from the encrypted data
        var nonce = encryptedData.Slice(index, nonceSize);
        index += nonceSize;

        var tag = encryptedData.Slice(index, tagSize);
        index += tagSize;

        var cipheredBytes = encryptedData.Slice(index);

        var hash = HashType.FromId(hashAlgorithmId);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            this.options.Key,
            salt.ToArray(),
            iterations,
            hash.ToHashAlgorithmName(),
            keySize);

        // Decrypt
        Span<byte> plainBytes = new byte[cipheredBytes.Length];
        using var aes = new AesGcm(key, tagSize);
        aes.Decrypt(nonce, cipheredBytes, tag, plainBytes);

        Array.Clear(key, 0, key.Length);

        // Convert plain bytes back into string
        return plainBytes;
    }
}

public class AesGcmEncryptionProviderOptions
{
    public byte[] Key { get; set; } = [];

    public int Iterations { get; set; } = 60000;

    public short Version { get; set; } = 1;

    public short SaltSize { get; set; } = 32;

    public HashType Hash { get; set; } = HashType.SHA256;

    public int KeyByteSize { get; set; } = 32;

    public AesGcmEncryptionProviderOptions WithKey(ReadOnlySpan<char> key, System.Text.Encoding? encoding = null)
    {
        if (key.IsEmpty)
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        encoding ??= EncryptionUtil.DefaultEncoding;
        var keySet = key.ToArray();
        this.Key = encoding.GetBytes(keySet);
        Array.Clear(keySet, 0, keySet.Length);
        return this;
    }
}