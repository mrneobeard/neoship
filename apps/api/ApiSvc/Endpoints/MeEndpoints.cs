using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NeoShip.ApiSvc.Services;
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

        return group;
    }

    private static async Task<User?> AuthenticateAsync(HttpContext httpContext, SessionService sessions, CancellationToken ct)
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
        SessionService sessions,
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
        SessionService sessions,
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
        SessionService sessions,
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
}
