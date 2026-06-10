using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps tenant and organization endpoints.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// app.MapTenantEndpoints();
/// </code>
/// </remarks>
public static class TenantEndpoints
{
    /// <summary>
    /// Maps tenant and organization routes.
    /// </summary>
    /// <param name="routes">The route builder.</param>
    /// <returns>The mapped <see cref="RouteGroupBuilder"/>.</returns>
    public static RouteGroupBuilder MapTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs");

        group.MapGet("/", ListOrganizationsAsync);
        group.MapPost("/", CreateOrganizationAsync);
        group.MapGet("/{orgSlug}", GetOrganizationAsync);
        group.MapPatch("/{orgSlug}", UpdateOrganizationAsync);
        group.MapDelete("/{orgSlug}", DeleteOrganizationAsync);

        return group;
    }

    private sealed record OrganizationResponse(Guid Id, string Name, string Slug, string Status, string TenantMode, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record CreateOrganizationRequest(string Name, string Slug);

    private sealed record UpdateOrganizationRequest(string? Name);

    private sealed record DeleteOrganizationRequest(string Confirmation);

    private static OrganizationResponse ToResponse(Organization org)
        => new(org.Id, org.Name, org.Slug, org.Status.Name, org.TenantMode.Name, org.CreatedAt, org.UpdatedAt);

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static async Task<User?> AuthenticateAsync(HttpContext httpContext, SessionStore sessions, CancellationToken ct)
        => await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);

    private static async Task<IResult> ListOrganizationsAsync(
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var orgs = await organizations.ListForUserAsync(user.Id, ct);
        var data = orgs.Select(ToResponse).ToList();
        var pagination = new ApiPagination(data.Count, nextCursor: null, previousCursor: null, hasMore: false);

        return TypedResults.Ok(new ApiCollectionEnvelope<OrganizationResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> CreateOrganizationAsync(
        [FromBody] CreateOrganizationRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var validation = ValidateOrganizationInput(req.Name, req.Slug);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var org = await organizations.CreateAsync(user.Id, req.Name, req.Slug, ct);
        if (org is null)
        {
            return Error(httpContext, StatusCodes.Status409Conflict, "conflict", "Organization slug is unavailable.");
        }

        await audit.RecordAsync("org.create", org.Id, user.Id, "org.create", targetType: "org", targetId: org.Id.ToString(), ct: ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{org.Slug}",
            new ApiEnvelope<OrganizationResponse>(ToResponse(org), ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> GetOrganizationAsync(
        string orgSlug,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var org = await organizations.GetBySlugAsync(orgSlug, ct);
        if (org is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "tenant_not_found", "Organization not found.");
        }

        if (!await organizations.UserCanAccessAsync(user.Id, org.Id, ct))
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        return TypedResults.Ok(new ApiEnvelope<OrganizationResponse>(ToResponse(org), ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> UpdateOrganizationAsync(
        string orgSlug,
        [FromBody] UpdateOrganizationRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        PermissionResolver permissions,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var org = await organizations.GetBySlugAsync(orgSlug, ct);
        if (org is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "tenant_not_found", "Organization not found.");
        }

        if (!await organizations.UserCanAccessAsync(user.Id, org.Id, ct))
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        var allowed = await permissions.UserHasAsync(user.Id, PermissionKey.Create("org.settings", "write"), PermissionScopeKind.Organization, org.Slug, ct);
        if (!allowed)
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        var validation = ValidateOrganizationInput(req.Name, org.Slug, validateSlug: false);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var updated = await organizations.UpdateAsync(org.Id, req.Name, ct);
        if (updated is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Organization not found.");
        }

        await audit.RecordAsync("org.update", org.Id, user.Id, "org.update", targetType: "org", targetId: org.Id.ToString(), ct: ct);

        return TypedResults.Ok(new ApiEnvelope<OrganizationResponse>(ToResponse(updated), ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> DeleteOrganizationAsync(
        string orgSlug,
        [FromBody] DeleteOrganizationRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        PermissionResolver permissions,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var org = await organizations.GetBySlugAsync(orgSlug, ct);
        if (org is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "tenant_not_found", "Organization not found.");
        }

        if (!await organizations.UserCanAccessAsync(user.Id, org.Id, ct))
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        var allowed = await permissions.UserHasAsync(user.Id, PermissionKey.Create("org.settings", "write"), PermissionScopeKind.Organization, org.Slug, ct);
        if (!allowed)
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        if (!string.Equals(req.Confirmation?.Trim(), org.Slug, StringComparison.Ordinal))
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?>
            {
                ["fields"] = new Dictionary<string, string[]> { ["confirmation"] = ["Confirmation must match the organization slug."] },
            });
        }

        var deleted = await organizations.DeleteAsync(org.Id, ct);
        if (deleted is null)
        {
            return Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Organization not found.");
        }

        await audit.RecordAsync("org.delete", org.Id, user.Id, "org.delete", targetType: "org", targetId: org.Id.ToString(), ct: ct);
        return TypedResults.Ok(new ApiEnvelope<OrganizationResponse>(ToResponse(deleted), ApiMeta.FromHttpContext(httpContext)));
    }

    private static Dictionary<string, string[]> ValidateOrganizationInput(string? name, string? slug, bool validateSlug = true)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        if (validateSlug)
        {
            var normalizedSlug = OrganizationStore.NormalizeSlug(slug ?? string.Empty);
            if (!IsValidSlug(normalizedSlug))
            {
                errors["slug"] = ["Slug must be 3-64 lowercase letters, numbers, or hyphens, and start and end with a letter or number."];
            }
        }

        return errors;
    }

    private static bool IsValidSlug(string slug)
    {
        if (slug.Length is < 3 or > 64 || !char.IsAsciiLetterOrDigit(slug[0]) || !char.IsAsciiLetterOrDigit(slug[^1]))
        {
            return false;
        }

        return slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
    }
}
