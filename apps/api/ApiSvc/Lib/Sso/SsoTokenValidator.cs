using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Lib.Sso;

/// <summary>
/// Validates OIDC ID tokens.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var identity = await validator.ValidateAsync(provider, idToken, nonce, ct);
/// </code>
/// </remarks>
public interface ISsoTokenValidator
{
    /// <summary>
    /// Validates an ID token.
    /// </summary>
    /// <param name="provider">The identity provider configuration.</param>
    /// <param name="idToken">The ID token.</param>
    /// <param name="nonce">The expected nonce.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The validated identity, or <see langword="null"/>.</returns>
    Task<SsoExternalIdentity?> ValidateAsync(UserIdentityProvider provider, string idToken, string nonce, CancellationToken ct = default);
}

/// <summary>
/// Default IdentityModel OIDC token validator.
/// </summary>
public sealed class SsoTokenValidator : ISsoTokenValidator
{
    private static readonly string[] ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.EcdsaSha256];

    private readonly HttpClient http;

    /// <summary>
    /// Initializes a new <see cref="SsoTokenValidator"/> instance.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    public SsoTokenValidator(HttpClient http)
    {
        this.http = http;
    }

    /// <inheritdoc/>
    public async Task<SsoExternalIdentity?> ValidateAsync(UserIdentityProvider provider, string idToken, string nonce, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(provider.IssuerUrl) || string.IsNullOrWhiteSpace(provider.ClientId))
        {
            return null;
        }

        try
        {
            var discoveryUrl = provider.IssuerUrl.TrimEnd('/') + "/.well-known/openid-configuration";
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                discoveryUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(this.http) { RequireHttps = true });
            var configuration = await configurationManager.GetConfigurationAsync(ct);

            var handler = new JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(idToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = provider.ClientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = configuration.SigningKeys,
                ValidAlgorithms = ValidAlgorithms,
            });

            if (!result.IsValid || result.ClaimsIdentity is null)
            {
                return null;
            }

            var claims = result.ClaimsIdentity.Claims.ToList();
            if (!string.Equals(FindClaim(claims, JwtRegisteredClaimNames.Nonce), nonce, StringComparison.Ordinal))
            {
                return null;
            }

            var subject = FindClaim(claims, JwtRegisteredClaimNames.Sub);
            var email = FindClaim(claims, JwtRegisteredClaimNames.Email);
            var name = FindClaim(claims, JwtRegisteredClaimNames.Name) ?? email;
            var emailVerified = string.Equals(FindClaim(claims, "email_verified"), "true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email) || !emailVerified)
            {
                return null;
            }

            return new SsoExternalIdentity(subject, email, emailVerified, name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static string? FindClaim(IEnumerable<Claim> claims, string type)
        => claims.FirstOrDefault(x => string.Equals(x.Type, type, StringComparison.Ordinal) || string.Equals(x.Type, ClaimTypesMap(type), StringComparison.Ordinal))?.Value;

    private static string ClaimTypesMap(string type) => type switch
    {
        JwtRegisteredClaimNames.Email => ClaimTypes.Email,
        JwtRegisteredClaimNames.Name => ClaimTypes.Name,
        _ => type,
    };
}

/// <summary>
/// Represents a validated external SSO identity.
/// </summary>
/// <param name="Subject">The provider subject.</param>
/// <param name="Email">The verified email address.</param>
/// <param name="EmailVerified">A value indicating whether the provider verified the email.</param>
/// <param name="Name">The display name.</param>
public sealed record SsoExternalIdentity(string Subject, string Email, bool EmailVerified, string? Name);
