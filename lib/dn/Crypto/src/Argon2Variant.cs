namespace NeoBeard.Crypto;

/// <summary>
/// Identifies an Argon2 password hashing variant.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var variant = Argon2Variant.Argon2id;
/// Assert.Equal(Argon2Variant.Argon2id, variant);
/// </code>
/// </example>
/// </remarks>
public enum Argon2Variant
{
    /// <summary>
    /// Argon2d uses data-dependent memory access.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var variant = Argon2Variant.Argon2d;
    /// Assert.Equal(Argon2Variant.Argon2d, variant);
    /// </code>
    /// </example>
    /// </remarks>
    Argon2d,

    /// <summary>
    /// Argon2i uses data-independent memory access.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var variant = Argon2Variant.Argon2i;
    /// Assert.Equal(Argon2Variant.Argon2i, variant);
    /// </code>
    /// </example>
    /// </remarks>
    Argon2i,

    /// <summary>
    /// Argon2id combines Argon2i and Argon2d indexing and is the recommended default.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var variant = Argon2Variant.Argon2id;
    /// Assert.Equal(Argon2Variant.Argon2id, variant);
    /// </code>
    /// </example>
    /// </remarks>
    Argon2id,
}