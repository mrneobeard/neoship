namespace NeoBeard.Ssh.Tests;

public static class SshConfigDocumentTests
{
    [Fact]
    public static void Lookup_ResolvesHostOptionsIdentityFilesAndEnv()
    {
        var doc = SshConfigDocument.Parse(
            """
            Host *
              User default
              Port 22
              SendEnv LANG LC_*
              SetEnv APP_ENV=global
            Host prod
              HostName prod.example.com
              User deploy
              IdentityFile ~/.ssh/prod
              IdentityFile ~/.ssh/prod2
              SendEnv APP_*
              SetEnv APP_ENV=prod API=https://prod.example.com
            """);

        var resolved = doc.Lookup("prod");

        Assert.Equal("prod.example.com", resolved.Hostname);
        Assert.Equal("default", resolved.User);
        Assert.Equal(22, resolved.Port);
        Assert.Equal(2, resolved.IdentityFile.Count);
        Assert.Equal(["LANG", "LC_*", "APP_*"], resolved.SendEnv);
        Assert.Equal("prod", resolved.SetEnv["APP_ENV"]);
        Assert.Equal("https://prod.example.com", resolved.SetEnv["API"]);
    }
}