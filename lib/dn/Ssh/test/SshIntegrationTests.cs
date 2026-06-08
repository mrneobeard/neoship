using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace NeoBeard.Ssh.Tests;

public static class SshIntegrationTests
{
    private const string Password = "password";

    [Fact]
    public static async Task PasswordAuth_RunUploadDownloadAndList_Succeeds()
    {
        Assert.SkipWhen(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Testcontainers SSH tests require Linux.");
        Assert.SkipWhen(!CommandExists("docker"), "Docker is required for Testcontainers SSH tests.");

        await using var fixture = await OpenSshFixture.StartAsync();
        SshClient client;
        try
        {
            client = await SshClient.ConnectAsync("127.0.0.1", fixture.Port, "test", Password);
        }
        catch
        {
            var logs = await fixture.Container.GetLogsAsync(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, true, TestContext.Current.CancellationToken);
            TestContext.Current.SendDiagnosticMessage(logs.Stdout + logs.Stderr);
            var sshdLog = await fixture.Container.ExecAsync(["sh", "-lc", "cat /tmp/sshd.log 2>/dev/null || true"], TestContext.Current.CancellationToken);
            TestContext.Current.SendDiagnosticMessage(sshdLog.Stdout + sshdLog.Stderr);
            throw;
        }

        using (client)
        {
            await client.RunAsync(Encoding.UTF8.GetBytes("mkdir -p /config/dotnet-ssh").AsSpan());
            var combined = await client.CombinedOutputAsync("printf o; printf e 1>&2".AsSpan());
            await client.ScpUploadAsync("/config/dotnet-ssh/password.txt", Encoding.UTF8.GetBytes("password-data"));

            var downloaded = await client.ScpDownloadAsync("/config/dotnet-ssh/password.txt");
            var entries = await client.ListFilesAsync("/config/dotnet-ssh");

            Assert.Equal("oe", combined);
            Assert.Equal("password-data", Encoding.UTF8.GetString(downloaded));
            Assert.Contains("password.txt", entries);
        }
    }

    [Fact]
    public static async Task PasswordAuth_RunUploadDownloadAndList_Succeeds_Sync()
    {
        Assert.SkipWhen(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Testcontainers SSH tests require Linux.");
        Assert.SkipWhen(!CommandExists("docker"), "Docker is required for Testcontainers SSH tests.");

        await using var fixture = await OpenSshFixture.StartAsync();
        SshClient client;
        try
        {
            client = SshClient.Connect("127.0.0.1", fixture.Port, "test", Password);
        }
        catch
        {
            var logs = await fixture.Container.GetLogsAsync(DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, true, TestContext.Current.CancellationToken);
            TestContext.Current.SendDiagnosticMessage(logs.Stdout + logs.Stderr);
            var sshdLog = await fixture.Container.ExecAsync(["sh", "-lc", "cat /tmp/sshd.log 2>/dev/null || true"], TestContext.Current.CancellationToken);
            TestContext.Current.SendDiagnosticMessage(sshdLog.Stdout + sshdLog.Stderr);
            throw;
        }

        using (client)
        {
            client.Run("mkdir -p /config/dotnet-ssh".AsSpan());
            var combined = client.CombinedOutput("printf o; printf e 1>&2".AsSpan());
            client.ScpUpload("/config/dotnet-ssh/password.txt", Encoding.UTF8.GetBytes("password-data"));

            var downloaded = client.ScpDownload("/config/dotnet-ssh/password.txt");
            var entries = client.ListFiles("/config/dotnet-ssh");
            var output = client.Output(Encoding.UTF8.GetBytes("printf sync-ok").AsSpan());
            var session = client.NewSession();
            _ = session.Setenv("SYNC_TOKEN", Encoding.UTF8.GetBytes("secret-value").AsSpan());
            session.Start("printf session-ok".AsSpan());
            var sessionResult = session.Wait();

            Assert.Equal("password-data", Encoding.UTF8.GetString(downloaded));
            Assert.Contains("password.txt", entries);
            Assert.Equal("sync-ok", output);
            Assert.Equal("oe", combined);
            Assert.Equal("session-ok", sessionResult.Stdout);
        }
    }

    [Fact]
    public static async Task RsaIdentities_LegacyPkcs1Pkcs8EncryptedPemAndEncryptedOpenSsh_Authenticate()
    {
        Assert.SkipWhen(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Testcontainers SSH tests require Linux.");
        Assert.SkipWhen(!CommandExists("docker"), "Docker is required for Testcontainers SSH tests.");

        await using var fixture = await OpenSshFixture.StartAsync();
        foreach (var key in fixture.RsaIdentityTexts)
        {
            using var client = await SshClient.ConnectAsync(
                "127.0.0.1",
                fixture.Port,
                "test",
                config: new SshClientConfig(privateKey: key.Text, privateKeyPassphrase: key.Passphrase));

            var output = await client.OutputAsync("printf rsa-ok".AsSpan());
            Assert.Equal("rsa-ok", output);
        }
    }

    [Fact]
    public static async Task Ed25519Identities_OpenSshFormat_Authenticate()
    {
        Assert.SkipWhen(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Testcontainers SSH tests require Linux.");
        Assert.SkipWhen(!CommandExists("docker"), "Docker is required for Testcontainers SSH tests.");

        await using var fixture = await OpenSshFixture.StartAsync();
        foreach (var key in fixture.Ed25519IdentityTexts)
        {
            using var client = await SshClient.ConnectAsync(
                "127.0.0.1",
                fixture.Port,
                "test",
                config: new SshClientConfig(privateKey: key.Text, privateKeyPassphrase: key.Passphrase));

            var output = await client.OutputAsync("printf ed25519-ok".AsSpan());
            Assert.Equal("ed25519-ok", output);
        }
    }

    private static bool CommandExists(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator).Any(dir => File.Exists(Path.Combine(dir, name)));
    }

    private sealed class OpenSshFixture : IAsyncDisposable
    {
        private readonly IContainer container;
        private readonly string directory;

        private OpenSshFixture(IContainer container, string directory, int port, IReadOnlyList<(string Text, string? Passphrase)> rsaIdentityTexts, IReadOnlyList<(string Text, string? Passphrase)> ed25519IdentityTexts)
        {
            this.container = container;
            this.directory = directory;
            this.Port = port;
            this.RsaIdentityTexts = rsaIdentityTexts;
            this.Ed25519IdentityTexts = ed25519IdentityTexts;
        }

        public int Port { get; }

        public IContainer Container => this.container;

        public IReadOnlyList<(string Text, string? Passphrase)> RsaIdentityTexts { get; }

        public IReadOnlyList<(string Text, string? Passphrase)> Ed25519IdentityTexts { get; }

        public static async Task<OpenSshFixture> StartAsync()
        {
            var dir = Path.Combine(Path.GetTempPath(), "neobeard-ssh-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var rsaPkcs1 = Path.Combine(dir, "id_rsa_pkcs1");
            var rsaOpenSsh = Path.Combine(dir, "id_rsa_openssh");
            var rsaEncrypted = Path.Combine(dir, "id_rsa_encrypted");
            var rsaOpenSshEncrypted = Path.Combine(dir, "id_rsa_openssh_encrypted");
            var ed25519 = Path.Combine(dir, "id_ed25519");
            var ed25519Encrypted = Path.Combine(dir, "id_ed25519_encrypted");

            Run("ssh-keygen", $"-q -t rsa -b 2048 -m PEM -N \"\" -f \"{rsaPkcs1}\"");
            Run("ssh-keygen", $"-q -t rsa -b 2048 -N \"\" -f \"{rsaOpenSsh}\"");
            Run("ssh-keygen", $"-q -t rsa -b 2048 -m PEM -N \"secret\" -f \"{rsaEncrypted}\"");
            Run("ssh-keygen", $"-q -t rsa -b 2048 -N \"secret\" -f \"{rsaOpenSshEncrypted}\"");
            Run("ssh-keygen", $"-q -t ed25519 -N \"\" -f \"{ed25519}\"");
            Run("ssh-keygen", $"-q -t ed25519 -N \"secret\" -f \"{ed25519Encrypted}\"");

            var rsaPkcs1Public = (await File.ReadAllTextAsync(rsaPkcs1 + ".pub")).Trim();
            var rsaOpenSshPublic = (await File.ReadAllTextAsync(rsaOpenSsh + ".pub")).Trim();
            var rsaEncryptedPublic = (await File.ReadAllTextAsync(rsaEncrypted + ".pub")).Trim();
            var rsaOpenSshEncryptedPublic = (await File.ReadAllTextAsync(rsaOpenSshEncrypted + ".pub")).Trim();
            var ed25519Public = (await File.ReadAllTextAsync(ed25519 + ".pub")).Trim();
            var ed25519EncryptedPublic = (await File.ReadAllTextAsync(ed25519Encrypted + ".pub")).Trim();
            var authorized = string.Join('\n', rsaPkcs1Public, rsaOpenSshPublic, rsaEncryptedPublic, rsaOpenSshEncryptedPublic, ed25519Public, ed25519EncryptedPublic);
            var publicKey = await File.ReadAllTextAsync(rsaPkcs1 + ".pub");

            var container = new ContainerBuilder("lscr.io/linuxserver/openssh-server:latest")
                .WithPortBinding(2222, true)
                .WithEnvironment("PUID", "1000")
                .WithEnvironment("PGID", "1000")
                .WithEnvironment("USER_NAME", "test")
                .WithEnvironment("USER_PASSWORD", Password)
                .WithEnvironment("PASSWORD_ACCESS", "true")
                .WithEnvironment("PUBLIC_KEY", publicKey)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(2222))
                .Build();

            await container.StartAsync();
            await container.ExecAsync([
                "sh",
                "-lc",
                $"printf '%s\n' 'LogLevel DEBUG3' 'KexAlgorithms diffie-hellman-group14-sha256' 'HostKeyAlgorithms ssh-rsa,rsa-sha2-256,rsa-sha2-512' >> /etc/ssh/sshd_config && mkdir -p /config/.ssh /home/test/.ssh && printf '%s\n' '{authorized.Replace("'", "'\\''", StringComparison.Ordinal)}' | tee /config/.ssh/authorized_keys /home/test/.ssh/authorized_keys >/dev/null && (chown -R test:test /config/.ssh /home/test/.ssh 2>/dev/null || chown -R 1000:1000 /config/.ssh /home/test/.ssh) && chmod 700 /config/.ssh /home/test/.ssh && chmod 600 /config/.ssh/authorized_keys /home/test/.ssh/authorized_keys && pkill sshd; /usr/sbin/sshd -E /tmp/sshd.log",
            ]);
            await WaitForSshAsync(container, TestContext.Current.CancellationToken);

            var rsaIdentities = new List<(string Text, string? Passphrase)>
            {
                (await File.ReadAllTextAsync(rsaPkcs1), null),
                (await File.ReadAllTextAsync(rsaOpenSsh), null),
                (await File.ReadAllTextAsync(rsaEncrypted), "secret"),
                (await File.ReadAllTextAsync(rsaOpenSshEncrypted), "secret"),
                (ExportPkcs8(await File.ReadAllTextAsync(rsaPkcs1)), null),
            };

            var ed25519Identities = new List<(string Text, string? Passphrase)>
            {
                (await File.ReadAllTextAsync(ed25519), null),
                (await File.ReadAllTextAsync(ed25519Encrypted), "secret"),
            };

            return new OpenSshFixture(
                container,
                dir,
                container.GetMappedPublicPort(2222),
                rsaIdentities,
                ed25519Identities);
        }

        public async ValueTask DisposeAsync()
        {
            await this.container.DisposeAsync();
            Directory.Delete(this.directory, recursive: true);
        }

        private static string ExportPkcs8(string pkcs1Pem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pkcs1Pem);
            return rsa.ExportPkcs8PrivateKeyPem();
        }

        private static void Run(string fileName, string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
            process?.WaitForExit();
            if (process is null || process.ExitCode != 0)
                throw new InvalidOperationException($"{fileName} failed: {process?.StandardError.ReadToEnd()}");
        }

        private static async Task WaitForSshAsync(IContainer container, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var result = await container.ExecAsync(["sh", "-lc", "nc -z 127.0.0.1 2222"], cancellationToken);
                if (result.ExitCode == 0)
                    return;

                await Task.Delay(100, cancellationToken);
            }

            throw new InvalidOperationException("OpenSSH test container did not restart sshd in time.");
        }
    }
}
