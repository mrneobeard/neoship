namespace NeoBeard.Crypto;

/// <summary>
/// Describes Argon2 key derivation parameters.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var parameters = Argon2Parameters.SecondRecommended;
/// Assert.Equal(Argon2Variant.Argon2id, parameters.Variant);
/// </code>
/// </example>
/// </remarks>
public sealed class Argon2Parameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2Parameters"/> class.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters();
    /// Assert.Equal(3, parameters.Iterations);
    /// </code>
    /// </example>
    /// </remarks>
    public Argon2Parameters()
    {
    }

    /// <summary>
    /// Gets RFC 9106's second recommended Argon2id option for memory-constrained environments.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = Argon2Parameters.SecondRecommended;
    /// Assert.Equal(65536, parameters.MemorySizeKiB);
    /// </code>
    /// </example>
    /// </remarks>
    public static Argon2Parameters SecondRecommended => new()
    {
        Variant = Argon2Variant.Argon2id,
        Iterations = 3,
        MemorySizeKiB = 65536,
        DegreeOfParallelism = 4,
        TagLength = 32,
    };

    /// <summary>
    /// Gets or sets the Argon2 variant.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { Variant = Argon2Variant.Argon2i };
    /// Assert.Equal(Argon2Variant.Argon2i, parameters.Variant);
    /// </code>
    /// </example>
    /// </remarks>
    public Argon2Variant Variant { get; set; } = Argon2Variant.Argon2id;

    /// <summary>
    /// Gets or sets the number of passes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { Iterations = 4 };
    /// Assert.Equal(4, parameters.Iterations);
    /// </code>
    /// </example>
    /// </remarks>
    public int Iterations { get; set; } = 3;

    /// <summary>
    /// Gets or sets the memory size in KiB.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { MemorySizeKiB = 32768 };
    /// Assert.Equal(32768, parameters.MemorySizeKiB);
    /// </code>
    /// </example>
    /// </remarks>
    public int MemorySizeKiB { get; set; } = 65536;

    /// <summary>
    /// Gets or sets the degree of parallelism.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { DegreeOfParallelism = 2 };
    /// Assert.Equal(2, parameters.DegreeOfParallelism);
    /// </code>
    /// </example>
    /// </remarks>
    public int DegreeOfParallelism { get; set; } = 4;

    /// <summary>
    /// Gets or sets the output tag length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { TagLength = 16 };
    /// Assert.Equal(16, parameters.TagLength);
    /// </code>
    /// </example>
    /// </remarks>
    public int TagLength { get; set; } = 32;

    /// <summary>
    /// Gets or sets optional associated data.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { AssociatedData = "context"u8.ToArray() };
    /// Assert.Equal(7, parameters.AssociatedData.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] AssociatedData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets an optional secret value.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var parameters = new Argon2Parameters { KnownSecret = "pepper"u8.ToArray() };
    /// Assert.Equal(6, parameters.KnownSecret.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] KnownSecret { get; set; } = Array.Empty<byte>();
}