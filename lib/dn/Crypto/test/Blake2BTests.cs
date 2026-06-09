using System.Security.Cryptography;

namespace NeoBeard.Crypto.Tests;

public class Blake2BTests
{
    [Theory]
    [InlineData("", "786A02F742015903C6C6FD852552D272912F4740E15847618A86E217F71F5419D25E1031AFEE585313896444934EB04B903A685B1448B755D56F701AFE9BE2CE")]
    [InlineData("abc", "BA80A53F981C4D0D6A2797B69F12F6E94C212F14685AC4B74B12BB6FDBFFA2D17D87C5392AAB792DC252D5DE4533CC9518D38AA8DBF1925AB92386EDD4009923")]
    public void ComputeHash_Matches_Official_Blake2B512_Vectors(string input, string expectedHex)
    {
        using var blake = new Blake2B();
        var hash = blake.ComputeHash(System.Text.Encoding.ASCII.GetBytes(input));
        Assert.Equal(expectedHex, Convert.ToHexString(hash));
    }

    [Fact]
    public void ComputeHash_Matches_Official_Blake2B256_Empty_Vector()
    {
        using var blake = new Blake2B(32);
        var hash = blake.ComputeHash(Array.Empty<byte>());
        Assert.Equal("0E5751C026E543B2E8AB2EB06099DAA1D1E5DF47778F7787FAAB45CDF12FE3A8", Convert.ToHexString(hash));
    }

    [Fact]
    public void TransformBlock_Matches_OneShot_Hash()
    {
        var data = Enumerable.Range(0, 1024).Select(static value => (byte)value).ToArray();

        using var oneShot = new Blake2B();
        var expected = oneShot.ComputeHash(data);

        using var incremental = new Blake2B();
        incremental.TransformBlock(data, 0, 17, null, 0);
        incremental.TransformBlock(data, 17, 619, null, 0);
        incremental.TransformFinalBlock(data, 636, data.Length - 636);

        Assert.Equal(expected, incremental.Hash);
    }

    [Fact]
    public void KeyedHash_Uses_Key_Salt_And_Personalization()
    {
        var key = "key"u8.ToArray();
        var salt = "1234567890abcdef"u8.ToArray();
        var personalization = "personalization!"u8.ToArray();

        using var first = new Blake2B(key, salt, personalization, 64);
        using var second = new Blake2B(key, salt, personalization, 64);
        using var differentSalt = new Blake2B(key, "abcdef1234567890"u8.ToArray(), personalization, 64);

        var data = "message"u8.ToArray();
        var hash = first.ComputeHash(data);

        Assert.Equal(second.ComputeHash(data), hash);
        Assert.NotEqual(differentSalt.ComputeHash(data), hash);
        Assert.Equal(salt, first.Salt);
        Assert.Equal(personalization, first.Personalization);
    }

    [Fact]
    public void ComputeTreeHash_Uses_Tree_Parameters()
    {
        var data = Enumerable.Range(0, 200).Select(static value => (byte)value).ToArray();

        var treeHash = Blake2B.ComputeTreeHash(data, 64, 100, 2);
        using var sequential = new Blake2B();
        var sequentialHash = sequential.ComputeHash(data);

        Assert.Equal(64, treeHash.Length);
        Assert.NotEqual(sequentialHash, treeHash);
    }

    [Fact]
    public void CreateHmac_For_Blake2B_Uses_Keyed_Blake2B()
    {
        var key = "secret"u8.ToArray();
        var data = "message"u8.ToArray();

        var actual = HashType.Blake2B_256.ComputeHmac(key, data);
        using var expectedBlake = new Blake2B(key, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, 32);
        var expected = expectedBlake.ComputeHash(data);

        Assert.Equal(expected, actual);
    }
}