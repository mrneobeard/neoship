using System.Text;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class TokenGeneratorTests
{
    [Fact]
    public void GenerateSessionToken_ReturnsTokenAndDigest()
    {
        var (rawToken, digest) = TokenGenerator.GenerateSessionToken();
        Assert.False(string.IsNullOrEmpty(rawToken));
        Assert.NotEmpty(digest);
    }

    [Fact]
    public void ComputeDigest_SameInput_ReturnsSameOutput()
    {
        var data = Encoding.UTF8.GetBytes("hello world");
        var d1 = TokenGenerator.ComputeDigest(data);
        var d2 = TokenGenerator.ComputeDigest(data);
        Assert.Equal(d1, d2);
    }

    [Fact]
    public void ComputeDigest_DifferentInput_ReturnsDifferentOutput()
    {
        var d1 = TokenGenerator.ComputeDigest(Encoding.UTF8.GetBytes("alpha"));
        var d2 = TokenGenerator.ComputeDigest(Encoding.UTF8.GetBytes("beta"));
        Assert.NotEqual(d1, d2);
    }

    [Fact]
    public void ComputeDigestBase64_ReturnsValidBase64()
    {
        var result = TokenGenerator.ComputeDigestBase64("test");
        var bytes = Convert.FromBase64String(result);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateResetToken_ReturnsUniqueTokens()
    {
        var t1 = TokenGenerator.GenerateResetToken();
        var t2 = TokenGenerator.GenerateResetToken();
        Assert.NotEqual(t1, t2);
    }
}