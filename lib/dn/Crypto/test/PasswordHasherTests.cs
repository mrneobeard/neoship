namespace NeoBeard.Crypto.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void HashAndVerify_Argon2Id_ReturnsSuccess()
    {
        var options = new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
            SaltLength = 12,
            IvLength = 12,
            HashLength = 16,
            Argon2Parameters = new Argon2Parameters
            {
                Iterations = 1,
                MemorySizeKiB = 32,
                DegreeOfParallelism = 1,
                TagLength = 16,
            },
        };

        var hasher = new PasswordHasher(options);
        var hash = hasher.Hash("correct horse battery staple");

        Assert.Equal(PasswordHashResult.Success, hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void HashAndVerify_Pbkdf2_ReturnsSuccess()
    {
        var options = new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Pbkdf2,
            SaltLength = 12,
            IvLength = 12,
            HashLength = 32,
            Pbkdf2Iterations = 500,
            Pbkdf2HashAlgorithm = PasswordPbkdf2Algorithm.SHA512,
        };

        var hasher = new PasswordHasher(options);
        var hash = hasher.Hash("correct horse battery staple");

        Assert.Equal(PasswordHashResult.Success, hasher.Verify("correct horse battery staple", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFailed()
    {
        var options = new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Pbkdf2,
            Pbkdf2Iterations = 500,
        };

        var hasher = new PasswordHasher(options);
        var hash = hasher.Hash("first password");

        Assert.Equal(PasswordHashResult.Failed, hasher.Verify("wrong password", hash));
    }

    [Fact]
    public void Verify_WithOlderArgon2Policy_ReturnsNeedsRehash()
    {
        var oldHasher = new PasswordHasher(new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
            SaltLength = 12,
            IvLength = 12,
            HashLength = 16,
            Argon2Parameters = new Argon2Parameters
            {
                Iterations = 1,
                MemorySizeKiB = 32,
                DegreeOfParallelism = 1,
                TagLength = 16,
            },
        });

        var legacyHash = oldHasher.Hash("upgrade me");

        var currentHasher = new PasswordHasher(new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
            SaltLength = 12,
            IvLength = 12,
            HashLength = 16,
            Argon2Parameters = new Argon2Parameters
            {
                Iterations = 2,
                MemorySizeKiB = 32,
                DegreeOfParallelism = 1,
                TagLength = 16,
            },
        });

        Assert.Equal(PasswordHashResult.NeedsRehash, currentHasher.Verify("upgrade me", legacyHash));
    }

    [Fact]
    public void Verify_Pbkdf2HashWithArgon2Default_ReturnsNeedsRehash()
    {
        var pbkdf2Hasher = new PasswordHasher(new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Pbkdf2,
            SaltLength = 12,
            IvLength = 12,
            HashLength = 32,
            Pbkdf2Iterations = 500,
        });

        var legacyHash = pbkdf2Hasher.Hash("migrate me");
        var defaultHasher = new PasswordHasher();

        Assert.Equal(PasswordHashResult.NeedsRehash, defaultHasher.Verify("migrate me", legacyHash));
    }
}
