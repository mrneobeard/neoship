namespace NeoBeard.Crypto;

/// <summary>
/// Represents a Shamir secret sharing share.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var share = new ShamirShare(1, "data"u8.ToArray());
/// Assert.Equal(1, share.Index);
/// </code>
/// </example>
/// </remarks>
public sealed class ShamirShare
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShamirShare"/> class.
    /// </summary>
    /// <param name="index">The non-zero share index.</param>
    /// <param name="value">The share bytes.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var share = new ShamirShare(1, "data"u8.ToArray());
    /// Assert.Equal(4, share.Value.Length);
    /// </code>
    /// </example>
    /// </remarks>
    public ShamirShare(byte index, byte[] value)
    {
        if (index == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Share index must be non-zero.");
        }

        ArgumentNullException.ThrowIfNull(value);
        this.Index = index;
        this.Value = value;
    }

    /// <summary>
    /// Gets the one-based share index.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var share = new ShamirShare(1, "data"u8.ToArray());
    /// Assert.Equal(1, share.Index);
    /// </code>
    /// </example>
    /// </remarks>
    public byte Index { get; }

    /// <summary>
    /// Gets the share bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var share = new ShamirShare(1, "data"u8.ToArray());
    /// Assert.Equal("data"u8.ToArray(), share.Value);
    /// </code>
    /// </example>
    /// </remarks>
    public byte[] Value { get; }
}