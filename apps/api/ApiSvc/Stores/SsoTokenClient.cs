using System.Text.Json;

using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Stores;

/// <summary>
/// Exchanges OIDC authorization codes for tokens.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// var response = await client.ExchangeAsync(provider, "code", redirectUri, ct);
/// </code>
/// </remarks>
public interface ISsoTokenClient
{
    /// <summary>
    /// Exchanges an authorization code for provider tokens.
    /// </summary>
    /// <param name="provider">The identity provider configuration.</param>
    /// <param name="code">The authorization code.</param>
    /// <param name="redirectUri">The callback redirect URI.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The token response, or <see langword="null"/>.</returns>
    Task<SsoTokenResponse?> ExchangeAsync(UserIdentityProvider provider, string code, string redirectUri, CancellationToken ct = default);
}

/// <summary>
/// Default HTTP OIDC token client.
/// </summary>
public sealed class SsoTokenClient : ISsoTokenClient
{
    private readonly HttpClient http;
    private readonly IdentityProviderSecretProtector secrets;

    /// <summary>
    /// Initializes a new <see cref="SsoTokenClient"/> instance.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    /// <param name="secrets">The identity-provider secret protector.</param>
    public SsoTokenClient(HttpClient http, IdentityProviderSecretProtector secrets)
    {
        this.http = http;
        this.secrets = secrets;
    }

    /// <inheritdoc/>
    public async Task<SsoTokenResponse?> ExchangeAsync(UserIdentityProvider provider, string code, string redirectUri, CancellationToken ct = default)
    {
        var tokenEndpoint = TryReadTokenEndpoint(provider.MetadataJson);
        if (tokenEndpoint is null || string.IsNullOrWhiteSpace(provider.ClientId))
        {
            return null;
        }

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri),
            new("client_id", provider.ClientId),
        };

        var clientSecret = this.secrets.Decrypt(provider.ClientSecretEncrypted);
        if (!string.IsNullOrEmpty(clientSecret))
        {
            form.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
        }

        using var response = await this.http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("id_token", out var idTokenElement))
        {
            return null;
        }

        var idToken = idTokenElement.GetString();
        return string.IsNullOrWhiteSpace(idToken) ? null : new SsoTokenResponse(idToken);
    }

    private static Uri? TryReadTokenEndpoint(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty("token_endpoint", out var element))
            {
                return null;
            }

            var value = element.GetString();
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Represents an OIDC token response used by SSO.
/// </summary>
/// <param name="IdToken">The ID token.</param>
public sealed record SsoTokenResponse(string IdToken);