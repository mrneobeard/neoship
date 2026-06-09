namespace NeoBeard.Crypto.Tests;

public class ChaCha20Tests
{
    [Fact]
    public void Create_Returns_New_Instance()
    {
        using var chacha = ChaCha20.Create();
        Assert.NotNull(chacha);
    }

    [Fact]
    public void LegalBlockSizes_Returns_64_Bits()
    {
        using var chacha = ChaCha20.Create();
        Assert.Single(chacha.LegalBlockSizes);
        Assert.Equal(64, chacha.LegalBlockSizes[0].MinSize);
    }

    [Fact]
    public void LegalKeySizes_Returns_128_To_256_Bits()
    {
        using var chacha = ChaCha20.Create();
        Assert.Single(chacha.LegalKeySizes);
        Assert.Equal(128, chacha.LegalKeySizes[0].MinSize);
        Assert.Equal(256, chacha.LegalKeySizes[0].MaxSize);
    }

    [Fact]
    public void Rounds_Default_Is_Twenty()
    {
        using var chacha = ChaCha20.Create();
        Assert.Equal(ChaChaRound.Twenty, chacha.Rounds);
    }

    [Fact]
    public void GenerateKey_Creates_32_Byte_Key()
    {
        using var chacha = ChaCha20.Create();
        chacha.GenerateKey();
        Assert.Equal(32, chacha.Key.Length);
    }

    [Fact]
    public void GenerateIV_Creates_8_Byte_IV()
    {
        using var chacha = ChaCha20.Create();
        chacha.GenerateIV();
        Assert.Equal(8, chacha.IV.Length);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_Works()
    {
        using var chacha = ChaCha20.Create();
        chacha.GenerateKey();
        chacha.GenerateIV();

        var plaintext = "Hello, World!"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];

        using (var encryptor = chacha.CreateEncryptor())
        {
            encryptor.TransformBlock(plaintext, 0, plaintext.Length, ciphertext, 0);
        }

        var decrypted = new byte[plaintext.Length];
        using (var decryptor = chacha.CreateDecryptor())
        {
            decryptor.TransformBlock(ciphertext, 0, ciphertext.Length, decrypted, 0);
        }

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void CreateDecryptor_With_Null_IV_Uses_Generated_IV()
    {
        using var chacha = ChaCha20.Create();
        chacha.Key = new byte[32];

        using var decryptor = chacha.CreateDecryptor(chacha.Key, null);
        Assert.NotNull(chacha.IV);
        Assert.Equal(8, chacha.IV.Length);
    }

    [Fact]
    public void SkipXor_Can_Be_Set()
    {
        using var chacha = ChaCha20.Create();
        chacha.SkipXor = true;
        Assert.True(chacha.SkipXor);
    }

    [Fact]
    public void Counter_Can_Be_Set()
    {
        using var chacha = ChaCha20.Create();
        chacha.Counter = 42;
        Assert.Equal(42, chacha.Counter);
    }

    [Fact]
    public void Different_Keys_Produce_Different_Ciphertext()
    {
        using var chacha1 = ChaCha20.Create();
        using var chacha2 = ChaCha20.Create();

        chacha1.GenerateKey();
        chacha1.GenerateIV();
        chacha2.GenerateKey();
        chacha2.IV = chacha1.IV;

        var plaintext = "Hello, World!"u8.ToArray();
        var ciphertext1 = new byte[plaintext.Length];
        var ciphertext2 = new byte[plaintext.Length];

        using (var encryptor1 = chacha1.CreateEncryptor())
        using (var encryptor2 = chacha2.CreateEncryptor())
        {
            encryptor1.TransformBlock(plaintext, 0, plaintext.Length, ciphertext1, 0);
            encryptor2.TransformBlock(plaintext, 0, plaintext.Length, ciphertext2, 0);
        }

        Assert.NotEqual(ciphertext1, ciphertext2);
    }
}