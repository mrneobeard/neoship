using System.Net;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests OIDC token validation behavior.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Unit)]
[Trait(Traits.Category, Traits.Auth)]
public class SsoTokenValidatorTests
{
    /// <summary>
    /// Verifies discovery failures return <see langword="null"/>.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_DiscoveryFailureReturnsNull()
    {
        var validator = new SsoTokenValidator(new HttpClient(new StaticHandler(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var provider = new UserIdentityProvider
        {
            IssuerUrl = "https://idp.example.com",
            ClientId = "client-id",
        };

        var result = await validator.ValidateAsync(provider, "not-a-token", "nonce", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies missing issuer configuration returns <see langword="null"/>.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_MissingIssuerReturnsNull()
    {
        var validator = new SsoTokenValidator(new HttpClient(new StaticHandler(new HttpResponseMessage(HttpStatusCode.OK))));
        var provider = new UserIdentityProvider
        {
            ClientId = "client-id",
        };

        var result = await validator.ValidateAsync(provider, "not-a-token", "nonce", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    private sealed class StaticHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public StaticHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.response);
        }
    }
}