namespace NeoBeard.Crypto.Tests;

public class Blake2ParallelTests
{
    [Fact]
    public void Blake2Bp_Matches_Explicit_Striped_Tree()
    {
        var data = Enumerable.Range(0, 4097).Select(static value => (byte)value).ToArray();
        using var blake = new Blake2Bp();

        var actual = blake.ComputeHash(data);
        var expected = ComputeBlake2Bp(data);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Blake2Sp_Matches_Explicit_Striped_Tree()
    {
        var data = Enumerable.Range(0, 4097).Select(static value => (byte)value).ToArray();
        using var blake = new Blake2Sp();

        var actual = blake.ComputeHash(data);
        var expected = ComputeBlake2Sp(data);

        Assert.Equal(expected, actual);
    }

    private static byte[] ComputeBlake2Bp(byte[] data)
    {
        const int parallelism = 4;
        const int outBytes = 64;
        var leafHashes = new byte[parallelism * outBytes];
        for (var i = 0; i < parallelism; i++)
        {
            var leafData = Stripe(data, i, parallelism, 128);
            var config = new Blake2BTreeConfig(parallelism, 2, 0, i, 0, outBytes, i == parallelism - 1);
            using var leaf = new Blake2B(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, outBytes, config);
            leaf.ComputeHash(leafData).CopyTo(leafHashes.AsSpan(i * outBytes));
        }

        var rootConfig = new Blake2BTreeConfig(parallelism, 2, 0, 0, 1, outBytes, true);
        using var root = new Blake2B(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, outBytes, rootConfig);
        return root.ComputeHash(leafHashes);
    }

    private static byte[] ComputeBlake2Sp(byte[] data)
    {
        const int parallelism = 8;
        const int outBytes = 32;
        var leafHashes = new byte[parallelism * outBytes];
        for (var i = 0; i < parallelism; i++)
        {
            var leafData = Stripe(data, i, parallelism, 64);
            var config = new Blake2STreeConfig(parallelism, 2, 0, i, 0, outBytes, i == parallelism - 1);
            using var leaf = new Blake2S(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, outBytes, config);
            leaf.ComputeHash(leafData).CopyTo(leafHashes.AsSpan(i * outBytes));
        }

        var rootConfig = new Blake2STreeConfig(parallelism, 2, 0, 0, 1, outBytes, true);
        using var root = new Blake2S(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, outBytes, rootConfig);
        return root.ComputeHash(leafHashes);
    }

    private static byte[] Stripe(byte[] data, int leaf, int parallelism, int stripeLength)
    {
        using var stream = new MemoryStream();
        for (var offset = leaf * stripeLength; offset < data.Length; offset += parallelism * stripeLength)
        {
            stream.Write(data, offset, Math.Min(stripeLength, data.Length - offset));
        }

        return stream.ToArray();
    }
}