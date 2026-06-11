using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization invite endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapInviteEndpoints();
/// </code>
/// </example>
public static class InviteEndpoints
{
    /// <summary>
    /// Maps current-organization invite endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/invites");
        group.MapGet("", ListInvitesAsync);
        group.MapPost("", CreateInviteAsync);
        group.MapPost("/{inviteId:guid}/revoke", RevokeInviteAsync);

        var userManagementGroup = routes.MapGroup("/api/v1/users/invites");
        userManagementGroup.MapGet("", ListInvitesAsync);
        userManagementGroup.MapPost("", CreateInviteAsync);
        userManagementGroup.MapPost("/{inviteId:guid}/revoke", RevokeInviteAsync);
        return routes;
    }

    private sealed record OrganizationInviteResponse(Guid Id, string Email, DateTime CreatedAt, DateTime ExpiresAt, DateTime? AcceptedAt, DateTime? RevokedAt);

    private sealed record CreateOrganizationInviteResponse(Guid Id, string Email, string Token, DateTime CreatedAt, DateTime ExpiresAt);

    private sealed record CreateOrganizationInviteRequest(string Email, List<Guid>? RoleIds, List<Guid>? GroupIds);

    private sealed record ParsedEmailListQuery(int Limit, int Offset, string? FilterEmail, string Sort, Dictionary<string, string[]> Errors);

    private static async Task<IResult> ListInvitesAsync([FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[email]")] string? filterEmail, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, OrganizationStore orgs, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.members", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseEmailListQuery(httpContext, limit, cursor, filterEmail, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<OrganizationInvite> filtered = await orgs.ListInvitesAsync(auth.Org.Id, ct);
        if (!string.IsNullOrWhiteSpace(query.FilterEmail))
        {
            filtered = filtered.Where(x => x.Email.Contains(query.FilterEmail, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort switch
        {
            "createdAt" => filtered.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => filtered.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
        };

        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(ToInviteResponse).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterEmail) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["email"] = query.FilterEmail };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<OrganizationInviteResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateInviteAsync([FromBody] CreateOrganizationInviteRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, OrganizationStore orgs, AuditStore audit, IEmailSender emails, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.members", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateCreateInvite(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (invite, token) = await orgs.CreateInviteAsync(auth.Org.Id, auth.User!.Id, req.Email, req.RoleIds ?? [], req.GroupIds ?? [], ct);
        await audit.RecordAsync("org.invites.create", auth.Org.Id, auth.User.Id, "org.invite.create", targetType: "organization_invite", targetId: invite.Id.ToString(), ct: ct);
        await emails.SendAsync(new EmailMessage(invite.Email, $"You're invited to {auth.Org.Name}", $"You've been invited to join {auth.Org.Name}. Accept the invite at /accept-invite?token={token}"), ct);
        return TypedResults.Created($"/api/v1/invites/{invite.Id}", Envelope(httpContext, new CreateOrganizationInviteResponse(invite.Id, invite.Email, token, invite.CreatedAt, invite.ExpiresAt)));
    }

    private static async Task<IResult> RevokeInviteAsync(Guid inviteId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, OrganizationStore orgs, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.members", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var revoked = await orgs.RevokeInviteAsync(auth.Org.Id, inviteId, ct);
        if (!revoked)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.invites.revoke", auth.Org.Id, auth.User!.Id, "org.invite.revoke", targetType: "organization_invite", targetId: inviteId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static OrganizationInviteResponse ToInviteResponse(OrganizationInvite invite)
        => new(invite.Id, invite.Email, invite.CreatedAt, invite.ExpiresAt, invite.AcceptedAt, invite.RevokedAt);

    private static Dictionary<string, string[]> ValidateCreateInvite(CreateOrganizationInviteRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        var email = req.Email?.Trim() ?? string.Empty;
        if (!IsValidEmail(email))
        {
            errors["email"] = ["Email must be a valid email address and 320 characters or fewer."];
        }

        var roleIds = req.RoleIds ?? [];
        if (roleIds.Any(x => x == Guid.Empty) || roleIds.Count != roleIds.Distinct().Count())
        {
            errors["roleIds"] = ["Role identifiers must be non-empty and unique."];
        }

        var groupIds = req.GroupIds ?? [];
        if (groupIds.Any(x => x == Guid.Empty) || groupIds.Count != groupIds.Distinct().Count())
        {
            errors["groupIds"] = ["Group identifiers must be non-empty and unique."];
        }

        return errors;
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

    private static ParsedEmailListQuery ParseEmailListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterEmail, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "limit", "cursor", "filter[email]", "sort" };
        foreach (var key in httpContext.Request.Query.Keys)
        {
            if (!allowed.Contains(key))
            {
                errors[key] = ["Query parameter is not supported."];
            }
        }

        var resolvedLimit = limit ?? 50;
        if (resolvedLimit is < 1 or > 100)
        {
            errors["limit"] = ["Limit must be between 1 and 100."];
            resolvedLimit = 50;
        }

        var resolvedSort = string.IsNullOrWhiteSpace(sort) ? "-createdAt" : sort.Trim();
        if (resolvedSort is not ("createdAt" or "-createdAt"))
        {
            errors["sort"] = ["Sort must be createdAt or -createdAt."];
            resolvedSort = "-createdAt";
        }

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !TryDecodeCursor(cursor, out offset))
        {
            errors["cursor"] = ["Cursor is invalid."];
        }

        if (filterEmail is { Length: > 320 })
        {
            errors["filter.email"] = ["Email filter must be 320 characters or fewer."];
        }

        return new ParsedEmailListQuery(resolvedLimit, offset, filterEmail, resolvedSort, errors);
    }

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(offset.ToString(System.Globalization.CultureInfo.InvariantCulture))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryDecodeCursor(string cursor, out int offset)
    {
        offset = 0;
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
            var value = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out offset) && offset >= 0;
        }
        catch (FormatException)
        {
            return false;
        }
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
