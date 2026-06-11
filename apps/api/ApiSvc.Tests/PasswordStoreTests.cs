namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class PasswordHashingTests
{
    [Fact]
    public void Hash_ReturnsNonNullString()
    {
        var hash = PasswordHashing.Hash("test-password");
        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHash()
    {
        var hash1 = PasswordHashing.Hash("test-password");
        var hash2 = PasswordHashing.Hash("test-password");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsSuccess()
    {
        var hash = PasswordHashing.Hash("correct-password");
        var (success, _) = PasswordHashing.Verify("correct-password", hash);
        Assert.True(success);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFailed()
    {
        var hash = PasswordHashing.Hash("correct-password");
        var (success, _) = PasswordHashing.Verify("wrong-password", hash);
        Assert.False(success);
    }

    [Fact]
    public void Verify_EmptyInput_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PasswordHashing.Hash(""));
    }
}