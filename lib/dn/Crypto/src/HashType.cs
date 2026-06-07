using System.ComponentModel;
using System.Security.Cryptography;

namespace NeoBeard.Crypto;

/// <summary>
/// Represents hash algorithm types with IDs matching the Go implementation.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var hashType = HashType.SHA256;
/// Console.WriteLine($"ID: {hashType.Id}, Size: {hashType.Size}");
/// </code>
/// </example>
/// </remarks>
public readonly struct HashType : IEquatable<HashType>
{
    private const short Md5Id = 1;
    private const short Sha1Id = 2;
    private const short Sha224Id = 3;
    private const short Sha256Id = 4;
    private const short Sha384Id = 5;
    private const short Sha512Id = 6;
    private const short Sha3224Id = 7;
    private const short Sha3256Id = 8;
    private const short Sha3384Id = 9;
    private const short Sha3512Id = 10;
    private const short Blake2B256Id = 11;
    private const short Blake2B384Id = 12;
    private const short Blake2B512Id = 13;
    private const short Blake2S128Id = 14;
    private const short Blake2S256Id = 15;

    /// <summary>
    /// Gets the MD5 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.MD5;
    /// Assert.Equal(16, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType MD5 => new(Md5Id, "MD5", 16);

    /// <summary>
    /// Gets the SHA-1 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA1;
    /// Assert.Equal(20, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA1 => new(Sha1Id, "SHA1", 20);

    /// <summary>
    /// Gets the SHA-224 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA224;
    /// Assert.Equal(28, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA224 => new(Sha224Id, "SHA224", 28);

    /// <summary>
    /// Gets the SHA-256 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA256;
    /// Assert.Equal(32, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA256 => new(Sha256Id, "SHA256", 32);

    /// <summary>
    /// Gets the SHA-384 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA384;
    /// Assert.Equal(48, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA384 => new(Sha384Id, "SHA384", 48);

    /// <summary>
    /// Gets the SHA-512 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA512;
    /// Assert.Equal(64, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA512 => new(Sha512Id, "SHA512", 64);

    /// <summary>
    /// Gets the SHA3-224 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA3_224;
    /// Assert.Equal(28, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA3_224 => new(Sha3224Id, "SHA3-224", 28);

    /// <summary>
    /// Gets the SHA3-256 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA3_256;
    /// Assert.Equal(32, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA3_256 => new(Sha3256Id, "SHA3-256", 32);

    /// <summary>
    /// Gets the SHA3-384 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA3_384;
    /// Assert.Equal(48, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA3_384 => new(Sha3384Id, "SHA3-384", 48);

    /// <summary>
    /// Gets the SHA3-512 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA3_512;
    /// Assert.Equal(64, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType SHA3_512 => new(Sha3512Id, "SHA3-512", 64);

    /// <summary>
    /// Gets the BLAKE2B-256 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Blake2B_256;
    /// Assert.Equal(32, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Blake2B_256 => new(Blake2B256Id, "BLAKE2B-256", 32);

    /// <summary>
    /// Gets the BLAKE2B-384 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Blake2B_384;
    /// Assert.Equal(48, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Blake2B_384 => new(Blake2B384Id, "BLAKE2B-384", 48);

    /// <summary>
    /// Gets the BLAKE2B-512 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Blake2B_512;
    /// Assert.Equal(64, hash.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Blake2B_512 => new(Blake2B512Id, "BLAKE2B-512", 64);

    /// <summary>
    /// Gets the BLAKE2S-128 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Blake2S_128;
    /// Assert.Equal(16, hash.Size);
    /// Assert.True(hash.SupportsHmac());
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Blake2S_128 => new(Blake2S128Id, "BLAKE2S-128", 16);

    /// <summary>
    /// Gets the BLAKE2S-256 hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Blake2S_256;
    /// Assert.Equal(32, hash.Size);
    /// Assert.True(hash.SupportsHmac());
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Blake2S_256 => new(Blake2S256Id, "BLAKE2S-256", 32);

    /// <summary>
    /// Gets an unknown or unsupported hash type.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.Unknown;
    /// Assert.True(hash.IsUnknown());
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType Unknown => new(-1, "Unknown", 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="HashType"/> struct.
    /// </summary>
    /// <param name="id">The unique identifier for the hash algorithm.</param>
    /// <param name="name">The name of the hash algorithm.</param>
    /// <param name="size">The hash output size in bytes.</param>
    private HashType(short id, string name, int size)
    {
        this.Id = id;
        this.Name = name;
        this.Size = size;
    }

    /// <summary>
    /// Gets the unique identifier for the hash algorithm.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.Equal(4, HashType.SHA256.Id);
    /// </code>
    /// </example>
    /// </remarks>
    public short Id { get; }

    /// <summary>
    /// Gets the name of the hash algorithm.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.Equal("SHA256", HashType.SHA256.Name);
    /// </code>
    /// </example>
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Gets the hash output size in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.Equal(32, HashType.SHA256.Size);
    /// </code>
    /// </example>
    /// </remarks>
    public int Size { get; }

    /// <summary>
    /// Returns a hash type from its identifier.
    /// </summary>
    /// <param name="id">The identifier of the hash type.</param>
    /// <returns>The corresponding <see cref="HashType"/>.</returns>
    /// <exception cref="InvalidEnumArgumentException">Thrown when the id is not recognized.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.FromId(4);
    /// Assert.Equal(HashType.SHA256, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType FromId(short id)
    {
        return id switch
        {
            Md5Id => MD5,
            Sha1Id => SHA1,
            Sha224Id => SHA224,
            Sha256Id => SHA256,
            Sha384Id => SHA384,
            Sha512Id => SHA512,
            Sha3224Id => SHA3_224,
            Sha3256Id => SHA3_256,
            Sha3384Id => SHA3_384,
            Sha3512Id => SHA3_512,
            Blake2B256Id => Blake2B_256,
            Blake2B384Id => Blake2B_384,
            Blake2B512Id => Blake2B_512,
            Blake2S128Id => Blake2S_128,
            Blake2S256Id => Blake2S_256,
            _ => throw new InvalidEnumArgumentException(nameof(id), id, typeof(short)),
        };
    }

    /// <summary>
    /// Returns a hash type from its name.
    /// </summary>
    /// <param name="name">The name of the hash type.</param>
    /// <returns>The corresponding <see cref="HashType"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when the name is not recognized.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.FromName("SHA256");
    /// Assert.Equal(HashType.SHA256, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public static HashType FromName(string name)
    {
        return name?.ToUpperInvariant() switch
        {
            "MD5" => MD5,
            "SHA1" => SHA1,
            "SHA224" => SHA224,
            "SHA256" => SHA256,
            "SHA384" => SHA384,
            "SHA512" => SHA512,
            "SHA3-224" => SHA3_224,
            "SHA3-256" => SHA3_256,
            "SHA3-384" => SHA3_384,
            "SHA3-512" => SHA3_512,
            "BLAKE2B-256" => Blake2B_256,
            "BLAKE2B-384" => Blake2B_384,
            "BLAKE2B-512" => Blake2B_512,
            "BLAKE2S-128" => Blake2S_128,
            "BLAKE2S-256" => Blake2S_256,
            _ => throw new ArgumentException($"Unknown hash name: {name}", nameof(name)),
        };
    }

    /// <summary>
    /// Determines whether this hash type is valid and supported.
    /// </summary>
    /// <returns><c>true</c> if the hash type is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256.IsValid());
    /// Assert.False(HashType.Unknown.IsValid());
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsValid() => this.Id > 0 && this.Id <= Blake2S256Id;

    /// <summary>
    /// Determines whether this hash type represents an unknown or unsupported algorithm.
    /// </summary>
    /// <returns><c>true</c> if the hash type is unknown; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.False(HashType.SHA256.IsUnknown());
    /// Assert.True(HashType.Unknown.IsUnknown());
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsUnknown() => this.Id < 0;

    /// <summary>
    /// Determines whether this hash type supports HMAC operations.
    /// </summary>
    /// <returns><c>true</c> if HMAC is supported; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256.SupportsHmac());
    /// Assert.True(HashType.Blake2B_256.SupportsHmac());
    /// Assert.True(HashType.Blake2S_128.SupportsHmac());
    /// Assert.False(HashType.SHA224.SupportsHmac());
    /// </code>
    /// </example>
    /// </remarks>
    public bool SupportsHmac() => this.Id is >= Md5Id and <= Blake2S256Id and not(Sha224Id or Sha3224Id);

    /// <summary>
    /// Determines whether this hash type supports PBKDF2 key derivation.
    /// </summary>
    /// <returns><c>true</c> if PBKDF2 is supported; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256.SupportsPbkdf2());
    /// Assert.False(HashType.Blake2B_256.SupportsPbkdf2());
    /// </code>
    /// </example>
    /// </remarks>
    public bool SupportsPbkdf2() => this.Id is >= Sha1Id and <= Sha3512Id and not(Sha224Id or Sha3224Id);

    /// <summary>
    /// Creates an HMAC algorithm instance for this hash type with the specified key.
    /// </summary>
    /// <param name="key">The key for the HMAC algorithm.</param>
    /// <returns>An <see cref="HMAC"/> instance, or a keyed BLAKE2 instance for BLAKE2 types.</returns>
    /// <exception cref="InvalidOperationException">Thrown when HMAC is not supported for this hash type.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// using var hmac = HashType.SHA256.CreateHmac("secret-key"u8.ToArray());
    /// var hash = hmac.ComputeHash("message"u8.ToArray());
    /// </code>
    /// </example>
    /// </remarks>
    public object CreateHmac(byte[] key)
    {
        if (!this.SupportsHmac())
        {
            throw new InvalidOperationException($"HMAC is not supported for {this.Name}");
        }

        return this.Id switch
        {
            Md5Id => new HMACMD5(key),
            Sha1Id => new HMACSHA1(key),
            Sha256Id => new HMACSHA256(key),
            Sha384Id => new HMACSHA384(key),
            Sha512Id => new HMACSHA512(key),
            Sha3256Id => new HMACSHA3_256(key),
            Sha3384Id => new HMACSHA3_384(key),
            Sha3512Id => new HMACSHA3_512(key),
            Blake2B256Id => new Blake2B(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 32),
            Blake2B384Id => new Blake2B(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 48),
            Blake2B512Id => new Blake2B(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 64),
            Blake2S128Id => new Blake2S(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 16),
            Blake2S256Id => new Blake2S(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 32),
            _ => throw new InvalidOperationException($"HMAC is not supported for {this.Name}"),
        };
    }

    /// <summary>
    /// Computes the HMAC hash for the specified data using this hash type.
    /// </summary>
    /// <param name="key">The key for the HMAC algorithm.</param>
    /// <param name="data">The data to hash.</param>
    /// <returns>The computed HMAC hash.</returns>
    /// <exception cref="InvalidOperationException">Thrown when HMAC is not supported for this hash type.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA256.ComputeHmac("secret-key"u8.ToArray(), "message"u8.ToArray());
    /// Assert.Equal(32, hash.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] ComputeHmac(byte[] key, byte[] data)
    {
        if (!this.SupportsHmac())
        {
            throw new InvalidOperationException($"HMAC is not supported for {this.Name}");
        }

        var hmac = this.CreateHmac(key);
        try
        {
            return hmac switch
            {
                HMAC h => h.ComputeHash(data),
                Blake2B b => ComputeBlake2BHash(b, data),
                Blake2S s => ComputeBlake2SHash(s, data),
                _ => throw new InvalidOperationException($"Unexpected HMAC type: {hmac.GetType()}"),
            };
        }
        finally
        {
            (hmac as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Attempts to compute the HMAC hash for the specified data into the specified buffer.
    /// </summary>
    /// <param name="key">The key for the HMAC algorithm.</param>
    /// <param name="data">The data to hash.</param>
    /// <param name="destination">The buffer to receive the hash.</param>
    /// <param name="bytesWritten">The number of bytes written to the destination.</param>
    /// <returns><c>true</c> if the hash was computed successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when HMAC is not supported for this hash type.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var destination = new byte[32];
    /// var success = HashType.SHA256.TryComputeHmac("secret-key"u8.ToArray(), "message"u8.ToArray(), destination, out var written);
    /// Assert.True(success);
    /// Assert.Equal(32, written);
    /// </code>
    /// </example>
    /// </remarks>
    public bool TryComputeHmac(byte[] key, ReadOnlySpan<byte> data, Span<byte> destination, out int bytesWritten)
    {
        if (!this.SupportsHmac())
        {
            throw new InvalidOperationException($"HMAC is not supported for {this.Name}");
        }

        if (destination.Length < this.Size)
        {
            bytesWritten = 0;
            return false;
        }

        var hmac = this.CreateHmac(key);
        try
        {
            switch (hmac)
            {
                case HMAC h:
                    {
                        var hash = h.ComputeHash(data.ToArray());
                        hash.CopyTo(destination);
                        bytesWritten = hash.Length;
                        CryptographicOperations.ZeroMemory(hash);
                        return true;
                    }

                case Blake2B b:
                    {
                        b.TransformBlock(data.ToArray(), 0, data.Length, null, 0);
                        b.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        var hash = b.Hash ?? throw new CryptographicException("Blake2B hash computation failed.");
                        hash.CopyTo(destination);
                        bytesWritten = hash.Length;
                        CryptographicOperations.ZeroMemory(hash);
                        return true;
                    }

                case Blake2S s:
                    {
                        s.TransformBlock(data.ToArray(), 0, data.Length, null, 0);
                        s.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        var hash = s.Hash ?? throw new CryptographicException("Blake2S hash computation failed.");
                        hash.CopyTo(destination);
                        bytesWritten = hash.Length;
                        CryptographicOperations.ZeroMemory(hash);
                        return true;
                    }

                default:
                    bytesWritten = 0;
                    return false;
            }
        }
        finally
        {
            (hmac as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Converts this hash type to a <see cref="HashAlgorithmName"/> for use with PBKDF2.
    /// </summary>
    /// <returns>The corresponding <see cref="HashAlgorithmName"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PBKDF2 is not supported for this hash type.</exception>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var name = HashType.SHA256.ToHashAlgorithmName();
    /// Assert.Equal(HashAlgorithmName.SHA256, name);
    /// </code>
    /// </example>
    /// </remarks>
    public HashAlgorithmName ToHashAlgorithmName()
    {
        if (!this.SupportsPbkdf2())
        {
            throw new InvalidOperationException($"PBKDF2 is not supported for {this.Name}");
        }

        return this.Id switch
        {
            Sha1Id => HashAlgorithmName.SHA1,
            Sha256Id => HashAlgorithmName.SHA256,
            Sha384Id => HashAlgorithmName.SHA384,
            Sha512Id => HashAlgorithmName.SHA512,
            Sha3256Id => HashAlgorithmName.SHA3_256,
            Sha3384Id => HashAlgorithmName.SHA3_384,
            Sha3512Id => HashAlgorithmName.SHA3_512,
            _ => throw new InvalidOperationException($"PBKDF2 is not supported for {this.Name}"),
        };
    }

    /// <summary>
    /// Determines whether this instance equals another hash type.
    /// </summary>
    /// <param name="other">The other hash type to compare.</param>
    /// <returns><c>true</c> if the hash types are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256.Equals(HashType.SHA256));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(HashType other) => this.Id == other.Id;

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the objects are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256.Equals((object)HashType.SHA256));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj) => obj is HashType other && this.Equals(other);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = HashType.SHA256.GetHashCode();
    /// Assert.Equal(4, hash);
    /// </code>
    /// </example>
    /// </remarks>
    public override int GetHashCode() => this.Id.GetHashCode();

    /// <summary>
    /// Determines whether two hash types are equal.
    /// </summary>
    /// <param name="left">The first hash type.</param>
    /// <param name="right">The second hash type.</param>
    /// <returns><c>true</c> if the hash types are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256 == HashType.SHA256);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator ==(HashType left, HashType right) => left.Equals(right);

    /// <summary>
    /// Determines whether two hash types are not equal.
    /// </summary>
    /// <param name="left">The first hash type.</param>
    /// <param name="right">The second hash type.</param>
    /// <returns><c>true</c> if the hash types are not equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(HashType.SHA256 != HashType.SHA512);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator !=(HashType left, HashType right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of this hash type.
    /// </summary>
    /// <returns>The name of the hash algorithm.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.Equal("SHA256", HashType.SHA256.ToString());
    /// </code>
    /// </example>
    /// </remarks>
    public override string ToString() => this.Name;

    private static byte[] ComputeBlake2BHash(Blake2B blake2b, byte[] data)
    {
        var hashLength = blake2b.HashSize / 8;
        var key = blake2b.Key;
        using var fresh = new Blake2B(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, hashLength);
        return fresh.ComputeHash(data);
    }

    private static byte[] ComputeBlake2SHash(Blake2S blake2s, byte[] data)
    {
        var hashLength = blake2s.HashSize / 8;
        var key = blake2s.Key;
        using var fresh = new Blake2S(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, hashLength);
        return fresh.ComputeHash(data);
    }
}