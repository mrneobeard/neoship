using System.Security.Cryptography;

namespace NeoBeard.Crypto.Tests;

public class HashExtensionsTests
{
    [Fact]
    public void HashData_SHA256_Returns_Correct_Size()
    {
        var data = "Hello, World!"u8.ToArray();
        var hash = data.HashData(HashAlgorithmName.SHA256);
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void HashData_SHA512_Returns_Correct_Size()
    {
        var data = "Hello, World!"u8.ToArray();
        var hash = data.HashData(HashAlgorithmName.SHA512);
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void HashData_Produces_Consistent_Results()
    {
        var data = "Hello, World!"u8.ToArray();
        var hash1 = data.HashData(HashAlgorithmName.SHA256);
        var hash2 = data.HashData(HashAlgorithmName.SHA256);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashData_Different_Input_Produces_Different_Hash()
    {
        var data1 = "Hello, World!"u8.ToArray();
        var data2 = "Hello, World?"u8.ToArray();
        var hash1 = data1.HashData(HashAlgorithmName.SHA256);
        var hash2 = data2.HashData(HashAlgorithmName.SHA256);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashSHA256_Returns_Correct_Size()
    {
        var data = "Hello, World!"u8.ToArray();
        var hash = data.HashSHA256();
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void HashSHA256_Matches_System_SHA256()
    {
        var data = "Hello, World!"u8.ToArray();
        var hash1 = data.HashSHA256();
        using var sha256 = SHA256.Create();
        var hash2 = sha256.ComputeHash(data);
        Assert.Equal(hash1, hash2);
    }
}