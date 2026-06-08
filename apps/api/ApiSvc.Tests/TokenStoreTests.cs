using System.Text;
using NeoShip.ApiSvc.Stores;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class TokenStoreTests
{
    [Fact]
    public void GenerateSessionToken_ReturnsTokenAndDigest()
    {
        var store = new TokenStore();
        var (rawToken, digest) = store.GenerateSessionToken();
        Assert.False(string.IsNullOrEmpty(rawToken));
        Assert.NotEmpty(digest);
    }

    [Fact]
    public void ComputeDigest_SameInput_ReturnsSameOutput()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        var d1 = TokenStore.ComputeDigest(data);
        var d2 = TokenStore.ComputeDigest(data);
        Assert.Equal(d1, d2);
    }

    [Fact]
    public void ComputeDigest_DifferentInput_ReturnsDifferentOutput()
    {
        var d1 = TokenStore.ComputeDigest(Encoding.UTF8.GetBytes("alpha"));
        var d2 = TokenStore.ComputeDigest(Encoding.UTF8.GetBytes("beta"));
        Assert.NotEqual(d1, d2);
    }

    [Fact]
    public void ComputeDigestBase64_ReturnsValidBase64()
    {
        var result = TokenStore.ComputeDigestBase64("test");
        var bytes = Convert.FromBase64String(result);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateResetToken_ReturnsUniqueTokens()
    {
        var store = new TokenStore();
        var t1 = store.GenerateResetToken();
        var t2 = store.GenerateResetToken();
        Assert.NotEqual(t1, t2);
    }
}
