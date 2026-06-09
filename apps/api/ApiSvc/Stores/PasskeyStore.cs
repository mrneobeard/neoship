using System.Text;
using System.Text.Json;

using Fido2NetLib;
using Fido2NetLib.Objects;

using Microsoft.EntityFrameworkCore;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Stores passkey factors and creates WebAuthn registration options.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var options = await store.BeginRegistrationAsync(user, ct);
/// </code>
/// </remarks>
public sealed class PasskeyStore
{
    private readonly ShipDb db;
    private readonly Fido2 fido2;
    private readonly ILogger<PasskeyStore> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasskeyStore"/> class.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="fido2">The FIDO2 core service.</param>
    /// <param name="logger">The passkey store logger.</param>
    public PasskeyStore(ShipDb db, Fido2 fido2, ILogger<PasskeyStore> logger)
    {
        this.db = db;
        this.fido2 = fido2;
        this.logger = logger;
    }

    /// <summary>
    /// Lists passkeys for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user's passkey factors.</returns>
    public async Task<List<UserMfaFactor>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.UserMfaFactors
            .Where(x => x.UserId == userId && x.Type == MfaFactorType.Passkey.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Begins passkey registration for a user.
    /// </summary>
    /// <param name="user">The user registering a passkey.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The credential creation options that must be sent to the browser.</returns>
    public async Task<CredentialCreateOptions> BeginRegistrationAsync(User user, CancellationToken ct = default)
    {
        var existingCredentials = await db.UserMfaFactors
            .Where(x => x.UserId == user.Id && x.Type == MfaFactorType.Passkey.Id)
            .Select(x => new PublicKeyCredentialDescriptor(x.WebAuthnCredentialId))
            .ToListAsync(ct);

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id.ToString("N")),
                Name = user.Email,
                DisplayName = user.Name,
            },
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true,
            },
        });

        logger.LogInformation("Passkey registration started: user={UserId}", user.Id);
        return options;
    }

    /// <summary>
    /// Begins passkey login for a user email.
    /// </summary>
    /// <param name="email">The user email.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user and assertion options, or <see langword="null"/>.</returns>
    public async Task<(User User, AssertionOptions Options)?> BeginLoginAsync(string email, CancellationToken ct = default)
    {
        var emailUpcase = email.ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.EmailUpcase == emailUpcase, ct);
        if (user is null || user.StatusId == UserStatus.Suspended.Id)
        {
            return null;
        }

        var credentials = await db.UserMfaFactors
            .Where(x => x.UserId == user.Id && x.Type == MfaFactorType.Passkey.Id)
            .Select(x => new PublicKeyCredentialDescriptor(x.WebAuthnCredentialId))
            .ToListAsync(ct);

        if (credentials.Count == 0)
        {
            return null;
        }

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = credentials,
            UserVerification = UserVerificationRequirement.Preferred,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
            },
        });

        logger.LogInformation("Passkey login started: user={UserId}", user.Id);
        return (user, options);
    }

    /// <summary>
    /// Finishes passkey login and updates credential metadata.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="options">The original assertion options.</param>
    /// <param name="response">The authenticator assertion response.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated user and passkey factor, or <see langword="null"/>.</returns>
    public async Task<(User User, UserMfaFactor Factor)?> FinishLoginAsync(
        Guid userId,
        AssertionOptions options,
        AuthenticatorAssertionRawResponse response,
        CancellationToken ct = default)
    {
        var credentialDigest = ComputeCredentialIdDigest(response.RawId);
        var factor = await db.UserMfaFactors
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                    && x.Type == MfaFactorType.Passkey.Id
                    && x.WebAuthnCredentialIdDigest == credentialDigest,
                ct);

        if (factor?.User is null || factor.User.StatusId == UserStatus.Suspended.Id)
        {
            return null;
        }

        var expectedUserHandle = Encoding.UTF8.GetBytes(factor.UserId.ToString("N"));
        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = response,
            OriginalOptions = options,
            StoredPublicKey = factor.WebAuthnPublicKeyCredentialData,
            StoredSignatureCounter = factor.WebAuthnSignCount,
            IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(
                args.CredentialId.SequenceEqual(factor.WebAuthnCredentialId)
                    && args.UserHandle.SequenceEqual(expectedUserHandle)),
        }, ct);

        factor.WebAuthnSignCount = result.SignCount;
        factor.LastUsedAt = DateTime.UtcNow;
        factor.UpdatedAt = DateTime.UtcNow;
        factor.User.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Passkey login completed: factor={FactorId} user={UserId}", factor.Id, userId);
        return (factor.User, factor);
    }

    /// <summary>
    /// Finishes passkey registration and stores the verified credential.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="name">The passkey name.</param>
    /// <param name="options">The original credential creation options.</param>
    /// <param name="response">The authenticator attestation response.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The stored passkey factor.</returns>
    public async Task<UserMfaFactor> FinishRegistrationAsync(
        Guid userId,
        string? name,
        CredentialCreateOptions options,
        AuthenticatorAttestationRawResponse response,
        CancellationToken ct = default)
    {
        var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, cancellationToken) =>
            {
                var digest = ComputeCredentialIdDigest(args.CredentialId);
                return !await db.UserMfaFactors.AnyAsync(
                    x => x.Type == MfaFactorType.Passkey.Id && x.WebAuthnCredentialIdDigest == digest,
                    cancellationToken);
            },
        }, ct);

        var now = DateTime.UtcNow;
        var factor = new UserMfaFactor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Passkey" : name.Trim(),
            Type = MfaFactorType.Passkey.Id,
            WebAuthnCredentialId = result.Id,
            WebAuthnCredentialIdDigest = ComputeCredentialIdDigest(result.Id),
            WebAuthnPublicKeyCredentialData = result.PublicKey,
            WebAuthnSignCount = result.SignCount,
            TransportsJson = JsonSerializer.Serialize(result.Transports.Select(x => x.ToString()).ToArray()),
            CreatedAt = now,
            VerifiedAt = now,
        };

        db.UserMfaFactors.Add(factor);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Passkey registered: factor={FactorId} user={UserId}", factor.Id, userId);
        return factor;
    }

    /// <summary>
    /// Revokes a passkey for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="factorId">The passkey factor identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><see langword="true"/> when revoked; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RevokeAsync(Guid userId, Guid factorId, CancellationToken ct = default)
    {
        var factor = await db.UserMfaFactors.FirstOrDefaultAsync(
            x => x.Id == factorId && x.UserId == userId && x.Type == MfaFactorType.Passkey.Id,
            ct);

        if (factor is null)
        {
            return false;
        }

        db.UserMfaFactors.Remove(factor);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Passkey revoked: factor={FactorId} user={UserId}", factorId, userId);
        return true;
    }

    /// <summary>
    /// Computes the stored lookup digest for a WebAuthn credential identifier.
    /// </summary>
    /// <param name="credentialId">The WebAuthn credential identifier.</param>
    /// <returns>The Base64 digest.</returns>
    public static string ComputeCredentialIdDigest(byte[] credentialId)
    {
        return TokenStore.ComputeDigestBase64(credentialId);
    }
}