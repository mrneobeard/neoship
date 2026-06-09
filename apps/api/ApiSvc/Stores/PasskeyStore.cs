using System.Text;

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
}
