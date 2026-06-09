using System.Text;

namespace NeoBeard.Age.Tests;

public class SshEd25519KeyTests
{
    private const string PublicKey = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIB1t0ub2F9lTZ6t6m5yNsWfkMie4XDT+6H7SHJDdBPp3 dev@dev-atom";

    private const string PrivateKey = """
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAABG5vbmUAAAAEbm9uZQAAAAAAAAABAAAAMwAAAAtzc2gtZW
QyNTUxOQAAACAdbdLm9hfZU2erepucjbFn5DInuFw0/uh+0hyQ3QT6dwAAAJAEIfZOBCH2
TgAAAAtzc2gtZWQyNTUxOQAAACAdbdLm9hfZU2erepucjbFn5DInuFw0/uh+0hyQ3QT6dw
AAAEBVhCL+YcUCguYWCNrEw0bRNUG17Lnm8V/pton1IvKKqx1t0ub2F9lTZ6t6m5yNsWfk
Mie4XDT+6H7SHJDdBPp3AAAADGRldkBkZXYtYXRvbQE=
-----END OPENSSH PRIVATE KEY-----
""";

    [Fact]
    public void FromSshPublicKey_Parses_Ed25519_Recipient()
    {
        var recipient = AgeRecipient.FromSshPublicKey(PublicKey);

        Assert.Equal("ssh-ed25519", recipient.Kind);
    }

    [Fact]
    public void FromSshPrivateKey_Parses_Ed25519_Identity()
    {
        var identity = AgeIdentity.FromSshPrivateKey(PrivateKey);

        Assert.Equal("ssh-ed25519", identity.Kind);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrips_With_Ssh_Ed25519_Key()
    {
        var plaintext = Encoding.UTF8.GetBytes("ed25519 recipient");
        using var encrypted = new MemoryStream();

        AgeFile.Encrypt(new MemoryStream(plaintext), encrypted, [AgeRecipient.FromSshPublicKey(PublicKey)]);
        encrypted.Position = 0;
        using var output = new MemoryStream();
        AgeFile.Decrypt(encrypted, output, [AgeIdentity.FromSshPrivateKey(PrivateKey)]);

        Assert.Equal(plaintext, output.ToArray());
    }
}