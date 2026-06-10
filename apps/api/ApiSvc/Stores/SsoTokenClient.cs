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

        try
        {
            using var response = await this.http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var idToken = doc.RootElement.TryGetProperty("id_token", out var idTokenElement) ? idTokenElement.GetString() : null;
            var accessToken = doc.RootElement.TryGetProperty("access_token", out var accessTokenElement) ? accessTokenElement.GetString() : null;
            return string.IsNullOrWhiteSpace(idToken) && string.IsNullOrWhiteSpace(accessToken) ? null : new SsoTokenResponse(idToken, accessToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
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
/// <param name="AccessToken">The access token.</param>
public sealed record SsoTokenResponse(string? IdToken, string? AccessToken)
{
    /// <summary>
    /// Initializes a new OIDC-only <see cref="SsoTokenResponse"/> instance.
    /// </summary>
    /// <param name="idToken">The ID token.</param>
    public SsoTokenResponse(string idToken)
        : this(idToken, null)
    {
    }
}

/// <summary>
/// Fetches OAuth2 user profile details after token exchange.
/// </summary>
/// <example>
/// <code>
/// var profile = await client.FetchAsync(provider, accessToken, ct);
/// </code>
/// </example>
public interface ISsoOAuth2ProfileClient
{
    /// <summary>
    /// Fetches an OAuth2 profile from provider metadata endpoints.
    /// </summary>
    /// <param name="provider">The identity provider configuration.</param>
    /// <param name="accessToken">The OAuth2 access token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The OAuth2 profile, or <see langword="null"/>.</returns>
    Task<SsoOAuth2Profile?> FetchAsync(UserIdentityProvider provider, string accessToken, CancellationToken ct = default);
}

/// <summary>
/// Default HTTP OAuth2 profile client for common providers.
/// </summary>
public sealed class SsoOAuth2ProfileClient : ISsoOAuth2ProfileClient
{
    private readonly HttpClient http;

    /// <summary>
    /// Initializes a new <see cref="SsoOAuth2ProfileClient"/> instance.
    /// </summary>
    /// <param name="http">The HTTP client.</param>
    public SsoOAuth2ProfileClient(HttpClient http)
    {
        this.http = http;
    }

    /// <inheritdoc/>
    public async Task<SsoOAuth2Profile?> FetchAsync(UserIdentityProvider provider, string accessToken, CancellationToken ct = default)
    {
        var userEndpoint = TryReadEndpoint(provider.MetadataJson, "user_endpoint");
        if (userEndpoint is null || string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, userEndpoint);
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("NeoShip-IAM");

        try
        {
            using var userResponse = await this.http.SendAsync(userRequest, ct);
            if (!userResponse.IsSuccessStatusCode)
            {
                return null;
            }

            await using var userStream = await userResponse.Content.ReadAsStreamAsync(ct);
            using var userDoc = await JsonDocument.ParseAsync(userStream, cancellationToken: ct);
            var subject = ReadString(userDoc.RootElement, "id") ?? ReadString(userDoc.RootElement, "login");
            var name = ReadString(userDoc.RootElement, "name") ?? ReadString(userDoc.RootElement, "login");
            var email = ReadString(userDoc.RootElement, "email");
            var verified = !string.IsNullOrWhiteSpace(email);

            if (string.IsNullOrWhiteSpace(email) && TryReadEndpoint(provider.MetadataJson, "email_endpoint") is { } emailEndpoint)
            {
                (email, verified) = await this.FetchVerifiedEmailAsync(emailEndpoint, accessToken, ct);
            }

            return string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email)
                ? null
                : new SsoOAuth2Profile(subject, email, verified, name);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<(string? Email, bool Verified)> FetchVerifiedEmailAsync(Uri endpoint, string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("NeoShip-IAM");
        using var response = await this.http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return (null, false);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return (null, false);
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var email = ReadString(item, "email");
            var verified = item.TryGetProperty("verified", out var verifiedElement) && verifiedElement.ValueKind == JsonValueKind.True;
            var primary = item.TryGetProperty("primary", out var primaryElement) && primaryElement.ValueKind == JsonValueKind.True;
            if (!string.IsNullOrWhiteSpace(email) && verified && primary)
            {
                return (email, true);
            }
        }

        return (null, false);
    }

    private static Uri? TryReadEndpoint(string? metadataJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (!doc.RootElement.TryGetProperty(propertyName, out var element))
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

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null,
        };
    }
}

/// <summary>
/// Represents a verified OAuth2 profile used by SSO.
/// </summary>
/// <param name="Subject">The provider subject identifier.</param>
/// <param name="Email">The verified email address.</param>
/// <param name="EmailVerified">Whether the email is verified.</param>
/// <param name="Name">The optional display name.</param>
public sealed record SsoOAuth2Profile(string Subject, string Email, bool EmailVerified, string? Name);
