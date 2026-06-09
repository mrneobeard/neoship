using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.OperatingSystem.Tests;

public class KeyringTests
{
    [Fact]
    public void IsOsSupported_ReturnsTrueOnSupportedPlatform()
    {
        Assert.True(Keyring.IsOsSupported);
    }

    [Fact]
    public void SetSecret_And_GetSecret_ReturnsCorrectValue()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"TestService_{Guid.NewGuid():N}";
        var account = "test@example.com";
        var secret = "my-secret-password";

        Keyring.SetSecret(service, account, secret);
        var retrieved = Keyring.GetSecret(service, account);

        Assert.Equal(secret, retrieved);

        Keyring.DeleteSecret(service, account);
    }

    [Fact]
    public void SetSecret_Bytes_And_GetSecretAsBytes_ReturnsCorrectValue()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"TestService_{Guid.NewGuid():N}";
        var account = "test@example.com";
        var secret = new byte[] { 1, 2, 3, 4, 5 };

        Keyring.SetSecret(service, account, secret);
        var retrieved = Keyring.GetSecretAsBytes(service, account);

        Assert.Equal(secret, retrieved);

        Keyring.DeleteSecret(service, account);
    }

    [Fact]
    public void GetSecret_ReturnsNull_WhenNotFound()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"NonExistent_{Guid.NewGuid():N}";
        var account = "nonexistent@example.com";

        var result = Keyring.GetSecret(service, account);

        Assert.Null(result);
    }

    [Fact]
    public void GetSecretAsSecureString_ReturnsSecureString()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"TestService_{Guid.NewGuid():N}";
        var account = "secure@example.com";
        var secret = "my-secure-password";

        Keyring.SetSecret(service, account, secret);
        using var secureString = Keyring.GetSecretAsSecureString(service, account);

        Assert.NotNull(secureString);
        Assert.True(secureString.Length > 0);

        Keyring.DeleteSecret(service, account);
    }

    [Fact]
    public void ListSecrets_ReturnsRecordsForService()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"ListTestService_{Guid.NewGuid():N}";
        var account1 = "user1@example.com";
        var account2 = "user2@example.com";
        var secret1 = "password1";
        var secret2 = "password2";

        Keyring.SetSecret(service, account1, secret1);
        Keyring.SetSecret(service, account2, secret2);

        var records = Keyring.ListSecrets(service);

        Assert.NotEmpty(records);
        Assert.Contains(records, r => r.Account == account1);
        Assert.Contains(records, r => r.Account == account2);

        Keyring.DeleteSecret(service, account1);
        Keyring.DeleteSecret(service, account2);
    }

    [Fact]
    public void ListSecrets_ReturnsEmptyList_WhenServiceNotFound()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"NonExistentService_{Guid.NewGuid():N}";

        var records = Keyring.ListSecrets(service);

        Assert.Empty(records);
    }

    [Fact]
    public void DeleteSecret_RemovesSecret()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        var service = $"DeleteTestService_{Guid.NewGuid():N}";
        var account = "delete@example.com";
        var secret = "to-be-deleted";

        Keyring.SetSecret(service, account, secret);
        Keyring.DeleteSecret(service, account);
        var result = Keyring.GetSecret(service, account);

        Assert.Null(result);
    }
}