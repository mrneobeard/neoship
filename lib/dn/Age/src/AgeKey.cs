using System.Security.Cryptography;

namespace NeoBeard.Age;

/// <summary>
/// Represents a generated age X25519 key pair.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var key = AgeKey.Create();
/// Console.WriteLine(key.PublicKey.StartsWith("age1", StringComparison.Ordinal));
/// Console.WriteLine(key.PrivateKey.StartsWith("AGE-SECRET-KEY-", StringComparison.Ordinal));
/// </code>
/// </example>
/// </remarks>
public sealed record AgeKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgeKey"/> class.
    /// </summary>
    /// <param name="publicKey">The age public key.</param>
    /// <param name="privateKey">The age private key.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = new AgeKey("age1z7yqg7kh8yf93jk9l0fg0p9jgne3nv4kh8ysqufd5ym6as4p5pqqk5e5pd", "AGE-SECRET-KEY-1QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQGDZ6K5");
    /// Console.WriteLine(key.PublicKey);
    /// </code>
    /// </example>
    /// </remarks>
    public AgeKey(string publicKey, string privateKey)
    {
        this.PublicKey = publicKey;
        this.PrivateKey = privateKey;
    }

    /// <summary>
    /// Gets the age public key.
    /// </summary>
    /// <value>The age public key.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// Console.WriteLine(key.PublicKey);
    /// </code>
    /// </example>
    /// </remarks>
    public string PublicKey { get; }

    /// <summary>
    /// Gets the age private key.
    /// </summary>
    /// <value>The age private key.</value>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// Console.WriteLine(key.PrivateKey);
    /// </code>
    /// </example>
    /// </remarks>
    public string PrivateKey { get; }

    /// <summary>
    /// Creates a new age X25519 key pair.
    /// </summary>
    /// <returns>A generated age key pair.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var key = AgeKey.Create();
    /// Assert.StartsWith("age1", key.PublicKey);
    /// </code>
    /// </example>
    /// </remarks>
    public static AgeKey Create()
    {
        var secret = RandomNumberGenerator.GetBytes(32);
        var publicKey = X25519.ScalarMultBase(secret);
        return new AgeKey(
            Bech32.Encode("age", publicKey),
            Bech32.Encode("AGE-SECRET-KEY-", secret).ToUpperInvariant());
    }
}