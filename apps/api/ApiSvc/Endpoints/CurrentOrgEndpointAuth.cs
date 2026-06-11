using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Provides current-organization authorization helpers for endpoint aliases.
/// </summary>
/// <example>
/// <code>
/// var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "read"), ct);
/// </code>
/// </example>
internal static class CurrentOrgEndpointAuth
{
    private const double DefaultStepUpWindowMinutes = 15;

    /// <summary>
    /// Requires an authenticated principal with an organization-scoped permission in the current organization.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="permissions">The permission resolver.</param>
    /// <param name="db">The database context.</param>
    /// <param name="permission">The required permission.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="allowServiceAccount">A value indicating whether service-account bearer authentication is allowed.</param>
    /// <returns>The current-organization authorization result.</returns>
    internal static async Task<CurrentOrgAuthContext> RequireAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        PermissionKey permission,
        CancellationToken ct,
        bool allowServiceAccount = false)
    {
        var user = await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);
        if (user is not null)
        {
            var org = await db.Orgs.FirstOrDefaultAsync(x => x.Id == user.OrgId, ct);
            if (org is null)
            {
                return new CurrentOrgAuthContext(null, null, null!, NotFoundError(httpContext));
            }

            var allowed = await HasUserOrgPermissionAsync(httpContext, permissions, user.Id, permission, org.Slug, ct);
            if (!allowed)
            {
                return new CurrentOrgAuthContext(null, null, org, Forbidden(httpContext));
            }

            if (permission.Action == "write")
            {
                var stepUpFailure = await RequireRecentSessionAsync(httpContext, ct);
                if (stepUpFailure is not null)
                {
                    return new CurrentOrgAuthContext(null, null, org, stepUpFailure);
                }
            }

            return new CurrentOrgAuthContext(user, null, org, null);
        }

        var token = ReadBearerToken(httpContext.Request);
        var serviceAccountKey = token is null ? null : await httpContext.RequestServices.GetRequiredService<ServiceAccountStore>().AuthenticateApiKeyAsync(token, ct);
        if (serviceAccountKey?.ServiceAccount is null)
        {
            return new CurrentOrgAuthContext(null, null, null!, Unauthenticated(httpContext));
        }

        var serviceAccountOrg = await db.Orgs.FirstOrDefaultAsync(x => x.Id == serviceAccountKey.ServiceAccount.OrgId, ct);
        if (serviceAccountOrg is null)
        {
            return new CurrentOrgAuthContext(null, null, null!, NotFoundError(httpContext));
        }

        if (!allowServiceAccount)
        {
            return new CurrentOrgAuthContext(null, null, serviceAccountOrg, Forbidden(httpContext));
        }

        var serviceAccountAllowed = (await permissions.ResolveServiceAccountApiKeyAsync(serviceAccountKey.Id, ct)).Allows(permission, PermissionScopeKind.Organization, serviceAccountOrg.Slug);
        return serviceAccountAllowed
            ? new CurrentOrgAuthContext(null, serviceAccountKey, serviceAccountOrg, null)
            : new CurrentOrgAuthContext(null, null, serviceAccountOrg, Forbidden(httpContext));
    }

    private static async Task<bool> HasUserOrgPermissionAsync(HttpContext httpContext, PermissionResolver permissions, Guid userId, PermissionKey permission, string orgSlug, CancellationToken ct)
    {
        if (httpContext.Items.TryGetValue(MeEndpoints.UserApiKeyItemKey, out var value) && value is Guid apiKeyId)
        {
            return (await permissions.ResolveUserApiKeyAsync(apiKeyId, ct)).Allows(permission, PermissionScopeKind.Organization, orgSlug);
        }

        return await permissions.UserHasAsync(userId, permission, PermissionScopeKind.Organization, orgSlug, ct);
    }

    private static async Task<IResult?> RequireRecentSessionAsync(HttpContext httpContext, CancellationToken ct)
    {
        if (!httpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Auth:StepUp:Enabled", true))
        {
            return null;
        }

        var sessionId = httpContext.RequestServices.GetRequiredService<RequestContext>().SessionId;
        if (sessionId is null)
        {
            return StepUpRequired(httpContext);
        }

        var session = await httpContext.RequestServices.GetRequiredService<ShipDb>().UserSessions.FirstOrDefaultAsync(x => x.Id == sessionId.Value, ct);
        if (session is null)
        {
            return StepUpRequired(httpContext);
        }

        var windowMinutes = httpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Auth:StepUp:WindowMinutes", DefaultStepUpWindowMinutes);
        var window = TimeSpan.FromMinutes(windowMinutes <= 0 ? DefaultStepUpWindowMinutes : windowMinutes);
        var lastVerifiedAt = session.MfaVerifiedAt is not null && session.MfaVerifiedAt > session.CreatedAt ? session.MfaVerifiedAt.Value : session.CreatedAt;
        return lastVerifiedAt < DateTime.UtcNow.Subtract(window) ? StepUpRequired(httpContext) : null;
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

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult StepUpRequired(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "step_up_required", "Recent authentication is required for this operation.");

    private static IResult Unauthenticated(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");

    private static IResult Forbidden(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");
}

/// <summary>
/// Represents current-organization authorization state for an endpoint request.
/// </summary>
/// <example>
/// <code>
/// if (auth.Failure is not null) return auth.Failure;
/// </code>
/// </example>
/// <param name="User">The authenticated human user, when present.</param>
/// <param name="ServiceAccountKey">The authenticated service-account API key, when present.</param>
/// <param name="Org">The current organization.</param>
/// <param name="Failure">The failure result, when authorization failed.</param>
internal sealed record CurrentOrgAuthContext(User? User, ServiceAccountApiKey? ServiceAccountKey, Organization Org, IResult? Failure);
