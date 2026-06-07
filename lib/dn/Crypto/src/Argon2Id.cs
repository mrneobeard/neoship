using System;

namespace NeoBeard.Crypto
{
    /// <summary>
    /// Provides Argon2 hashing configured for password hashing.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hash = Argon2Id.DeriveKey("password"u8, "salt"u8, "iv"u8);
    /// Assert.Equal(32, hash.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public static class Argon2Id
    {
        /// <summary>
        /// Derives a fixed-length hash using Argon2id.
        /// </summary>
        /// <param name="password">The user supplied secret.</param>
        /// <param name="salt">The public per-hash salt.</param>
        /// <param name="iv">A per-hash initialization vector to include as extra keyed context.</param>
        /// <param name="parameters">Optional derivation parameters.</param>
        /// <returns>A derived hash byte array.</returns>
        /// <example>
        /// <code lang="csharp">
        /// var parameters = new Argon2Parameters
        /// {
        ///     Iterations = 3,
        ///     MemorySizeKiB = 64,
        ///     DegreeOfParallelism = 1,
        ///     TagLength = 32
        /// };
        /// var hash = Argon2Id.DeriveKey("password"u8, "salt"u8, "iv"u8, parameters);
        /// </code>
        /// </example>
        public static byte[] DeriveKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> iv, Argon2Parameters? parameters = null)
        {
            var source = parameters ?? Argon2Parameters.SecondRecommended;

            var knownSecret = source.KnownSecret ?? Array.Empty<byte>();
            var withIv = new byte[knownSecret.Length + iv.Length];
            var argonParameters = new Argon2Parameters
            {
                Variant = Argon2Variant.Argon2id,
                Iterations = source.Iterations,
                MemorySizeKiB = source.MemorySizeKiB,
                DegreeOfParallelism = source.DegreeOfParallelism,
                TagLength = source.TagLength,
                AssociatedData = (byte[])source.AssociatedData.Clone(),
                KnownSecret = withIv,
            };

            try
            {
                knownSecret.CopyTo(withIv);
                iv.CopyTo(withIv.AsSpan(knownSecret.Length));

                return Argon2.DeriveKey(password, salt, argonParameters);
            }
            finally
            {
                Array.Clear(argonParameters.AssociatedData, 0, argonParameters.AssociatedData.Length);
                Array.Clear(withIv, 0, withIv.Length);
            }
        }
    }
}
