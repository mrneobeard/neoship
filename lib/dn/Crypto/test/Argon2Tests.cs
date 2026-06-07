namespace NeoBeard.Crypto.Tests;

public class Argon2Tests
{
    [Theory]
    [InlineData(Argon2Variant.Argon2d, "512B391B6F1162975371D30919734294F868E3BE3984F3C1A13A4DB9FABE4ACB")]
    [InlineData(Argon2Variant.Argon2i, "C814D9D1DC7F37AA13F0D77F2494BDA1C8DE6B016DD388D29952A4C4672B6CE8")]
    [InlineData(Argon2Variant.Argon2id, "0D640DF58D78766C08C037A34A8B53C9D01EF0452D75B65EB52520E96B01E659")]
    public void DeriveKey_Matches_Rfc9106_Test_Vector(Argon2Variant variant, string expectedHex)
    {
        var parameters = new Argon2Parameters
        {
            Variant = variant,
            Iterations = 3,
            MemorySizeKiB = 32,
            DegreeOfParallelism = 4,
            TagLength = 32,
            AssociatedData = Enumerable.Repeat((byte)0x04, 12).ToArray(),
            KnownSecret = Enumerable.Repeat((byte)0x03, 8).ToArray(),
        };

        var password = Enumerable.Repeat((byte)0x01, 32).ToArray();
        var salt = Enumerable.Repeat((byte)0x02, 16).ToArray();
        var key = Argon2.DeriveKey(password, salt, parameters);

        Assert.Equal(expectedHex, Convert.ToHexString(key));
    }

    [Fact]
    public void DeriveKey_Changes_When_AssociatedData_Changes()
    {
        var first = new Argon2Parameters
        {
            MemorySizeKiB = 32,
            DegreeOfParallelism = 1,
            Iterations = 2,
            TagLength = 16,
            AssociatedData = "first"u8.ToArray(),
        };
        var second = new Argon2Parameters
        {
            MemorySizeKiB = 32,
            DegreeOfParallelism = 1,
            Iterations = 2,
            TagLength = 16,
            AssociatedData = "second"u8.ToArray(),
        };

        var password = "password"u8.ToArray();
        var salt = "1234567890123456"u8.ToArray();

        Assert.NotEqual(Argon2.DeriveKey(password, salt, first), Argon2.DeriveKey(password, salt, second));
    }

    [Fact]
    public void DeriveKey_Rejects_Too_Little_Memory()
    {
        var parameters = new Argon2Parameters
        {
            MemorySizeKiB = 7,
            DegreeOfParallelism = 1,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => Argon2.DeriveKey("password"u8, "salt"u8, parameters));
    }
}