using Fido2NetLib;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class AuthEndpoints
{
    public const string SessionCookieName = "neoship_sid";

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth");

        group.MapPost("/signup", SignupAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/sso/{orgSlug}/begin", BeginSsoAsync);
        group.MapGet("/sso/callback", FinishSsoAsync);
        group.MapPost("/passkeys/begin-login", BeginPasskeyLoginAsync);
        group.MapPost("/passkeys/finish-login", FinishPasskeyLoginAsync);
        group.MapPost("/api-keys/login", LoginWithApiKeyAsync);
        group.MapPost("/logout", LogoutAsync);

        group.MapPost("/password-reset/request", RequestPasswordResetAsync);
        group.MapPost("/password-reset/confirm", ConfirmPasswordResetAsync);

        group.MapPost("/email-verification/request", RequestEmailVerificationAsync);
        group.MapPost("/email-verification/confirm", ConfirmEmailVerificationAsync);

        group.MapPost("/token-exchange", TokenExchangeAsync);

        return group;
    }

    private static void SetSessionCookie(HttpResponse response, string rawToken, DateTime expiresAt)
    {
        response.Cookies.Append(SessionCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/",
        });
    }

    private static void ClearSessionCookie(HttpResponse response)
    {
        response.Cookies.Delete(SessionCookieName);
    }

    private static string? ReadSessionToken(HttpRequest request)
    {
        return request.Cookies[SessionCookieName];
    }

    public record SignupRequest(string Email, string Name, string Password);

    public record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl);

    private static async Task<Results<Created<UserResponse>, Conflict<string>>> SignupAsync(
        [FromBody] SignupRequest req,
        AuthStore auth,
        CancellationToken ct)
    {
        var (result, user, _, _) = await auth.SignupAsync(
            req.Email, req.Name, req.Password, Constants.DefaultOrganizationId, ct);

        if (result == SignupResult.EmailAlreadyExists || user is null)
            return TypedResults.Conflict("Email already registered.");

        return TypedResults.Created($"/api/v1/me", new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    public record LoginRequest(string Email, string Password);

    public record BeginSsoResponse(long ProviderId, string AuthorizationUrl, string State, DateTime ExpiresAt);

    public record BeginPasskeyLoginRequest(string Email);

    public record BeginPasskeyLoginResponse(Guid ChallengeId, string OptionsJson);

    public record FinishPasskeyLoginRequest(Guid ChallengeId, AuthenticatorAssertionRawResponse Response);

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult, StatusCodeHttpResult>> LoginAsync(
        [FromBody] LoginRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var (result, user, session, rawToken) = await auth.LoginAsync(
            req.Email, req.Password, Constants.DefaultOrganizationId, ct);

        if (result == LoginResult.AccountLocked)
            return TypedResults.StatusCode(423);

        if (result != LoginResult.Success || user is null || session is null || rawToken is null)
            return TypedResults.Unauthorized();

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    private static async Task<Results<Ok<BeginPasskeyLoginResponse>, UnauthorizedHttpResult>> BeginPasskeyLoginAsync(
        [FromBody] BeginPasskeyLoginRequest req,
        PasskeyStore passkeys,
        PasskeyChallengeStore challenges,
        CancellationToken ct)
    {
        var result = await passkeys.BeginLoginAsync(req.Email, ct);
        if (result is null)
        {
            return TypedResults.Unauthorized();
        }

        var (user, options) = result.Value;
        var challengeId = challenges.StoreLogin(user.Id, options);
        return TypedResults.Ok(new BeginPasskeyLoginResponse(challengeId, options.ToJson()));
    }

    private static async Task<Results<Ok<BeginSsoResponse>, NotFound>> BeginSsoAsync(
        string orgSlug,
        [FromQuery] long? providerId,
        HttpContext httpContext,
        SsoStore sso,
        CancellationToken ct)
    {
        var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/v1/auth/sso/callback";
        var result = await sso.BeginOidcAsync(orgSlug, providerId, redirectUri, ct);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new BeginSsoResponse(result.ProviderId, result.AuthorizationUrl, result.State, result.ExpiresAt));
    }

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult>> FinishSsoAsync(
        [FromQuery] string state,
        [FromQuery] string code,
        HttpContext httpContext,
        SsoStore sso,
        PermissionResolver permissions,
        SessionStore sessions,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await sso.FinishOidcAsync(state, code, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var permissionSet = await permissions.ResolveUserAsync(user.Id, ct);
        var (session, rawToken) = await sessions.CreateSessionAsync(user.Id, user.OrgId, permissionSet, ct);

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);
        await audit.RecordAsync("auth.sso.login", user.OrgId, user.Id, "sso.login", targetType: "user", targetId: user.Id.ToString(), ct: ct);

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult, StatusCodeHttpResult>> FinishPasskeyLoginAsync(
        [FromBody] FinishPasskeyLoginRequest req,
        HttpContext httpContext,
        PasskeyStore passkeys,
        PasskeyChallengeStore challenges,
        PermissionResolver permissions,
        SessionStore sessions,
        CancellationToken ct)
    {
        var challenge = challenges.TakeLogin(req.ChallengeId);
        if (challenge is null)
        {
            return TypedResults.Unauthorized();
        }

        var (userId, options) = challenge.Value;
        var result = await passkeys.FinishLoginAsync(userId, options, req.Response, ct);
        if (result is null)
        {
            return TypedResults.Unauthorized();
        }

        var (user, _) = result.Value;
        var permissionSet = await permissions.ResolveUserAsync(user.Id, ct);
        var (session, rawToken) = await sessions.CreateSessionAsync(user.Id, user.OrgId, permissionSet, ct);

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    /// <summary>
    /// Represents an API key login request.
    /// </summary>
    /// <param name="ApiKey">The plaintext API key.</param>
    public record ApiKeyLoginRequest(string ApiKey);

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult, StatusCodeHttpResult>> LoginWithApiKeyAsync(
        [FromBody] ApiKeyLoginRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var (result, user, session, rawToken) = await auth.LoginWithUserApiKeyAsync(req.ApiKey, ct);

        if (result == LoginResult.AccountLocked)
            return TypedResults.StatusCode(423);

        if (result != LoginResult.Success || user is null || session is null || rawToken is null)
            return TypedResults.Unauthorized();

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    private static async Task<Ok> LogoutAsync(
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var rawToken = ReadSessionToken(httpContext.Request);

        if (rawToken is not null)
        {
            var tokenBytes = Convert.FromBase64String(rawToken);
            var digest = TokenStore.ComputeDigest(tokenBytes);
            var digestBase64 = Convert.ToBase64String(digest);
            await auth.LogoutAsync(digestBase64, ct);
        }

        ClearSessionCookie(httpContext.Response);
        return TypedResults.Ok();
    }

    public record PasswordResetRequest(string Email);

    private static async Task<Ok> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequest req,
        AuthStore auth,
        CancellationToken ct)
    {
        await auth.RequestPasswordResetAsync(req.Email, ct);
        return TypedResults.Ok();
    }

    public record PasswordResetConfirm(string Token, string NewPassword);

    private static async Task<Results<Ok, UnauthorizedHttpResult>> ConfirmPasswordResetAsync(
        [FromBody] PasswordResetConfirm req,
        AuthStore auth,
        CancellationToken ct)
    {
        var success = await auth.ConfirmPasswordResetAsync(req.Token, req.NewPassword, ct);
        return success ? TypedResults.Ok() : TypedResults.Unauthorized();
    }

    public record EmailVerificationRequest(string Email);

    private static async Task<Ok> RequestEmailVerificationAsync(
        [FromBody] EmailVerificationRequest req,
        AuthStore auth,
        CancellationToken ct)
    {
        await auth.RequestEmailVerificationAsync(req.Email, ct);
        return TypedResults.Ok();
    }

    public record EmailVerificationConfirm(string Token);

    private static async Task<Results<Ok, UnauthorizedHttpResult>> ConfirmEmailVerificationAsync(
        [FromBody] EmailVerificationConfirm req,
        AuthStore auth,
        CancellationToken ct)
    {
        var success = await auth.ConfirmEmailVerificationAsync(req.Token, ct);
        return success ? TypedResults.Ok() : TypedResults.Unauthorized();
    }

    public record TokenExchangeResponse(string Token, string TokenType, long ExpiresIn);

    private static async Task<Results<Ok<TokenExchangeResponse>, UnauthorizedHttpResult>> TokenExchangeAsync(
        HttpContext httpContext,
        SessionStore sessions,
        TokenExchangeStore tokenExchange,
        CancellationToken ct)
    {
        var rawToken = ReadSessionToken(httpContext.Request);
        if (rawToken is null) return TypedResults.Unauthorized();

        var session = await sessions.ValidateSessionAsync(rawToken, ct);
        if (session is null) return TypedResults.Unauthorized();

        var token = tokenExchange.CreateToken(session.UserId, session.OrgId, session.ClaimsJson);
        return TypedResults.Ok(new TokenExchangeResponse(token, "Bearer", 300));
    }
}