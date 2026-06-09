namespace NeoBeard.Crypto.Tests;

public class ShamirSecretSharingTests
{
    [Fact]
    public void Combine_Recovers_Secret_With_Threshold_Shares()
    {
        var secret = "correct horse battery staple"u8.ToArray();
        var shares = ShamirSecretSharing.Split(secret, 3, 5);

        var recovered = ShamirSecretSharing.Combine(new[] { shares[0], shares[2], shares[4] });

        Assert.Equal(secret, recovered);
    }

    [Fact]
    public void Split_Creates_Different_Shares()
    {
        var shares = ShamirSecretSharing.Split("secret"u8.ToArray(), 2, 3);

        Assert.NotEqual(shares[0].Value, shares[1].Value);
    }

    [Fact]
    public void Combine_Rejects_Duplicate_Shares()
    {
        var shares = ShamirSecretSharing.Split("secret"u8.ToArray(), 2, 3);

        Assert.Throws<ArgumentException>(() => ShamirSecretSharing.Combine(new[] { shares[0], shares[0] }));
    }

    [Fact]
    public void Split_Rejects_Invalid_Threshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShamirSecretSharing.Split("secret"u8.ToArray(), 1, 3));
    }
}