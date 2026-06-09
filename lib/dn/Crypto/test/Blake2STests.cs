namespace NeoBeard.Crypto.Tests;

public class Blake2STests
{
    [Theory]
    [InlineData("", "69217A3079908094E11121D042354A7C1F55B6482CA1A51E1B250DFD1ED0EEF9")]
    [InlineData("abc", "508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982")]
    public void ComputeHash_Matches_Official_Blake2S256_Vectors(string input, string expectedHex)
    {
        using var blake = new Blake2S();
        var hash = blake.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
        Assert.Equal(expectedHex, Convert.ToHexString(hash));
    }

    [Fact]
    public void TransformBlock_Matches_OneShot_Hash()
    {
        var data = Enumerable.Range(0, 512).Select(static value => (byte)value).ToArray();

        using var oneShot = new Blake2S();
        var expected = oneShot.ComputeHash(data);

        using var incremental = new Blake2S();
        incremental.TransformBlock(data, 0, 11, null, 0);
        incremental.TransformBlock(data, 11, 213, null, 0);
        incremental.TransformFinalBlock(data, 224, data.Length - 224);

        Assert.Equal(expected, incremental.Hash);
    }

    [Fact]
    public void CreateHmac_For_Blake2S_Uses_Keyed_Blake2S()
    {
        var key = "secret"u8.ToArray();
        var data = "message"u8.ToArray();

        var actual = HashType.Blake2S_256.ComputeHmac(key, data);
        using var expectedBlake = new Blake2S(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 32);
        var expected = expectedBlake.ComputeHash(data);

        Assert.Equal(expected, actual);
    }
}