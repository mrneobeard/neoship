namespace NeoBeard.Crypto.Tests;

public class Blake3Tests
{
    [Fact]
    public void HashData_Matches_Official_Empty_Vector()
    {
        var hash = Blake3.HashData(Array.Empty<byte>());
        Assert.Equal("AF1349B9F5F9A1A6A0404DEA36DCC9499BCB25C9ADC112B7CC9A93CAE41F3262", Convert.ToHexString(hash));
    }

    [Fact]
    public void HashData_Matches_Official_OneByte_Vector()
    {
        var hash = Blake3.HashData(new byte[] { 0 });
        Assert.Equal("2D3ADEDFF11B61F14C886E35AFA036736DCD87A74D27B5C1510225D0F592E213", Convert.ToHexString(hash));
    }

    [Fact]
    public void KeyedHash_Matches_Official_Empty_Vector()
    {
        var key = "whats the Elvish word for friend"u8.ToArray();
        var hash = Blake3.KeyedHash(key, Array.Empty<byte>());
        Assert.Equal("92B2B75604ED3C761F9D6F62392C8A9227AD0EA3F09573E783F1498A4ED60D26", Convert.ToHexString(hash));
    }

    [Fact]
    public void DeriveKey_Matches_Official_Empty_Vector()
    {
        var hash = Blake3.DeriveKey("BLAKE3 2019-12-27 16:29:52 test vectors context", Array.Empty<byte>());
        Assert.Equal("2CC39783C223154FEA8DFB7C1B1660F2AC2DCBD1C1DE8277B0B0DD39B7E50D7D", Convert.ToHexString(hash));
    }
}