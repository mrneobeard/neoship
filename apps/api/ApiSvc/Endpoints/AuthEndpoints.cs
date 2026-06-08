using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NeoShip.ApiSvc.Services;
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
        group.MapPost("/logout", LogoutAsync);

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
        AuthService auth,
        CancellationToken ct)
    {
        var (result, user, _, rawToken) = await auth.SignupAsync(
            req.Email, req.Name, req.Password, Constants.DefaultOrganizationId, null, null, ct);

        if (result == SignupResult.EmailAlreadyExists || user is null)
        {
            return TypedResults.Conflict("Email already registered.");
        }

        return TypedResults.Created($"/api/v1/me", new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    public record LoginRequest(string Email, string Password);

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult, StatusCodeHttpResult>> LoginAsync(
        [FromBody] LoginRequest req,
        HttpContext httpContext,
        AuthService auth,
        CancellationToken ct)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var (result, user, session, rawToken) = await auth.LoginAsync(
            req.Email, req.Password, Constants.DefaultOrganizationId, ip, userAgent, ct);

        if (result == LoginResult.AccountLocked)
        {
            return TypedResults.StatusCode(423);
        }

        if (result != LoginResult.Success || user is null || session is null || rawToken is null)
        {
            return TypedResults.Unauthorized();
        }

        SetSessionCookie(httpContext.Response, rawToken, session.ExpiresAt);

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    private static async Task<Ok> LogoutAsync(
        HttpContext httpContext,
        AuthService auth,
        CancellationToken ct)
    {
        var rawToken = ReadSessionToken(httpContext.Request);

        if (rawToken is not null)
        {
            var tokenBytes = Convert.FromBase64String(rawToken);
            var digest = SHA256.HashData(tokenBytes);
            var digestBase64 = Convert.ToBase64String(digest);
            await auth.LogoutAsync(digestBase64, ct);
        }

        ClearSessionCookie(httpContext.Response);
        return TypedResults.Ok();
    }
}
