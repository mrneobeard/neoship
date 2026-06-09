using System.Security.Cryptography;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class MeEndpoints
{
    /// <summary>
    /// The HTTP context item key that stores the authenticated user API key identifier.
    /// </summary>
    public const string UserApiKeyItemKey = "neoship.user_api_key_id";

    public static RouteGroupBuilder MapMeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/me");

        group.MapGet("/", GetMeAsync);
        group.MapGet("/sessions", GetSessionsAsync);
        group.MapPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync);

        group.MapGet("/api-keys", GetApiKeysAsync);
        group.MapPost("/api-keys", CreateApiKeyAsync);
        group.MapPost("/api-keys/{apiKeyId:guid}/revoke", RevokeApiKeyAsync);

        group.MapPost("/mfa/totp/start", StartTotpAsync);
        group.MapPost("/mfa/totp/confirm", ConfirmTotpAsync);
        group.MapPost("/mfa/totp/disable", DisableTotpAsync);
        group.MapPost("/mfa/recovery-codes/regenerate", RegenerateRecoveryCodesAsync);
        group.MapDelete("/mfa/recovery-codes", RevokeRecoveryCodesAsync);

        group.MapGet("/passkeys", GetPasskeysAsync);
        group.MapPost("/passkeys/begin-registration", BeginPasskeyRegistrationAsync);
        group.MapPost("/passkeys/{factorId:guid}/revoke", RevokePasskeyAsync);

        return group;
    }

    /// <summary>
    /// Authenticates a user from a session cookie or bearer user API key.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated <see cref="User"/>, or <see langword="null"/>.</returns>
    public static async Task<User?> AuthenticateAsync(HttpContext httpContext, SessionStore sessions, CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[AuthEndpoints.SessionCookieName];
        if (rawToken is not null)
        {
            var session = await sessions.ValidateSessionAsync(rawToken, ct);
            if (session is not null)
            {
                return await httpContext.RequestServices.GetRequiredService<ShipDb>()
                    .Users.FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
            }

            return null;
        }

        var bearerToken = ReadBearerToken(httpContext.Request);
        if (bearerToken is null)
        {
            return null;
        }

        var apiKeys = httpContext.RequestServices.GetRequiredService<ApiKeyStore>();
        var apiKey = await apiKeys.AuthenticateUserApiKeyAsync(bearerToken, ct);
        if (apiKey?.User is null)
        {
            return null;
        }

        httpContext.Items[UserApiKeyItemKey] = apiKey.Id;
        return apiKey.User;
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl);

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult>> GetMeAsync(
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    public record SessionResponse(
        Guid Id,
        string? IpAddress,
        string? UserAgent,
        DateTime CreatedAt,
        DateTime LastUsedAt,
        DateTime ExpiresAt);

    private static async Task<Results<Ok<List<SessionResponse>>, UnauthorizedHttpResult>> GetSessionsAsync(
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var userSessions = await sessions.ListSessionsAsync(user.Id, ct);

        var result = userSessions.Select(s => new SessionResponse(
            s.Id,
            s.IpAddress,
            s.UserAgent,
            s.CreatedAt,
            s.LastUsedAt,
            s.ExpiresAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokeSessionAsync(
        Guid sessionId,
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var db = httpContext.RequestServices.GetRequiredService<ShipDb>();
        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id, ct);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        await sessions.RevokeSessionAsync(sessionId, "user_revoked", ct);

        return TypedResults.Ok();
    }

    public record ApiKeyResponse(
        Guid Id,
        string Name,
        string? Description,
        string? ScopesJson,
        DateTime CreatedAt,
        DateTime? LastUsedAt,
        DateTime? ExpiresAt);

    public record CreateApiKeyResponse(
        Guid Id,
        string Name,
        string PlaintextKey,
        DateTime CreatedAt);

    private static async Task<Results<Ok<List<ApiKeyResponse>>, UnauthorizedHttpResult>> GetApiKeysAsync(
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var keys = await apiKeys.ListUserApiKeysAsync(user.Id, ct);

        var result = keys.Select(k => new ApiKeyResponse(
            k.Id, k.Name, k.Description, k.ScopesJson, k.CreatedAt, k.LastUsedAt, k.ExpiresAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    public record CreateApiKeyRequest(string Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private static async Task<Results<Created<CreateApiKeyResponse>, UnauthorizedHttpResult>> CreateApiKeyAsync(
        [FromBody] CreateApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        ShipDb db,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(
            user.Id, req.Name, req.Description, req.ScopesJson, req.ExpiresAt);

        db.UserApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/v1/me/api-keys/{apiKey.Id}",
            new CreateApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokeApiKeyAsync(
        Guid apiKeyId,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var success = await apiKeys.RevokeUserApiKeyAsync(apiKeyId, user.Id, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok();
    }

    private sealed record StartTotpRequest(string? Name);

    private sealed record StartTotpResponse(Guid FactorId, string Secret, string OtpAuthUri);

    private sealed record ConfirmTotpRequest(Guid FactorId, string Code);

    private sealed record DisableTotpRequest(Guid FactorId);

    private sealed record RegenerateRecoveryCodesRequest(int? Count);

    private sealed record RegenerateRecoveryCodesResponse(IReadOnlyList<string> Codes);

    private sealed record PasskeyResponse(Guid Id, string Name, DateTime CreatedAt, DateTime? LastUsedAt);

    private sealed record BeginPasskeyRegistrationResponse(string OptionsJson);

    private static async Task<Results<Ok<StartTotpResponse>, UnauthorizedHttpResult>> StartTotpAsync(
        [FromBody] StartTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var (factor, secret) = await mfa.StartTotpAsync(user.Id, req.Name, ct);
        var issuer = Uri.EscapeDataString("NeoShip");
        var label = Uri.EscapeDataString($"NeoShip:{user.Email}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits=6&period=30";

        return TypedResults.Ok(new StartTotpResponse(factor.Id, secret, uri));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> ConfirmTotpAsync(
        [FromBody] ConfirmTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var confirmed = await mfa.ConfirmTotpAsync(user.Id, req.FactorId, req.Code, ct);
        return confirmed ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> DisableTotpAsync(
        [FromBody] DisableTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var disabled = await mfa.DisableTotpAsync(user.Id, req.FactorId, ct);
        return disabled ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<RegenerateRecoveryCodesResponse>, UnauthorizedHttpResult>> RegenerateRecoveryCodesAsync(
        [FromBody] RegenerateRecoveryCodesRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var codes = await mfa.RegenerateRecoveryCodesAsync(user.Id, req.Count ?? 10, ct);
        return TypedResults.Ok(new RegenerateRecoveryCodesResponse(codes));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> RevokeRecoveryCodesAsync(
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        await mfa.RevokeRecoveryCodesAsync(user.Id, ct);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<List<PasskeyResponse>>, UnauthorizedHttpResult>> GetPasskeysAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var factors = await passkeys.ListAsync(user.Id, ct);
        var response = factors.Select(x => new PasskeyResponse(x.Id, x.Name, x.CreatedAt, x.LastUsedAt)).ToList();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<BeginPasskeyRegistrationResponse>, UnauthorizedHttpResult>> BeginPasskeyRegistrationAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var options = await passkeys.BeginRegistrationAsync(user, ct);
        return TypedResults.Ok(new BeginPasskeyRegistrationResponse(options.ToJson()));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokePasskeyAsync(
        Guid factorId,
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var revoked = await passkeys.RevokeAsync(user.Id, factorId, ct);
        return revoked ? TypedResults.Ok() : TypedResults.NotFound();
    }
}