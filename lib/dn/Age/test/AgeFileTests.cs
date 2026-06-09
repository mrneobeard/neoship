using System.Security.Cryptography;
using System.Text;

namespace NeoBeard.Age.Tests;

public class AgeFileTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrips_With_Age_Key()
    {
        var key = AgeKey.Create();
        var plaintext = Encoding.UTF8.GetBytes(new string('a', (64 * 1024) + 17));
        using var input = new MemoryStream(plaintext);
        using var encrypted = new MemoryStream();

        AgeFile.Encrypt(input, encrypted, [AgeRecipient.FromPublicKey(key.PublicKey)]);
        encrypted.Position = 0;
        using var output = new MemoryStream();
        AgeFile.Decrypt(encrypted, output, [AgeIdentity.FromPrivateKey(key.PrivateKey)]);

        Assert.Equal(plaintext, output.ToArray());
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrips_Empty_File()
    {
        var key = AgeKey.Create();
        using var encrypted = new MemoryStream();

        AgeFile.Encrypt(new MemoryStream(), encrypted, [AgeRecipient.FromPublicKey(key.PublicKey)]);
        encrypted.Position = 0;
        using var output = new MemoryStream();
        AgeFile.Decrypt(encrypted, output, [AgeIdentity.FromPrivateKey(key.PrivateKey)]);

        Assert.Empty(output.ToArray());
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrips_With_Ssh_Rsa_Key()
    {
        using var rsa = RSA.Create(2048);
        var plaintext = "ssh recipient"u8.ToArray();
        var publicKey = SshRsaKey.ExportPublicKey(rsa);
        using var encrypted = new MemoryStream();

        AgeFile.Encrypt(new MemoryStream(plaintext), encrypted, [AgeRecipient.FromSshPublicKey(publicKey)]);
        encrypted.Position = 0;
        using var output = new MemoryStream();
        AgeFile.Decrypt(encrypted, output, [AgeIdentity.FromSshPrivateKey(rsa.ExportRSAPrivateKeyPem())]);

        Assert.Equal(plaintext, output.ToArray());
    }

    [Fact]
    public void Decrypt_Rejects_Tampered_Payload()
    {
        var key = AgeKey.Create();
        using var encrypted = new MemoryStream();
        AgeFile.Encrypt(new MemoryStream("hello"u8.ToArray()), encrypted, [AgeRecipient.FromPublicKey(key.PublicKey)]);
        var bytes = encrypted.ToArray();
        bytes[^1] ^= 1;

        Assert.Throws<AgeException>(() => AgeFile.Decrypt(
            new MemoryStream(bytes),
            new MemoryStream(),
            [AgeIdentity.FromPrivateKey(key.PrivateKey)]));
    }

    [Fact]
    public void File_Apis_RoundTrip()
    {
        var key = AgeKey.Create();
        var directory = Path.Combine(Path.GetTempPath(), "neobeard-age-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var plain = Path.Combine(directory, "plain.txt");
            var age = Path.Combine(directory, "plain.txt.age");
            var output = Path.Combine(directory, "plain.out");
            File.WriteAllText(plain, "file api");

            AgeFile.EncryptFile(plain, age, [AgeRecipient.FromPublicKey(key.PublicKey)]);
            AgeFile.DecryptFile(age, output, [AgeIdentity.FromPrivateKey(key.PrivateKey)]);

            Assert.Equal("file api", File.ReadAllText(output));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}