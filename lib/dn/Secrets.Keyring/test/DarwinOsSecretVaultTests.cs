using System.Runtime.InteropServices;

using NeoBeard.Secrets.Darwin;

namespace NeoBeard.Secrets.OperatingSystem.Tests;

public class DarwinOsSecretVaultTests
{
    [Fact]
    public void ListSecrets_Returns_Matching_Darwin_Keychain_Items()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        var vault = new DarwinOsSecretVault();
        var service = $"NeoBeardDarwinList_{Guid.NewGuid():N}";
        var account1 = "user1@example.com";
        var account2 = "user2@example.com";

        try
        {
            vault.SetSecret(service, account1, "secret1");
            vault.SetSecret(service, account2, "secret2");

            var records = vault.ListSecrets(service);

            Assert.Contains(records, static record => record.Account == "user1@example.com");
            Assert.Contains(records, static record => record.Account == "user2@example.com");
        }
        finally
        {
            vault.DeleteSecret(service, account1);
            vault.DeleteSecret(service, account2);
        }
    }

    [Fact]
    public void ListSecrets_Returns_Empty_List_For_Missing_Darwin_Service()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        var vault = new DarwinOsSecretVault();
        var service = $"NeoBeardDarwinMissing_{Guid.NewGuid():N}";

        var records = vault.ListSecrets(service);

        Assert.Empty(records);
    }
}