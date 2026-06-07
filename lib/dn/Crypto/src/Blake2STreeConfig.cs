namespace NeoBeard.Crypto;

/// <summary>
/// Describes BLAKE2s tree hashing parameters.
/// </summary>
/// <remarks>
/// <example>
/// <code lang="csharp">
/// var config = new Blake2STreeConfig(8, 2, 0, 0, 0, 32, false);
/// Assert.Equal(8, config.FanOut);
/// </code>
/// </example>
/// </remarks>
public readonly struct Blake2STreeConfig : IEquatable<Blake2STreeConfig>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2STreeConfig"/> struct.
    /// </summary>
    /// <param name="fanOut">The maximum number of child nodes.</param>
    /// <param name="depth">The maximum tree depth.</param>
    /// <param name="leafLength">The leaf length in bytes.</param>
    /// <param name="nodeOffset">The node offset within the current level.</param>
    /// <param name="nodeDepth">The node depth.</param>
    /// <param name="innerHashLength">The inner hash length in bytes.</param>
    /// <param name="isLastNode">A value indicating whether this node is the last node at its level.</param>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 0, 1, 0, 32, true);
    /// Assert.True(config.IsLastNode);
    /// </code>
    /// </example>
    /// </remarks>
    public Blake2STreeConfig(byte fanOut, byte depth, int leafLength, long nodeOffset, byte nodeDepth, int innerHashLength, bool isLastNode)
    {
        if (leafLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leafLength), "Leaf length must be non-negative.");
        }

        if (nodeOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeOffset), "Node offset must be non-negative.");
        }

        if (innerHashLength is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(innerHashLength), "Inner hash length must be between 0 and 32 bytes.");
        }

        this.FanOut = fanOut;
        this.Depth = depth;
        this.LeafLength = leafLength;
        this.NodeOffset = nodeOffset;
        this.NodeDepth = nodeDepth;
        this.InnerHashLength = innerHashLength;
        this.IsLastNode = isLastNode;
    }

    /// <summary>
    /// Gets the sequential BLAKE2s tree configuration.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = Blake2STreeConfig.Sequential;
    /// Assert.Equal(1, config.FanOut);
    /// </code>
    /// </example>
    /// </remarks>
    public static Blake2STreeConfig Sequential => new(1, 1, 0, 0, 0, 0, false);

    /// <summary>
    /// Gets the maximum number of child nodes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = Blake2STreeConfig.Sequential;
    /// Assert.Equal(1, config.FanOut);
    /// </code>
    /// </example>
    /// </remarks>
    public byte FanOut { get; }

    /// <summary>
    /// Gets the maximum tree depth.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = Blake2STreeConfig.Sequential;
    /// Assert.Equal(1, config.Depth);
    /// </code>
    /// </example>
    /// </remarks>
    public byte Depth { get; }

    /// <summary>
    /// Gets the leaf length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 1024, 0, 0, 32, false);
    /// Assert.Equal(1024, config.LeafLength);
    /// </code>
    /// </example>
    /// </remarks>
    public int LeafLength { get; }

    /// <summary>
    /// Gets the node offset within the current level.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 0, 1, 0, 32, false);
    /// Assert.Equal(1L, config.NodeOffset);
    /// </code>
    /// </example>
    /// </remarks>
    public long NodeOffset { get; }

    /// <summary>
    /// Gets the node depth.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 0, 0, 1, 32, true);
    /// Assert.Equal(1, config.NodeDepth);
    /// </code>
    /// </example>
    /// </remarks>
    public byte NodeDepth { get; }

    /// <summary>
    /// Gets the inner hash length in bytes.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 0, 0, 0, 32, false);
    /// Assert.Equal(32, config.InnerHashLength);
    /// </code>
    /// </example>
    /// </remarks>
    public int InnerHashLength { get; }

    /// <summary>
    /// Gets a value indicating whether this node is the last node at its level.
    /// </summary>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var config = new Blake2STreeConfig(8, 2, 0, 0, 1, 32, true);
    /// Assert.True(config.IsLastNode);
    /// </code>
    /// </example>
    /// </remarks>
    public bool IsLastNode { get; }

    /// <summary>
    /// Determines whether this instance equals another BLAKE2s tree configuration.
    /// </summary>
    /// <param name="other">The other configuration to compare.</param>
    /// <returns><c>true</c> if the configurations are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Blake2STreeConfig.Sequential.Equals(Blake2STreeConfig.Sequential));
    /// </code>
    /// </example>
    /// </remarks>
    public bool Equals(Blake2STreeConfig other)
        => this.FanOut == other.FanOut
        && this.Depth == other.Depth
        && this.LeafLength == other.LeafLength
        && this.NodeOffset == other.NodeOffset
        && this.NodeDepth == other.NodeDepth
        && this.InnerHashLength == other.InnerHashLength
        && this.IsLastNode == other.IsLastNode;

    /// <summary>
    /// Determines whether this instance equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><c>true</c> if the object is equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Blake2STreeConfig.Sequential.Equals((object)Blake2STreeConfig.Sequential));
    /// </code>
    /// </example>
    /// </remarks>
    public override bool Equals(object? obj) => obj is Blake2STreeConfig other && this.Equals(other);

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var hashCode = Blake2STreeConfig.Sequential.GetHashCode();
    /// Assert.NotEqual(0, hashCode);
    /// </code>
    /// </example>
    /// </remarks>
    public override int GetHashCode()
        => HashCode.Combine(this.FanOut, this.Depth, this.LeafLength, this.NodeOffset, this.NodeDepth, this.InnerHashLength, this.IsLastNode);

    /// <summary>
    /// Determines whether two configurations are equal.
    /// </summary>
    /// <param name="left">The first configuration.</param>
    /// <param name="right">The second configuration.</param>
    /// <returns><c>true</c> if the configurations are equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// Assert.True(Blake2STreeConfig.Sequential == Blake2STreeConfig.Sequential);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator ==(Blake2STreeConfig left, Blake2STreeConfig right) => left.Equals(right);

    /// <summary>
    /// Determines whether two configurations are not equal.
    /// </summary>
    /// <param name="left">The first configuration.</param>
    /// <param name="right">The second configuration.</param>
    /// <returns><c>true</c> if the configurations are not equal; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <example>
    /// <code lang="csharp">
    /// var tree = new Blake2STreeConfig(8, 2, 0, 0, 0, 32, false);
    /// Assert.True(Blake2STreeConfig.Sequential != tree);
    /// </code>
    /// </example>
    /// </remarks>
    public static bool operator !=(Blake2STreeConfig left, Blake2STreeConfig right) => !left.Equals(right);
}