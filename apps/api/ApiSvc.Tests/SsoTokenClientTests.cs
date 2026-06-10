using System.Net;

using Microsoft.Extensions.Configuration;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests OIDC token exchange behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class SsoTokenClientTests
{
    /// <summary>
    /// Verifies OAuth2 profile fetch uses the verified primary provider email.
    /// </summary>
    [Fact]
    public async Task FetchAsync_ReturnsVerifiedOAuth2ProfileEmail()
    {
        var handler = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":123,\"login\":\"octo\",\"name\":\"Octo Cat\",\"email\":null}"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"email\":\"octo@example.com\",\"verified\":true,\"primary\":true}]"),
            });
        var client = new SsoOAuth2ProfileClient(new HttpClient(handler));
        var provider = CreateProvider();
        provider.ProviderTypeId = UserIdentityProviderType.OAUTH2.Id;
        provider.MetadataJson = "{\"user_endpoint\":\"https://api.github.com/user\",\"email_endpoint\":\"https://api.github.com/user/emails\"}";

        var profile = await client.FetchAsync(provider, "access-token", TestContext.Current.CancellationToken);

        Assert.NotNull(profile);
        Assert.Equal("123", profile!.Subject);
        Assert.Equal("octo@example.com", profile.Email);
        Assert.True(profile.EmailVerified);
        Assert.Equal("Octo Cat", profile.Name);
        Assert.Equal(2, handler.Requests.Count);
    }

    /// <summary>
    /// Verifies token exchange posts required OIDC fields and returns the ID token.
    /// </summary>
    [Fact]
    public async Task ExchangeAsync_PostsCodeAndReturnsIdToken()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id_token\":\"id-token\"}"),
        });
        var client = new SsoTokenClient(new HttpClient(handler), CreateProtector());
        var provider = CreateProvider();

        var result = await client.ExchangeAsync(provider, "auth-code", "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("id-token", result!.IdToken);
        Assert.Equal("https://idp.example.com/oauth2/token", handler.RequestUri!.ToString());
        var body = handler.Body!;
        Assert.Contains("grant_type=authorization_code", body, StringComparison.Ordinal);
        Assert.Contains("code=auth-code", body, StringComparison.Ordinal);
        Assert.Contains("client_id=client-id", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies token exchange includes encrypted client secrets when present.
    /// </summary>
    [Fact]
    public async Task ExchangeAsync_IncludesClientSecretWhenStored()
    {
        var protector = CreateProtector();
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id_token\":\"id-token\"}"),
        });
        var client = new SsoTokenClient(new HttpClient(handler), protector);
        var provider = CreateProvider();
        provider.ClientSecretEncrypted = protector.Encrypt("client-secret");

        var result = await client.ExchangeAsync(provider, "auth-code", "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Contains("client_secret=client-secret", handler.Body!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies token exchange rejects non-HTTPS token endpoints.
    /// </summary>
    [Fact]
    public async Task ExchangeAsync_RejectsNonHttpsTokenEndpoint()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var client = new SsoTokenClient(new HttpClient(handler), CreateProtector());
        var provider = CreateProvider();
        provider.MetadataJson = "{\"token_endpoint\":\"http://idp.example.com/oauth2/token\"}";

        var result = await client.ExchangeAsync(provider, "auth-code", "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Null(handler.RequestUri);
    }

    /// <summary>
    /// Verifies malformed token responses fail closed.
    /// </summary>
    [Fact]
    public async Task ExchangeAsync_MalformedTokenResponseReturnsNull()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json"),
        });
        var client = new SsoTokenClient(new HttpClient(handler), CreateProtector());

        var result = await client.ExchangeAsync(CreateProvider(), "auth-code", "https://app.example.com/api/v1/auth/sso/callback", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private static IdentityProviderSecretProtector CreateProtector()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:IdentityProviders:EncryptionKey"] = "test-identity-provider-secret-key",
            })
            .Build();
        return new IdentityProviderSecretProtector(configuration);
    }

    private static UserIdentityProvider CreateProvider()
    {
        return new UserIdentityProvider
        {
            Name = "Acme OIDC",
            ProviderTypeId = UserIdentityProviderType.OIDC.Id,
            StatusId = UserIdentityProviderStatus.Active.Id,
            ClientId = "client-id",
            MetadataJson = "{\"token_endpoint\":\"https://idp.example.com/oauth2/token\"}",
        };
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public CaptureHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        public Uri? RequestUri { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RequestUri = request.RequestUri;
            this.Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return this.response;
        }
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses;

        public SequenceHandler(params HttpResponseMessage[] responses)
        {
            this.responses = new Queue<HttpResponseMessage>(responses);
        }

        public List<Uri?> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request.RequestUri);
            return Task.FromResult(this.responses.Dequeue());
        }
    }
}
