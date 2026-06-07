using System.Security.Cryptography;

namespace NeoBeard.Crypto.Tests;

public class Salsa20Tests
{
    [Fact]
    public void Create_Returns_New_Instance()
    {
        using var salsa = Salsa20.Create();
        Assert.NotNull(salsa);
    }

    [Fact]
    public void LegalBlockSizes_Returns_64_Bits()
    {
        using var salsa = Salsa20.Create();
        Assert.Single(salsa.LegalBlockSizes);
        Assert.Equal(64, salsa.LegalBlockSizes[0].MinSize);
    }

    [Fact]
    public void LegalKeySizes_Returns_128_To_256_Bits()
    {
        using var salsa = Salsa20.Create();
        Assert.Single(salsa.LegalKeySizes);
        Assert.Equal(128, salsa.LegalKeySizes[0].MinSize);
        Assert.Equal(256, salsa.LegalKeySizes[0].MaxSize);
    }

    [Fact]
    public void Rounds_Default_Is_Twenty()
    {
        using var salsa = Salsa20.Create();
        Assert.Equal(SalsaRounds.Twenty, salsa.Rounds);
    }

    [Fact]
    public void GenerateKey_Creates_32_Byte_Key()
    {
        using var salsa = Salsa20.Create();
        salsa.GenerateKey();
        Assert.Equal(32, salsa.Key.Length);
    }

    [Fact]
    public void GenerateIV_Creates_8_Byte_IV()
    {
        using var salsa = Salsa20.Create();
        salsa.GenerateIV();
        Assert.Equal(8, salsa.IV.Length);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_Works()
    {
        using var salsa = Salsa20.Create();
        salsa.GenerateKey();
        salsa.GenerateIV();

        var plaintext = "Hello, World!"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];

        using (var encryptor = salsa.CreateEncryptor())
        {
            encryptor.TransformBlock(plaintext, 0, plaintext.Length, ciphertext, 0);
        }

        var decrypted = new byte[plaintext.Length];
        using (var decryptor = salsa.CreateDecryptor())
        {
            decryptor.TransformBlock(ciphertext, 0, ciphertext.Length, decrypted, 0);
        }

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void CreateDecryptor_With_Null_IV_Uses_Existing_IV()
    {
        using var salsa = Salsa20.Create();
        salsa.Key = new byte[32];
        salsa.IV = new byte[8];

        using var decryptor = salsa.CreateDecryptor(salsa.Key, null);
        Assert.NotNull(decryptor);
    }

    [Fact]
    public void SkipXor_Can_Be_Set()
    {
        using var salsa = Salsa20.Create();
        salsa.SkipXor = true;
        Assert.True(salsa.SkipXor);
    }

    [Fact]
    public void Rounds_Can_Be_Changed()
    {
        using var salsa = Salsa20.Create();
        salsa.Rounds = SalsaRounds.Twelve;
        Assert.Equal(SalsaRounds.Twelve, salsa.Rounds);
    }

    [Fact]
    public void Different_Keys_Produce_Different_Ciphertext()
    {
        using var salsa1 = Salsa20.Create();
        using var salsa2 = Salsa20.Create();

        salsa1.GenerateKey();
        salsa1.GenerateIV();
        salsa2.GenerateKey();
        salsa2.IV = salsa1.IV;

        var plaintext = "Hello, World!"u8.ToArray();
        var ciphertext1 = new byte[plaintext.Length];
        var ciphertext2 = new byte[plaintext.Length];

        using (var encryptor1 = salsa1.CreateEncryptor())
        using (var encryptor2 = salsa2.CreateEncryptor())
        {
            encryptor1.TransformBlock(plaintext, 0, plaintext.Length, ciphertext1, 0);
            encryptor2.TransformBlock(plaintext, 0, plaintext.Length, ciphertext2, 0);
        }

        Assert.NotEqual(ciphertext1, ciphertext2);
    }

    [Fact]
    public void Same_Key_And_IV_Produce_Same_Ciphertext()
    {
        using var salsa1 = Salsa20.Create();
        using var salsa2 = Salsa20.Create();

        var key = new byte[32];
        var iv = new byte[8];
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(iv);

        salsa1.Key = key;
        salsa1.IV = iv;
        salsa2.Key = key;
        salsa2.IV = iv;

        var plaintext = "Hello, World!"u8.ToArray();
        var ciphertext1 = new byte[plaintext.Length];
        var ciphertext2 = new byte[plaintext.Length];

        using (var encryptor1 = salsa1.CreateEncryptor())
        using (var encryptor2 = salsa2.CreateEncryptor())
        {
            encryptor1.TransformBlock(plaintext, 0, plaintext.Length, ciphertext1, 0);
            encryptor2.TransformBlock(plaintext, 0, plaintext.Length, ciphertext2, 0);
        }

        Assert.Equal(ciphertext1, ciphertext2);
    }
}