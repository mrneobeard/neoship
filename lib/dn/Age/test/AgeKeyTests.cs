namespace NeoBeard.Age.Tests;

public class AgeKeyTests
{
    [Fact]
    public void Create_Generates_Usable_Key_Pair()
    {
        var key = AgeKey.Create();

        Assert.StartsWith("age1", key.PublicKey);
        Assert.StartsWith("AGE-SECRET-KEY-", key.PrivateKey);
        Assert.Equal("X25519", AgeRecipient.FromPublicKey(key.PublicKey).Kind);
        Assert.Equal("X25519", AgeIdentity.FromPrivateKey(key.PrivateKey).Kind);
    }

    [Fact]
    public void Known_Secret_Produces_Expected_Public_Key()
    {
        var publicKey = Bech32.Encode("age", X25519.ScalarMultBase(new byte[32]));

        Assert.Equal("age19ljhmg68e43yx9fgm2k9lwefquc0la5y4lzvlshdjzv47kxt8d6qr9vf4p", publicKey);
    }
}