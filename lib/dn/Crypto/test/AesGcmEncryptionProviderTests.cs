namespace NeoBeard.Crypto.Tests;

public static class AesGcmEncryptionProviderTests
{
    [Fact]
    public static void AesGcm_BlackBoxTest()
    {
        var options = new AesGcmEncryptionProviderOptions();
        options.WithKey("whatever man");

        var provider = new AesGcmEncryptionProvider(options);

        var data = new byte[] { 1, 2, 3, 4, 5 };
        var encryptedData = provider.Encrypt(data);

        var decryptedData = provider.Decrypt(encryptedData);

        Assert.NotEqual(data, encryptedData);
        Assert.Equal(data, decryptedData.ToArray());
    }

    [Fact]
    public static void AesGcm_BlackBoxTestWithText()
    {
        var options = new AesGcmEncryptionProviderOptions();
        options.WithKey("whatever man");

        var text = "some basic text!";
        var data = System.Text.Encoding.UTF8.GetBytes(text);
        var provider = new AesGcmEncryptionProvider(options);
        var encryptedData = provider.Encrypt(data);

        var decryptedData = provider.Decrypt(encryptedData);

        Assert.NotEqual(data, encryptedData);
        Assert.Equal(data, decryptedData.ToArray());
        Assert.Equal(text, System.Text.Encoding.UTF8.GetString(decryptedData.ToArray()));
    }
}