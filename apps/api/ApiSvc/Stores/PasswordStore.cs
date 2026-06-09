using System.Security.Cryptography;

using NeoBeard.Crypto;

namespace NeoShip.ApiSvc.Stores;

public class PasswordStore
{
    private readonly PasswordHasher hasher;

    public PasswordStore()
    {
        this.hasher = new PasswordHasher(new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
            Argon2Parameters = Argon2Parameters.SecondRecommended,
        });
    }

    public string Hash(string password)
    {
        return this.hasher.Hash(password);
    }

    public (bool Success, bool NeedsRehash) Verify(string password, string hash)
    {
        var result = this.hasher.Verify(password, hash);
        return (result != PasswordHashResult.Failed, result == PasswordHashResult.NeedsRehash);
    }
}