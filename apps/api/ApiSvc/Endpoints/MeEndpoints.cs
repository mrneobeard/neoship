using System.Security.Cryptography;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class MeEndpoints
{
    public static RouteGroupBuilder MapMeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/me");

        group.MapGet("/", GetMeAsync);
        group.MapGet("/sessions", GetSessionsAsync);
        group.MapPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync);

        group.MapGet("/api-keys", GetApiKeysAsync);
        group.MapPost("/api-keys", CreateApiKeyAsync);
        group.MapPost("/api-keys/{apiKeyId:guid}/revoke", RevokeApiKeyAsync);

        return group;
    }

    public static async Task<User?> AuthenticateAsync(HttpContext httpContext, SessionStore sessions, CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[AuthEndpoints.SessionCookieName];
        if (rawToken is null)
        {
            return null;
        }

        var session = await sessions.ValidateSessionAsync(rawToken, ct);
        if (session is null)
        {
            return null;
        }

        return await httpContext.RequestServices.GetRequiredService<ShipDb>()
            .Users.FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
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
}