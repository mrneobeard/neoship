using NeoShip.ApiSvc.Stores;

namespace NeoShip.ApiSvc.Tests;

[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class PasswordStoreTests
{
    [Fact]
    public void Hash_ReturnsNonNullString()
    {
        var store = new PasswordStore();
        var hash = store.Hash("test-password");
        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHash()
    {
        var store = new PasswordStore();
        var hash1 = store.Hash("test-password");
        var hash2 = store.Hash("test-password");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsSuccess()
    {
        var store = new PasswordStore();
        var hash = store.Hash("correct-password");
        var (success, _) = store.Verify("correct-password", hash);
        Assert.True(success);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFailed()
    {
        var store = new PasswordStore();
        var hash = store.Hash("correct-password");
        var (success, _) = store.Verify("wrong-password", hash);
        Assert.False(success);
    }

    [Fact]
    public void Verify_EmptyInput_ThrowsArgumentOutOfRange()
    {
        var store = new PasswordStore();
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Hash(""));
    }
}
