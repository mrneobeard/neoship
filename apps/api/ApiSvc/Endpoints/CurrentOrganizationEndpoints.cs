using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization metadata endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapCurrentOrganizationEndpoints();
/// </code>
/// </example>
public static class CurrentOrganizationEndpoints
{
    /// <summary>
    /// Maps current-organization metadata endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapCurrentOrganizationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/org");
        group.MapGet("", GetCurrentOrganizationAsync);
        group.MapPatch("", UpdateCurrentOrganizationAsync);
        group.MapDelete("", DeleteCurrentOrganizationAsync);
        return routes;
    }

    private sealed record OrganizationResponse(Guid Id, string Name, string Slug, string Status, string TenantMode, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record UpdateOrganizationRequest(string? Name);

    private sealed record DeleteOrganizationRequest(string Confirmation);

    private static async Task<IResult> GetCurrentOrganizationAsync(HttpContext httpContext, SessionStore sessions, ShipDb db, CancellationToken ct)
    {
        var user = await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");
        }

        var org = await db.Orgs.FindAsync([user.OrgId], ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        return TypedResults.Ok(Envelope(httpContext, ToResponse(org)));
    }

    private static async Task<IResult> UpdateCurrentOrganizationAsync([FromBody] UpdateOrganizationRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, OrganizationStore organizations, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.settings", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateOrganizationInput(req.Name);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var updated = await organizations.UpdateAsync(auth.Org.Id, req.Name, ct);
        if (updated is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.update", auth.Org.Id, auth.User!.Id, "org.update", targetType: "org", targetId: auth.Org.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToResponse(updated)));
    }

    private static async Task<IResult> DeleteCurrentOrganizationAsync([FromBody] DeleteOrganizationRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, OrganizationStore organizations, ShipDb db, AuditStore audit, IConfiguration configuration, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.settings", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!string.Equals(req.Confirmation?.Trim(), auth.Org.Slug, StringComparison.Ordinal))
        {
            return ValidationError(httpContext, new Dictionary<string, string[]> { ["confirmation"] = ["Confirmation must match the organization slug."] });
        }

        var retentionDays = Math.Max(0, configuration.GetValue("Auth:Deletion:RetentionDays", 30));
        var deleted = await organizations.DeleteAsync(auth.Org.Id, retentionDays, ct);
        if (deleted is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.delete", auth.Org.Id, auth.User!.Id, "org.delete", targetType: "org", targetId: auth.Org.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToResponse(deleted)));
    }

    private static OrganizationResponse ToResponse(Organization org)
        => new(org.Id, org.Name, org.Slug, org.Status.Name, org.TenantMode.Name, org.CreatedAt, org.UpdatedAt);

    private static Dictionary<string, string[]> ValidateOrganizationInput(string? name)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        return errors;
    }

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");
}
