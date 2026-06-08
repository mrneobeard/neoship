using System.Security.Cryptography;
using NeoBeard.Crypto;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Services;

public class PasswordService
{
    private readonly PasswordHasher _hasher;

    public PasswordService()
    {
        _hasher = new PasswordHasher(new PasswordHasherOptions
        {
            PreferredAlgorithm = PasswordHashAlgorithm.Argon2Id,
            Argon2Parameters = Argon2Parameters.SecondRecommended,
        });
    }

    public string Hash(string password)
    {
        return _hasher.Hash(password);
    }

    public (bool Success, bool NeedsRehash) Verify(string password, string hash)
    {
        var result = _hasher.Verify(password, hash);
        return (result != PasswordHashResult.Failed, result == PasswordHashResult.NeedsRehash);
    }
}
