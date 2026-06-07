using System.Security.Cryptography;

namespace NeoBeard.Age.Tests;

public class SshRsaKeyTests
{
    [Fact]
    public void ExportPublicKey_Can_Be_Parsed_As_Recipient()
    {
        using var rsa = RSA.Create(2048);

        var key = SshRsaKey.ExportPublicKey(rsa);
        var recipient = AgeRecipient.FromSshPublicKey(key);

        Assert.StartsWith("ssh-rsa ", key);
        Assert.Equal("ssh-rsa", recipient.Kind);
    }

    [Fact]
    public void FromSshPublicKey_Rejects_Unsupported_Key_Type()
    {
        Assert.Throws<FormatException>(() => AgeRecipient.FromSshPublicKey("ssh-dss AAAAB3NzaC1kc3MA"));
    }
}