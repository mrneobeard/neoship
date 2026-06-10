using Fido2NetLib;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class AuthEndpoints
{
    public const string SessionCookieName = "neoship_sid";
    public const string LoginRateLimitPolicy = "auth-login";
    public const string SensitiveRateLimitPolicy = "auth-sensitive";

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/auth");

        group.MapPost("/signup", SignupAsync);
        group.MapPost("/login", LoginAsync).RequireRateLimiting(LoginRateLimitPolicy);
        group.MapGet("/sso/{orgSlug}/begin", BeginSsoAsync);
        group.MapGet("/sso/callback", FinishSsoAsync);
        group.MapPost("/passkeys/begin-login", BeginPasskeyLoginAsync).RequireRateLimiting(LoginRateLimitPolicy);
        group.MapPost("/passkeys/finish-login", FinishPasskeyLoginAsync).RequireRateLimiting(LoginRateLimitPolicy);
        group.MapPost("/api-keys/login", LoginWithApiKeyAsync).RequireRateLimiting(LoginRateLimitPolicy);
        group.MapPost("/logout", LogoutAsync);

        group.MapPost("/password-reset/request", RequestPasswordResetAsync).RequireRateLimiting(SensitiveRateLimitPolicy);
        group.MapPost("/password-reset/confirm", ConfirmPasswordResetAsync).RequireRateLimiting(SensitiveRateLimitPolicy);

        group.MapPost("/email-verification/request", RequestEmailVerificationAsync).RequireRateLimiting(SensitiveRateLimitPolicy);
        group.MapPost("/email-verification/confirm", ConfirmEmailVerificationAsync).RequireRateLimiting(SensitiveRateLimitPolicy);

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

    private static async Task<IResult> SignupAsync(
        [FromBody] SignupRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateSignup(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (result, user, _, _) = await auth.SignupAsync(
            req.Email, req.Name, req.Password, Constants.DefaultOrganizationId, ct);

        if (result == SignupResult.AuthMethodNotAllowed)
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);

        if (result == SignupResult.EmailAlreadyExists || user is null)
            return TypedResults.Conflict("Email already registered.");

        return TypedResults.Created($"/api/v1/me", Envelope(httpContext, new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl)));
    }

    public record LoginRequest(string Email, string Password);

    public record BeginSsoResponse(long ProviderId, string AuthorizationUrl, string State, DateTime ExpiresAt);

    public record BeginPasskeyLoginRequest(string Email);

    public record BeginPasskeyLoginResponse(Guid ChallengeId, string OptionsJson);

    public record FinishPasskeyLoginRequest(Guid ChallengeId, AuthenticatorAssertionRawResponse Response);

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateLogin(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (result, user, session, rawToken) = await auth.LoginAsync(
            req.Email, req.Password, Constants.DefaultOrganizationId, ct);

        if (result == LoginResult.AccountLocked)
            return TypedResults.StatusCode(423);

        if (result == LoginResult.AuthMethodNotAllowed)
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);

        if (result == LoginResult.MfaRequired)
            return TypedResults.Json(
                new ApiErrorEnvelope(
                    new ApiError("mfa_required", "MFA enrollment is required."),
                    ApiMeta.FromHttpContext(httpContext)),
                statusCode: StatusCodes.Status403Forbidden);

        if (result != LoginResult.Success || user is null || session is null || rawToken is null)
            return TypedResults.Unauthorized();

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(Envelope(httpContext, new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl)));
    }

    private static async Task<IResult> BeginPasskeyLoginAsync(
        [FromBody] BeginPasskeyLoginRequest req,
        HttpContext httpContext,
        PasskeyStore passkeys,
        PasskeyChallengeStore challenges,
        CancellationToken ct)
    {
        var validation = ValidateEmailRequest(req.Email);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var result = await passkeys.BeginLoginAsync(req.Email, ct);
        if (result is null)
        {
            return TypedResults.Unauthorized();
        }

        var (user, options) = result.Value;
        var challengeId = challenges.StoreLogin(user.Id, options);
        return TypedResults.Ok(Envelope(httpContext, new BeginPasskeyLoginResponse(challengeId, options.ToJson())));
    }

    private static async Task<IResult> BeginSsoAsync(
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

        return TypedResults.Ok(Envelope(httpContext, new BeginSsoResponse(result.ProviderId, result.AuthorizationUrl, result.State, result.ExpiresAt)));
    }

    private static async Task<IResult> FinishSsoAsync(
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

        return TypedResults.Ok(Envelope(httpContext, new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl)));
    }

    private static async Task<IResult> FinishPasskeyLoginAsync(
        [FromBody] FinishPasskeyLoginRequest req,
        HttpContext httpContext,
        PasskeyStore passkeys,
        PasskeyChallengeStore challenges,
        PermissionResolver permissions,
        SessionStore sessions,
        AuditStore audit,
        CancellationToken ct)
    {
        var validation = ValidateFinishPasskeyLogin(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

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
        await audit.RecordAsync("auth.passkey.login", user.OrgId, user.Id, "passkey.login", ct: ct);

        return TypedResults.Ok(Envelope(httpContext, new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl)));
    }

    /// <summary>
    /// Represents an API key login request.
    /// </summary>
    /// <param name="ApiKey">The plaintext API key.</param>
    public record ApiKeyLoginRequest(string ApiKey);

    private static async Task<IResult> LoginWithApiKeyAsync(
        [FromBody] ApiKeyLoginRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateApiKeyLogin(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (result, user, session, rawToken) = await auth.LoginWithUserApiKeyAsync(req.ApiKey, ct);

        if (result == LoginResult.AccountLocked)
            return TypedResults.StatusCode(423);

        if (result == LoginResult.AuthMethodNotAllowed)
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);

        if (result != LoginResult.Success || user is null || session is null || rawToken is null)
            return TypedResults.Unauthorized();

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(Envelope(httpContext, new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl)));
    }

    private static async Task<Ok<ApiEnvelope<object>>> LogoutAsync(
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
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record PasswordResetRequest(string Email);

    private static async Task<IResult> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateEmailRequest(req.Email);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        await auth.RequestPasswordResetAsync(req.Email, ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record PasswordResetConfirm(string Token, string NewPassword);

    private static async Task<IResult> ConfirmPasswordResetAsync(
        [FromBody] PasswordResetConfirm req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidatePasswordResetConfirm(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var success = await auth.ConfirmPasswordResetAsync(req.Token, req.NewPassword, ct);
        return success ? TypedResults.Ok(Envelope<object>(httpContext, null)) : TypedResults.Unauthorized();
    }

    public record EmailVerificationRequest(string Email);

    private static async Task<IResult> RequestEmailVerificationAsync(
        [FromBody] EmailVerificationRequest req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateEmailRequest(req.Email);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        await auth.RequestEmailVerificationAsync(req.Email, ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record EmailVerificationConfirm(string Token);

    private static async Task<IResult> ConfirmEmailVerificationAsync(
        [FromBody] EmailVerificationConfirm req,
        HttpContext httpContext,
        AuthStore auth,
        CancellationToken ct)
    {
        var validation = ValidateToken(req.Token);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var success = await auth.ConfirmEmailVerificationAsync(req.Token, ct);
        return success ? TypedResults.Ok(Envelope<object>(httpContext, null)) : TypedResults.Unauthorized();
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

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => TypedResults.Json(
            new ApiErrorEnvelope(
                new ApiError("validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields }),
                ApiMeta.FromHttpContext(httpContext)),
            statusCode: StatusCodes.Status422UnprocessableEntity);

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    private static Dictionary<string, string[]> ValidateSignup(SignupRequest req)
    {
        var errors = ValidateEmailRequest(req.Email);

        if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 160)
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        AddPasswordErrors(errors, req.Password, "password");
        return errors;
    }

    private static Dictionary<string, string[]> ValidateLogin(LoginRequest req)
    {
        var errors = ValidateEmailRequest(req.Email);
        if (string.IsNullOrEmpty(req.Password))
        {
            errors["password"] = ["Password is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateEmailRequest(string? email)
    {
        var errors = new Dictionary<string, string[]>();
        var value = email?.Trim() ?? string.Empty;
        if (!IsValidEmail(value))
        {
            errors["email"] = ["Email must be a valid email address and 320 characters or fewer."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateFinishPasskeyLogin(FinishPasskeyLoginRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (req.ChallengeId == Guid.Empty)
        {
            errors["challengeId"] = ["Challenge ID is required."];
        }

        if (req.Response is null)
        {
            errors["response"] = ["Passkey assertion response is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateApiKeyLogin(ApiKeyLoginRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(req.ApiKey))
        {
            errors["apiKey"] = ["API key is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidatePasswordResetConfirm(PasswordResetConfirm req)
    {
        var errors = ValidateToken(req.Token);
        AddPasswordErrors(errors, req.NewPassword, "newPassword");
        return errors;
    }

    private static Dictionary<string, string[]> ValidateToken(string? token)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(token))
        {
            errors["token"] = ["Token is required."];
        }

        return errors;
    }

    private static void AddPasswordErrors(Dictionary<string, string[]> errors, string? password, string field)
    {
        if (string.IsNullOrEmpty(password) || password.Length is < 12 or > 256)
        {
            errors[field] = ["Password must be 12 to 256 characters."];
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length is < 3 or > 320)
        {
            return false;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && email[(at + 1)..].Contains('.', StringComparison.Ordinal);
    }
}
