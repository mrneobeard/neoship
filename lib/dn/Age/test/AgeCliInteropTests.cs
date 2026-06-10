namespace NeoBeard.Age.Tests;

public sealed class AgeCliInteropTests
{
    // Generated with `mise exec age -- age-keygen -o` in this environment on 2026-06-07.
    private const string CliPublicKey = "age1kzh33t59ftcywhsxyk4kkyrd8c5x2ulqpw3w5wv5plstajf4g5aqt3ma9r";
    private const string CliSecretKey = "AGE-SECRET-KEY-1M0J706Z6QXT0A4ALWZ098Q5AFLTWXCVXLFT8VFYAG24MXX56CUSQQLCA3H";

    // Generated with `mise exec age -- age -r <public key> -o`.
    private const string CliEncryptedPayload1Base64 =
        "YWdlLWVuY3J5cHRpb24ub3JnL3YxCi0+IFgyNTUxOSB4QVZQeDY5YnVaNERmYnVNSDJ3bE9jMG4rWGlyK0RXbEFHakVGYlJsbW53ClhRaUJzS0phTDJLMWFMallaam10N2RNd2x6cTE2Mk9ISGtET1JmbFYra1EKLS0tIFJhTkJpU0xyQ25xRGNhQW96R1YrSlhCanlYWG0vVmNNdVpGNnJPajQwUnMKWdqnjYaTQdZHJ4vU5m48rxY9m/feGXQbvDhfnEono6YNIU7YhHRZdIenn15m+nzufZTvLcPSVdRhYj1SGQosrskc";

    // Generated with `mise exec age -- age -r <public key> -o`.
    private const string CliEncryptedPayload2Base64 =
        "YWdlLWVuY3J5cHRpb24ub3JnL3YxCi0+IFgyNTUxOSByTU9DWVN5SFZiNE5jWFhBVzMra0JGYzZlK29lMzM5ODREcTVaN3VjdzNNClNpbTNKYm02bHpFY2lXeFVUd0t5MnpjTml1Ym5aR2cvU2dlT1ppM1llY2MKLS0tIGVSN3B4MUt3L00zZzMrWjdNZlJqc0d0bm5EMDlBVHNNenVYaGxWMXE3blEKIuhmz81/1hNsZOAi63+EhgKhrNODrRz96pphv5w3uPb8ljqcEU599CfUPhCBlRWmRQ6mXMMgnpHXjoT5Y9TLnQOKl+7cbE5MV1vUzOX/8oWqz1NWtOyZUICPe3g=";

    // Generated with `mise exec age -- age -r <public key> -o`.
    private const string CliEncryptedPayload3Base64 =
        "YWdlLWVuY3J5cHRpb24ub3JnL3YxCi0+IFgyNTUxOSBSTzdUM2VYYTZjbDdnQkFvbmRZR0pIYkRwSkJMc3J5RW5iUDg0RGU1aFhRCnZCSXdnVlFaUnkvNE5sZHVCQmlXenRUcDBPb0xwbTJrTmhQM2xtMjlJUlEKLS0tIHZRQTdzN1llRElIZDI1bHVIQlpiemd6K1JzTWk3aXU4cGZnUjBCaXphenMKtxIeSLnUtuTxpXxfNRguEaHZ2yxpsfubTP2cLuXdEuOlAfDLKbMkletOZH4wspvIYZY+t91v+lXQ215pDH7n+bl4ixMB5W90";

    [Fact]
    public void Cli_Generated_Key_And_First_Message_Are_Compatible_With_Library()
    {
        VerifyCliEncryptedPayload(
            CliEncryptedPayload1Base64,
            "value from age cli generated keys\n"u8.ToArray());
    }

    [Fact]
    public void Cli_Generated_Second_Message_Are_Compatible_With_Library()
    {
        VerifyCliEncryptedPayload(
            CliEncryptedPayload2Base64,
            "cli interoperability payload #2\nwith newline\nand third line\n"u8.ToArray());
    }

    [Fact]
    public void Cli_Generated_Binary_Message_Are_Compatible_With_Library()
    {
        VerifyCliEncryptedPayload(
            CliEncryptedPayload3Base64,
            "binary\0\x01\x02 with emoji ✅ and spaces end\n"u8.ToArray());
    }

    private static void VerifyCliEncryptedPayload(string encryptedPayloadBase64, byte[] expectedPlaintext)
    {
        var directory = CreateTempDirectory();
        try
        {
            var encryptedPath = Path.Combine(directory, "input.age");
            var decryptedPath = Path.Combine(directory, "decrypted.txt");
            File.WriteAllBytes(encryptedPath, Convert.FromBase64String(encryptedPayloadBase64));

            AgeFile.DecryptFile(encryptedPath, decryptedPath, [AgeIdentity.FromPrivateKey(CliSecretKey)]);
            var decrypted = File.ReadAllBytes(decryptedPath);

            Assert.Equal(expectedPlaintext, decrypted);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTempDirectory()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "neobeard-age-cli-fs-" + Guid.NewGuid().ToString("N"))).FullName;
    }
}