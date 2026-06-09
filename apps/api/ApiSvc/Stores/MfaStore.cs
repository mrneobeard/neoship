using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores and verifies user MFA factors.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var factor = await store.StartTotpAsync(userId, "Authenticator", ct);
/// </code>
/// </remarks>
public sealed class MfaStore
{
    private const int TotpSecretBytes = 20;
    private const int TotpDigits = 6;
    private const long TotpStepSeconds = 30;

    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    private readonly ShipDb db;
    private readonly ILogger<MfaStore> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MfaStore"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">The MFA store logger.</param>
    public MfaStore(ShipDb db, ILogger<MfaStore> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>
    /// Starts TOTP setup by creating an unverified factor.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The factor name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created <see cref="UserMfaFactor"/> and Base32 secret.</returns>
    public async Task<(UserMfaFactor Factor, string Secret)> StartTotpAsync(Guid userId, string? name, CancellationToken ct = default)
    {
        var secretBytes = RandomNumberGenerator.GetBytes(TotpSecretBytes);
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Authenticator app" : name.Trim(),
            Type = MfaFactorType.Totp.Id,
            ValueEncrypted = secretBytes,
            CreatedAt = DateTime.UtcNow,
        };

        db.UserMfaFactors.Add(factor);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("TOTP setup started: factor={FactorId} user={UserId}", factor.Id, userId);
        return (factor, ToBase32(secretBytes));
    }

    /// <summary>
    /// Confirms a TOTP factor with a current verification code.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The factor identifier.</param>
    /// <param name="code">The verification code.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when confirmed; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ConfirmTotpAsync(Guid userId, Guid factorId, string code, CancellationToken ct = default)
    {
        var factor = await db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Totp.Id,
            ct);

        if (factor is null || !VerifyTotp(factor.ValueEncrypted, code, DateTimeOffset.UtcNow))
        {
            return false;
        }

        factor.VerifiedAt = DateTime.UtcNow;
        factor.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("TOTP factor confirmed: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Disables a TOTP factor.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The factor identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when disabled; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> DisableTotpAsync(Guid userId, Guid factorId, CancellationToken ct = default)
    {
        var factor = await db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Totp.Id,
            ct);

        if (factor is null)
        {
            return false;
        }

        db.UserMfaFactors.Remove(factor);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("TOTP factor disabled: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Computes a TOTP code for a secret at a timestamp.
    /// </summary>
    /// <param name="secret">The shared secret bytes.</param>
    /// <param name="timestamp">The timestamp.</param>
    /// <returns>The six-digit TOTP code.</returns>
    public static string ComputeTotp(byte[] secret, DateTimeOffset timestamp)
    {
        var counter = timestamp.ToUnixTimeSeconds() / TotpStepSeconds;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);

        var otp = binary % 1_000_000;
        return otp.ToString($"D{TotpDigits}");
    }

    /// <summary>
    /// Converts bytes to Base32 without padding.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base32-encoded value.</returns>
    public static string ToBase32(byte[] bytes)
    {
        var output = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                output.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    private static bool VerifyTotp(byte[] secret, string code, DateTimeOffset timestamp)
    {
        var normalized = code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Length != TotpDigits || normalized.Any(c => c is < '0' or > '9'))
        {
            return false;
        }

        for (var offset = -1; offset <= 1; offset++)
        {
            var candidate = ComputeTotp(secret, timestamp.AddSeconds(offset * TotpStepSeconds));
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(candidate), Encoding.ASCII.GetBytes(normalized)))
            {
                return true;
            }
        }

        return false;
    }
}