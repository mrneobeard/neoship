using System.Net.Mail;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization group endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapGroupEndpoints();
/// </code>
/// </example>
public static class GroupEndpoints
{
    private const double DefaultStepUpWindowMinutes = 15;

    /// <summary>
    /// Maps current-organization group endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder routes)
    {
        var groups = routes.MapGroup("/api/v1/groups");
        groups.MapGet("", ListGroupsAsync);
        groups.MapPost("", CreateGroupAsync);
        groups.MapGet("/{groupId:guid}", GetGroupAsync);
        groups.MapPatch("/{groupId:guid}", UpdateGroupAsync);
        groups.MapDelete("/{groupId:guid}", DeleteGroupAsync);
        groups.MapPost("/{groupId:guid}/members/{principalType}/{principalId:guid}", AddGroupMemberAsync);
        groups.MapDelete("/{groupId:guid}/members/{principalType}/{principalId:guid}", RemoveGroupMemberAsync);
        groups.MapPost("/{groupId:guid}/roles/{roleId:guid}", AttachGroupRoleAsync);
        groups.MapDelete("/{groupId:guid}/roles/{roleId:guid}", DetachGroupRoleAsync);
        return routes;
    }

    private sealed record GroupResponse(Guid Id, string Name, string? Email, string? Description, int MemberCount, int ServiceAccountMemberCount, int RoleCount);

    private sealed record CreateGroupRequest(string Name, string? Email, string? Description);

    private sealed record UpdateGroupRequest(string? Name, string? Email, string? Description);

    private sealed record AuthContext(User? User, ServiceAccountApiKey? ServiceAccountKey, Organization Org, IResult? Failure);

    private sealed record ParsedNamedListQuery(int Limit, int Offset, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

    private static async Task<IResult> ListGroupsAsync(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<Group> filtered = await groups.ListAsync(auth.Org.Id, ct);
        if (!string.IsNullOrWhiteSpace(query.FilterName))
        {
            filtered = filtered.Where(x => x.Name.Contains(query.FilterName, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort switch
        {
            "-name" => filtered.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            _ => filtered.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
        };

        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var pageGroups = page.Take(query.Limit).ToList();
        var groupIds = pageGroups.Select(x => x.Id).ToList();
        var builtInRoleCounts = await db.RoleAssignments
            .Where(x => x.GroupId != null && groupIds.Contains(x.GroupId.Value))
            .GroupBy(x => x.GroupId!.Value)
            .Select(x => new { GroupId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);
        var data = pageGroups.Select(x => ToGroupResponse(x, builtInRoleCounts.GetValueOrDefault(x.Id))).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<GroupResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateGroupAsync(
        [FromBody] CreateGroupRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateGroupInput(req.Name, req.Email, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var group = await groups.CreateAsync(auth.Org.Id, req.Name, req.Email, req.Description, ct);
        await audit.RecordAsync("org.groups.create", auth.Org.Id, auth.User!.Id, "group.create", targetType: "group", targetId: group.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/groups/{group.Id}", Envelope(httpContext, ToGroupResponse(group)));
    }

    private static async Task<IResult> GetGroupAsync(Guid groupId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var group = await groups.GetAsync(auth.Org.Id, groupId, ct);
        if (group is null)
        {
            return NotFoundError(httpContext);
        }

        var builtInRoleCount = await db.RoleAssignments.CountAsync(x => x.GroupId == group.Id, ct);
        return TypedResults.Ok(Envelope(httpContext, ToGroupResponse(group, builtInRoleCount)));
    }

    private static async Task<IResult> UpdateGroupAsync(Guid groupId, [FromBody] UpdateGroupRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateGroupInput(req.Name, req.Email, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var group = await groups.UpdateAsync(auth.Org.Id, groupId, req.Name, req.Email, req.Description, ct);
        if (group is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.update", auth.Org.Id, auth.User!.Id, "group.update", targetType: "group", targetId: group.Id.ToString(), ct: ct);
        var builtInRoleCount = await db.RoleAssignments.CountAsync(x => x.GroupId == group.Id, ct);
        return TypedResults.Ok(Envelope(httpContext, ToGroupResponse(group, builtInRoleCount)));
    }

    private static async Task<IResult> DeleteGroupAsync(Guid groupId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var deleted = await groups.DeleteAsync(auth.Org.Id, groupId, ct);
        if (!deleted)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.delete", auth.Org.Id, auth.User!.Id, "group.delete", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> AddGroupMemberAsync(Guid groupId, string principalType, Guid principalId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeGroupMemberAsync(groupId, principalType, principalId, add: true, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> RemoveGroupMemberAsync(Guid groupId, string principalType, Guid principalId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeGroupMemberAsync(groupId, principalType, principalId, add: false, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> AttachGroupRoleAsync(Guid groupId, Guid roleId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeGroupRoleAsync(groupId, roleId, attach: true, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> DetachGroupRoleAsync(Guid groupId, Guid roleId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeGroupRoleAsync(groupId, roleId, attach: false, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> ChangeGroupMemberAsync(Guid groupId, string principalType, Guid principalId, bool add, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var userPrincipal = string.Equals(principalType, "user", StringComparison.OrdinalIgnoreCase);
        var serviceAccountPrincipal = string.Equals(principalType, "service-account", StringComparison.OrdinalIgnoreCase) || string.Equals(principalType, "service_account", StringComparison.OrdinalIgnoreCase);
        if (!userPrincipal && !serviceAccountPrincipal)
        {
            return ValidationError(httpContext, new Dictionary<string, string[]> { ["principalType"] = ["Principal type must be user or service-account."] });
        }

        var ok = userPrincipal
            ? add ? await groups.AddUserAsync(auth.Org.Id, groupId, principalId, ct) : await groups.RemoveUserAsync(auth.Org.Id, groupId, principalId, ct)
            : add ? await groups.AddServiceAccountAsync(auth.Org.Id, groupId, principalId, ct) : await groups.RemoveServiceAccountAsync(auth.Org.Id, groupId, principalId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        var type = userPrincipal ? "member" : "service_account";
        await audit.RecordAsync($"org.groups.{type}.{(add ? "add" : "remove")}", auth.Org.Id, auth.User!.Id, $"group.{type}.{(add ? "add" : "remove")}", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ChangeGroupRoleAsync(Guid groupId, Guid roleId, bool attach, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        var ok = builtInRole is not null
            ? attach ? await BuiltInRoleStore.AssignGroupAsync(db, auth.Org.Id, auth.Org.Slug, groupId, builtInRole.Key, auth.User!.Id, ct) : await BuiltInRoleStore.UnassignGroupAsync(db, auth.Org.Id, auth.Org.Slug, groupId, builtInRole.Key, ct)
            : attach ? await groups.AttachRoleAsync(auth.Org.Id, groupId, roleId, ct) : await groups.DetachRoleAsync(auth.Org.Id, groupId, roleId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync($"org.groups.role.{(attach ? "add" : "remove")}", auth.Org.Id, auth.User!.Id, $"group.role.{(attach ? "add" : "remove")}", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static GroupResponse ToGroupResponse(Group group, int builtInRoleCount = 0)
        => new(group.Id, group.Name, group.Email, group.Description, group.Members.Count + group.Owners.Count, group.ServiceAccountMembers.Count + group.ServiceAccountOwners.Count, group.Roles.Count + builtInRoleCount);

    private static Dictionary<string, string[]> ValidateGroupInput(string? name, string? email, string? description, bool requireName)
    {
        var errors = new Dictionary<string, string[]>();
        if ((requireName && string.IsNullOrWhiteSpace(name)) || (name is not null && (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)))
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        if (email is not null && !IsValidEmail(email.Trim()))
        {
            errors["email"] = ["Email must be a valid email address and 320 characters or fewer when provided."];
        }

        if (description is not null && description.Length > 1024)
        {
            errors["description"] = ["Description must be 1024 characters or fewer when provided."];
        }

        return errors;
    }

    private static bool IsValidEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320)
        {
            return false;
        }

        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task<AuthContext> RequireCurrentOrgPermissionAsync(HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, PermissionKey permission, CancellationToken ct, bool allowServiceAccount = false)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, permission, ct, allowServiceAccount);
        return new AuthContext(auth.User, auth.ServiceAccountKey, auth.Org, auth.Failure);
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

    private static ParsedNamedListQuery ParseNamedListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterName, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "limit", "cursor", "filter[name]", "sort" };
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

        var resolvedSort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.Trim();
        if (resolvedSort is not "name" and not "-name")
        {
            errors["sort"] = ["Sort must be name or -name."];
            resolvedSort = "name";
        }

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !TryDecodeCursor(cursor, out offset))
        {
            errors["cursor"] = ["Cursor is invalid."];
        }

        if (filterName is { Length: > 160 })
        {
            errors["filter.name"] = ["Name filter must be 160 characters or fewer."];
        }

        return new ParsedNamedListQuery(resolvedLimit, offset, filterName, resolvedSort, errors);
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

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    private static IResult StepUpRequired(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "step_up_required", "Recent authentication is required for this operation.");

    private static IResult Unauthenticated(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");

    private static IResult Forbidden(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");
}
