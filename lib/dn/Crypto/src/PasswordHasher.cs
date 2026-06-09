using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Crypto
{
    /// <summary>
    /// Identifies algorithms supported by <see cref="PasswordHasher"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hasher = new PasswordHasher(PasswordHashAlgorithm.Argon2Id);
    /// Assert.Equal(PasswordHashAlgorithm.Argon2Id, hasher.PreferredAlgorithm);
    /// </code>
    /// </remarks>
    public enum PasswordHashAlgorithm : byte
    {
        /// <summary>
        /// Uses Argon2id.
        /// </summary>
        Argon2Id = 1,

        /// <summary>
        /// Uses PBKDF2.
        /// </summary>
        Pbkdf2 = 2,
    }

    /// <summary>
    /// Identifies PBKDF2 hash algorithm values used by <see cref="PasswordHasher"/>.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new PasswordHasherOptions { Pbkdf2HashAlgorithm = PasswordPbkdf2Algorithm.SHA256 };
    /// </code>
    /// </example>
    /// </remarks>
    public enum PasswordPbkdf2Algorithm : byte
    {
        /// <summary>
        /// SHA-1 digest.
        /// </summary>
        SHA1 = 1,

        /// <summary>
        /// SHA-256 digest.
        /// </summary>
        SHA256 = 2,

        /// <summary>
        /// SHA-384 digest.
        /// </summary>
        SHA384 = 3,

        /// <summary>
        /// SHA-512 digest.
        /// </summary>
        SHA512 = 4,
    }

    /// <summary>
    /// Defines password hashing policy and algorithm parameters.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var options = new PasswordHasherOptions
    /// {
    ///     PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
    ///     Argon2Parameters = new Argon2Parameters { Iterations = 2, MemorySizeKiB = 64, DegreeOfParallelism = 1 }
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class PasswordHasherOptions
    {
        private const int DefaultSaltSize = 16;
        private const int DefaultIvSize = 16;
        private const int DefaultHashLength = 32;
        private const int DefaultPbkdf2Iterations = 120000;

        /// <summary>
        /// Gets or sets the algorithm used by <see cref="PasswordHasher.Hash(ReadOnlySpan{byte})"/> when no algorithm override is supplied.
        /// </summary>
        /// <value>The preferred algorithm to encode in generated hashes.</value>
        public PasswordHashAlgorithm PreferredAlgorithm { get; set; } = PasswordHashAlgorithm.Argon2Id;

        /// <summary>
        /// Gets or sets the length in bytes of generated random salt.
        /// </summary>
        /// <value>A salt byte length.</value>
        public int SaltLength { get; set; } = DefaultSaltSize;

        /// <summary>
        /// Gets or sets the length in bytes of generated initialization vector payload.
        /// </summary>
        /// <value>An IV byte length.</value>
        public int IvLength { get; set; } = DefaultIvSize;

        /// <summary>
        /// Gets or sets the derived hash length in bytes.
        /// </summary>
        /// <value>A hash byte length.</value>
        public int HashLength { get; set; } = DefaultHashLength;

        /// <summary>
        /// Gets or sets PBKDF2 iteration count.
        /// </summary>
        /// <value>The PBKDF2 iteration count.</value>
        public int Pbkdf2Iterations { get; set; } = DefaultPbkdf2Iterations;

        /// <summary>
        /// Gets or sets PBKDF2 hash algorithm.
        /// </summary>
        /// <value>The PBKDF2 digest.</value>
        public PasswordPbkdf2Algorithm Pbkdf2HashAlgorithm { get; set; } = PasswordPbkdf2Algorithm.SHA256;

        /// <summary>
        /// Gets or sets Argon2 parameters when <see cref="PreferredAlgorithm"/> is <see cref="PasswordHashAlgorithm.Argon2Id"/>.
        /// </summary>
        /// <value>The <see cref="Argon2Parameters"/> values used for new hashes.</value>
        public Argon2Parameters Argon2Parameters { get; set; } = Argon2Parameters.SecondRecommended;

        internal void Validate()
        {
            if (this.SaltLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.SaltLength), "Salt length must be a positive number.");

            if (this.IvLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.IvLength), "IV length must be a positive number.");

            if (this.HashLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.HashLength), "Hash length must be a positive number.");

            if (this.Pbkdf2Iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(this.Pbkdf2Iterations), "PBKDF2 iterations must be a positive number.");

            if (!Enum.IsDefined(this.Pbkdf2HashAlgorithm))
                throw new ArgumentOutOfRangeException(nameof(this.Pbkdf2HashAlgorithm), "Unsupported PBKDF2 digest.");

            if (this.PreferredAlgorithm == PasswordHashAlgorithm.Argon2Id)
            {
                if (this.Argon2Parameters is null)
                    throw new ArgumentNullException(nameof(this.Argon2Parameters), "Argon2 parameters are required for Argon2id.");

                if (this.Argon2Parameters.Iterations <= 0)
                    throw new ArgumentOutOfRangeException(nameof(this.Argon2Parameters), "Argon2 iterations must be a positive number.");

                if (this.Argon2Parameters.DegreeOfParallelism <= 0)
                    throw new ArgumentOutOfRangeException(nameof(this.Argon2Parameters), "Argon2 parallelism must be a positive number.");

                if (this.Argon2Parameters.MemorySizeKiB < (8 * this.Argon2Parameters.DegreeOfParallelism))
                    throw new ArgumentOutOfRangeException(nameof(this.Argon2Parameters), "Argon2 memory must be at least 8 KiB per lane.");

                if (this.Argon2Parameters.TagLength <= 0)
                    throw new ArgumentOutOfRangeException(nameof(this.Argon2Parameters), "Argon2 tag length must be a positive number.");
            }
        }
    }

    /// <summary>
    /// Hashes and verifies passwords using pluggable algorithms.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hasher = new PasswordHasher();
    /// var hash = hasher.Hash("correct horse battery staple");
    /// var result = hasher.Verify("correct horse battery staple", hash);
    /// </code>
    /// </example>
    /// </remarks>
    public class PasswordHasher : IPasswordHasher
    {
        private const int HeaderVersion = 1;
        private const int CurrentMaxHeaderFieldBytes = 64 * 1024 * 1024;
        private static readonly byte[] FileHeader = "PBHD"u8.ToArray();

        private readonly PasswordHasherOptions options;

        /// <summary>
        /// Initializes a new <see cref="PasswordHasher"/> with default policy (<see cref="PasswordHashAlgorithm.Argon2Id"/>).
        /// </summary>
        public PasswordHasher()
            : this(new PasswordHasherOptions())
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PasswordHasher"/> with a preferred algorithm.
        /// </summary>
        /// <param name="preferredAlgorithm">Preferred hashing algorithm.</param>
        /// <returns><see cref="PasswordHasher"/> configured with the selected default.</returns>
        public PasswordHasher(PasswordHashAlgorithm preferredAlgorithm)
            : this(new PasswordHasherOptions { PreferredAlgorithm = preferredAlgorithm })
        {
        }

        /// <summary>
        /// Initializes a new <see cref="PasswordHasher"/> with explicit options.
        /// </summary>
        /// <param name="options">Password hashing options.</param>
        public PasswordHasher(PasswordHasherOptions options)
        {
            this.options = Clone(options);
            this.options.Validate();

            if (this.options.Argon2Parameters is null)
                this.options.Argon2Parameters = Argon2Parameters.SecondRecommended;
        }

        /// <summary>
        /// Gets the algorithm used for new hashes when no algorithm override is supplied.
        /// </summary>
        /// <value>The preferred algorithm value.</value>
        public PasswordHashAlgorithm PreferredAlgorithm => this.options.PreferredAlgorithm;

        /// <inheritdoc />
        public string Hash(string password)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                return this.Hash(passwordBytes);
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        /// <inheritdoc />
        public string Hash(ReadOnlySpan<byte> password)
        {
            return this.Hash(password, this.options.PreferredAlgorithm);
        }

        /// <summary>
        /// Hashes password bytes using a specific algorithm.
        /// </summary>
        /// <param name="password">The password bytes.</param>
        /// <param name="algorithm">The password hash algorithm.</param>
        /// <returns>An encoded password hash payload.</returns>
        public string Hash(ReadOnlySpan<byte> password, PasswordHashAlgorithm algorithm)
        {
            this.options.Validate();
            if (password.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(password), "Password must contain bytes.");

            return algorithm switch
            {
                PasswordHashAlgorithm.Argon2Id => this.HashWithArgon2(password),
                PasswordHashAlgorithm.Pbkdf2 => this.HashWithPbkdf2(password),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm), "Unsupported password hashing algorithm."),
            };
        }

        /// <inheritdoc />
        public PasswordHashResult Verify(string password, string hash)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            if (hash is null)
                throw new ArgumentNullException(nameof(hash));

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = Encoding.UTF8.GetBytes(hash);
            try
            {
                return this.Verify(passwordBytes, hashBytes);
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(hashBytes, 0, hashBytes.Length);
            }
        }

        /// <inheritdoc />
        public PasswordHashResult Verify(ReadOnlySpan<byte> password, ReadOnlySpan<byte> hash)
        {
            this.options.Validate();
            if (password.IsEmpty)
                return PasswordHashResult.Failed;

            byte[]? decoded = null;
            PasswordHashRecord? record = null;
            byte[]? expectedHash = null;
            byte[]? passwordBytes = null;

            try
            {
                decoded = DecodeHashBytes(hash);
                if (decoded == null || decoded.Length == 0)
                    return PasswordHashResult.Failed;

                if (!TryParseRecord(decoded, out var parsedRecord))
                    return PasswordHashResult.Failed;

                record = parsedRecord;

                if (record.Hash.Length == 0)
                    return PasswordHashResult.Failed;

                passwordBytes = password.ToArray();
                expectedHash = record.Algorithm switch
                {
                    PasswordHashAlgorithm.Argon2Id => DeriveArgon2(passwordBytes, record.Salt, record.Iv, record),
                    PasswordHashAlgorithm.Pbkdf2 => DerivePbkdf2(passwordBytes, record.Salt, record.Iv, record.Iterations, record.Pbkdf2Algorithm, record.HashLength),
                    _ => throw new InvalidOperationException("Unsupported password hash algorithm."),
                };

                if (!CryptographicOperations.FixedTimeEquals(expectedHash, record.Hash))
                    return PasswordHashResult.Failed;

                return this.IsCurrentPolicy(record)
                    ? PasswordHashResult.Success
                    : PasswordHashResult.NeedsRehash;
            }
            catch (FormatException)
            {
                return PasswordHashResult.Failed;
            }
            finally
            {
                if (passwordBytes != null)
                    Array.Clear(passwordBytes, 0, passwordBytes.Length);

                if (expectedHash != null)
                    Array.Clear(expectedHash, 0, expectedHash.Length);

                record?.Clear();

                if (decoded != null)
                    Array.Clear(decoded, 0, decoded.Length);
            }
        }

        private static PasswordHasherOptions Clone(PasswordHasherOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var source = options.Argon2Parameters ?? Argon2Parameters.SecondRecommended;
            return new PasswordHasherOptions
            {
                PreferredAlgorithm = options.PreferredAlgorithm,
                SaltLength = options.SaltLength,
                IvLength = options.IvLength,
                HashLength = options.HashLength,
                Pbkdf2Iterations = options.Pbkdf2Iterations,
                Pbkdf2HashAlgorithm = options.Pbkdf2HashAlgorithm,
                Argon2Parameters = new Argon2Parameters
                {
                    Variant = source.Variant,
                    Iterations = source.Iterations,
                    MemorySizeKiB = source.MemorySizeKiB,
                    DegreeOfParallelism = source.DegreeOfParallelism,
                    TagLength = source.TagLength,
                    AssociatedData = (byte[])source.AssociatedData.Clone(),
                    KnownSecret = (byte[])source.KnownSecret.Clone(),
                },
            };
        }

        private string HashWithArgon2(ReadOnlySpan<byte> password)
        {
            byte[]? salt = null;
            byte[]? iv = null;
            byte[]? hash = null;
            byte[]? output = null;

            try
            {
                salt = GenerateRandom(this.options.SaltLength);
                iv = GenerateRandom(this.options.IvLength);

                hash = DeriveArgon2(password, salt, iv, new PasswordHashRecord
                {
                    Iterations = this.options.Argon2Parameters.Iterations,
                    MemorySizeKiB = this.options.Argon2Parameters.MemorySizeKiB,
                    Parallelism = this.options.Argon2Parameters.DegreeOfParallelism,
                    HashLength = this.options.HashLength,
                });

                output = BuildBinaryHash(
                    PasswordHashAlgorithm.Argon2Id,
                    this.options.HashLength,
                    this.options.Argon2Parameters.Iterations,
                    this.options.Argon2Parameters.MemorySizeKiB,
                    this.options.Argon2Parameters.DegreeOfParallelism,
                    PasswordPbkdf2Algorithm.SHA256,
                    salt,
                    iv,
                    hash);

                return Convert.ToBase64String(output);
            }
            finally
            {
                if (salt != null)
                    Array.Clear(salt, 0, salt.Length);

                if (iv != null)
                    Array.Clear(iv, 0, iv.Length);

                if (hash != null)
                    Array.Clear(hash, 0, hash.Length);

                if (output != null)
                    Array.Clear(output, 0, output.Length);
            }
        }

        private string HashWithPbkdf2(ReadOnlySpan<byte> password)
        {
            byte[]? salt = null;
            byte[]? iv = null;
            byte[]? hash = null;
            byte[]? output = null;

            try
            {
                salt = GenerateRandom(this.options.SaltLength);
                iv = GenerateRandom(this.options.IvLength);

                hash = DerivePbkdf2(password, salt, iv, this.options.Pbkdf2Iterations, this.options.Pbkdf2HashAlgorithm, this.options.HashLength);
                output = BuildBinaryHash(
                    PasswordHashAlgorithm.Pbkdf2,
                    this.options.HashLength,
                    this.options.Pbkdf2Iterations,
                    0,
                    0,
                    this.options.Pbkdf2HashAlgorithm,
                    salt,
                    iv,
                    hash);

                return Convert.ToBase64String(output);
            }
            finally
            {
                if (salt != null)
                    Array.Clear(salt, 0, salt.Length);

                if (iv != null)
                    Array.Clear(iv, 0, iv.Length);

                if (hash != null)
                    Array.Clear(hash, 0, hash.Length);

                if (output != null)
                    Array.Clear(output, 0, output.Length);
            }
        }

        private bool IsCurrentPolicy(PasswordHashRecord record)
        {
            if (record.Algorithm != this.options.PreferredAlgorithm)
                return false;

            if (record.Algorithm == PasswordHashAlgorithm.Argon2Id)
            {
                var parameters = this.options.Argon2Parameters;
                return record.HashLength == this.options.HashLength
                    && record.Iterations == parameters.Iterations
                    && record.MemorySizeKiB == parameters.MemorySizeKiB
                    && record.Parallelism == parameters.DegreeOfParallelism
                    && record.SaltLength == this.options.SaltLength
                    && record.IvLength == this.options.IvLength;
            }

            return record.HashLength == this.options.HashLength
                && record.Iterations == this.options.Pbkdf2Iterations
                && record.Pbkdf2Algorithm == this.options.Pbkdf2HashAlgorithm
                && record.SaltLength == this.options.SaltLength
                && record.IvLength == this.options.IvLength;
        }

        private static byte[] BuildBinaryHash(
            PasswordHashAlgorithm algorithm,
            int hashLength,
            int iterations,
            int memorySizeKiB,
            int parallelism,
            PasswordPbkdf2Algorithm pbkdf2Algorithm,
            byte[] salt,
            byte[] iv,
            byte[] hash)
        {
            if (hash.Length != hashLength)
                throw new ArgumentException("Hash length does not match requested hash length.", nameof(hash));

            int total = FileHeader.Length
                + 4
                + (sizeof(int) * 6)
                + salt.Length
                + iv.Length
                + hash.Length;

            var output = new byte[total];
            var index = 0;

            FileHeader.CopyTo(output, index);
            index += FileHeader.Length;
            output[index++] = HeaderVersion;
            output[index++] = (byte)algorithm;
            output[index++] = (byte)pbkdf2Algorithm;
            output[index++] = 0;

            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), iterations);
            index += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), memorySizeKiB);
            index += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), parallelism);
            index += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), salt.Length);
            index += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), iv.Length);
            index += sizeof(int);
            BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(index), hash.Length);
            index += sizeof(int);

            Array.Copy(salt, 0, output, index, salt.Length);
            index += salt.Length;
            Array.Copy(iv, 0, output, index, iv.Length);
            index += iv.Length;
            Array.Copy(hash, 0, output, index, hash.Length);

            return output;
        }

        private static byte[]? DecodeHashBytes(ReadOnlySpan<byte> hash)
        {
            if (hash.Length > FileHeader.Length && hash.StartsWith(FileHeader))
                return hash.ToArray();

            try
            {
                return Convert.FromBase64String(Encoding.UTF8.GetString(hash));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static bool TryParseRecord(byte[] payload, out PasswordHashRecord record)
        {
            record = new PasswordHashRecord();
            if (payload.Length < FileHeader.Length + 4 + (sizeof(int) * 6))
                return false;

            var index = 0;
            if (!payload.AsSpan(0, FileHeader.Length).SequenceEqual(FileHeader))
                return false;

            index += FileHeader.Length;
            if (payload[index++] != HeaderVersion)
                return false;

            if (!Enum.IsDefined(typeof(PasswordHashAlgorithm), payload[index]))
                return false;

            record.Algorithm = (PasswordHashAlgorithm)payload[index++];
            record.Pbkdf2Algorithm = (PasswordPbkdf2Algorithm)payload[index++];
            if (record.Algorithm == PasswordHashAlgorithm.Pbkdf2 && !Enum.IsDefined(typeof(PasswordPbkdf2Algorithm), record.Pbkdf2Algorithm))
                return false;

            index++;

            record.Iterations = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);
            record.MemorySizeKiB = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);
            record.Parallelism = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);
            record.SaltLength = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);
            record.IvLength = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);
            record.HashLength = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(index));
            index += sizeof(int);

            if (record.Iterations <= 0 || record.SaltLength <= 0 || record.IvLength <= 0 || record.HashLength <= 0)
                return false;

            if (record.MemorySizeKiB < 0 || record.Parallelism < 0)
                return false;

            if (!IsLegalLength(record.SaltLength) || !IsLegalLength(record.IvLength) || !IsLegalLength(record.HashLength))
                return false;

            if (record.Algorithm == PasswordHashAlgorithm.Argon2Id && (record.MemorySizeKiB <= 0 || record.Parallelism <= 0))
                return false;

            if (record.Algorithm == PasswordHashAlgorithm.Pbkdf2 && record.Parallelism != 0)
                return false;

            var required = checked(index + record.SaltLength + record.IvLength + record.HashLength);
            if (payload.Length != required)
                return false;

            record.Salt = payload.AsSpan(index, record.SaltLength).ToArray();
            index += record.SaltLength;
            record.Iv = payload.AsSpan(index, record.IvLength).ToArray();
            index += record.IvLength;
            record.Hash = payload.AsSpan(index, record.HashLength).ToArray();

            return true;
        }

        private static bool IsLegalLength(int value)
        {
            return value > 0 && value <= CurrentMaxHeaderFieldBytes;
        }

        private static byte[] DeriveArgon2(ReadOnlySpan<byte> password, byte[] salt, byte[] iv, PasswordHashRecord record)
        {
            var parameters = new Argon2Parameters
            {
                Variant = Argon2Variant.Argon2id,
                Iterations = record.Iterations,
                MemorySizeKiB = record.MemorySizeKiB,
                DegreeOfParallelism = record.Parallelism,
                TagLength = record.HashLength,
                AssociatedData = Array.Empty<byte>(),
                KnownSecret = Array.Empty<byte>(),
            };

            try
            {
                return Argon2Id.DeriveKey(password, salt, iv, parameters);
            }
            finally
            {
                Array.Clear(parameters.AssociatedData, 0, parameters.AssociatedData.Length);
                Array.Clear(parameters.KnownSecret, 0, parameters.KnownSecret.Length);
            }
        }

        private static byte[] DerivePbkdf2(byte[] password, byte[] salt, byte[] iv, int iterations, PasswordPbkdf2Algorithm algorithm, int hashLength)
        {
            var saltWithIv = new byte[salt.Length + iv.Length];
            Array.Copy(salt, 0, saltWithIv, 0, salt.Length);
            Array.Copy(iv, 0, saltWithIv, salt.Length, iv.Length);

            var hashAlgorithm = ResolveHashAlgorithm(algorithm);

            try
            {
                return Rfc2898DeriveBytes.Pbkdf2(password, saltWithIv, iterations, hashAlgorithm, hashLength);
            }
            finally
            {
                Array.Clear(saltWithIv, 0, saltWithIv.Length);
            }
        }

        private static byte[] DerivePbkdf2(
            ReadOnlySpan<byte> password,
            byte[] salt,
            byte[] iv,
            int iterations,
            PasswordPbkdf2Algorithm algorithm,
            int hashLength)
        {
            var passwordBytes = password.ToArray();
            try
            {
                return DerivePbkdf2(passwordBytes, salt, iv, iterations, algorithm, hashLength);
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private static byte[] GenerateRandom(int size)
        {
            var bytes = new byte[size];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        private static HashAlgorithmName ResolveHashAlgorithm(PasswordPbkdf2Algorithm algorithm)
        {
            return algorithm switch
            {
                PasswordPbkdf2Algorithm.SHA1 => HashAlgorithmName.SHA1,
                PasswordPbkdf2Algorithm.SHA384 => HashAlgorithmName.SHA384,
                PasswordPbkdf2Algorithm.SHA512 => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA256,
            };
        }

        private class PasswordHashRecord
        {
            public PasswordHashAlgorithm Algorithm;
            public PasswordPbkdf2Algorithm Pbkdf2Algorithm;
            public int Iterations;
            public int MemorySizeKiB;
            public int Parallelism;
            public int SaltLength;
            public int IvLength;
            public int HashLength;
            public byte[] Salt = Array.Empty<byte>();
            public byte[] Iv = Array.Empty<byte>();
            public byte[] Hash = Array.Empty<byte>();

            public void Clear()
            {
                Array.Clear(this.Salt, 0, this.Salt.Length);
                Array.Clear(this.Iv, 0, this.Iv.Length);
                Array.Clear(this.Hash, 0, this.Hash.Length);
            }
        }
    }
}
