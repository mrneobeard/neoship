using System.Runtime.InteropServices;

namespace NeoBeard.Secrets.OperatingSystem.Tests;

public class KeyringRecordTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var record = new KeyringRecord("MyService", "user@example.com", "secret123");

        Assert.Equal("MyService", record.Service);
        Assert.Equal("user@example.com", record.Account);
        Assert.Equal("secret123", record.Secret);
    }

    [Fact]
    public void Constructor_WithNullSecret_SetsNull()
    {
        var record = new KeyringRecord("Service", "Account");

        Assert.Equal("Service", record.Service);
        Assert.Equal("Account", record.Account);
        Assert.Null(record.Secret);
    }

    [Fact]
    public void Constructor_WithNullService_SetsEmptyString()
    {
        var record = new KeyringRecord(null!, "Account");

        Assert.Equal(string.Empty, record.Service);
    }

    [Fact]
    public void Constructor_WithNullAccount_SetsEmptyString()
    {
        var record = new KeyringRecord("Service", null!);

        Assert.Equal(string.Empty, record.Account);
    }
}