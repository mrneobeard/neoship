using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization user management endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapUserEndpoints();
/// </code>
/// </example>
public static class UserEndpoints
{
    /// <summary>
    /// Maps current-organization user management endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var users = routes.MapGroup("/api/v1/users");
        users.MapGet("", ListUsersAsync);
        users.MapDelete("/{userId:guid}", DeleteUserAsync);
        users.MapGet("/{userId:guid}/roles", ListUserRolesAsync);
        users.MapPost("/{userId:guid}/roles", AddUserRoleAsync);
        users.MapDelete("/{userId:guid}/roles/{roleId:guid}", RemoveUserRoleAsync);
        users.MapGet("/{userId:guid}/groups", ListUserGroupsAsync);
        users.MapPost("/{userId:guid}/groups", AddUserGroupAsync);
        users.MapDelete("/{userId:guid}/groups/{groupId:guid}", RemoveUserGroupAsync);
        return routes;
    }

    private sealed record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl, string Status);

    private sealed record RoleResponse(Guid Id, string Key, string Name, string? Description, bool BuiltIn, bool Editable);

    private sealed record GroupResponse(Guid Id, string Name, string? Email, string? Description);

    private sealed record AddUserRoleRequest(Guid RoleId);

    private sealed record AddUserGroupRequest(Guid GroupId);

    private sealed record ParsedUserListQuery(int Limit, int Offset, string? FilterEmail, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

    private static async Task<IResult> ListUsersAsync(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[email]")] string? filterEmail,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.members", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseUserListQuery(httpContext, limit, cursor, filterEmail, filterName, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<User> filtered = await users.ListOrgUsersAsync(auth.Org.Id, ct);
        if (!string.IsNullOrWhiteSpace(query.FilterEmail))
        {
            filtered = filtered.Where(x => x.Email.Contains(query.FilterEmail, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.FilterName))
        {
            filtered = filtered.Where(x => x.Name.Contains(query.FilterName, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort switch
        {
            "name" => filtered.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            "-name" => filtered.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            "-email" => filtered.OrderByDescending(x => x.Email, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            _ => filtered.OrderBy(x => x.Email, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
        };

        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(ToUserResponse).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(query.FilterEmail)) filters["email"] = query.FilterEmail;
        if (!string.IsNullOrWhiteSpace(query.FilterName)) filters["name"] = query.FilterName;
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<UserResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> DeleteUserAsync(Guid userId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.members", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!await users.SoftDeleteOrgUserAsync(auth.Org.Id, userId, ct))
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.users.delete", auth.Org.Id, auth.User!.Id, "user.delete", targetType: "user", targetId: userId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListUserRolesAsync(Guid userId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!await ActiveUserExistsAsync(db, auth.Org.Id, userId, ct))
        {
            return NotFoundError(httpContext);
        }

        var customRoles = await db.Roles
            .Include(x => x.Users)
            .Where(x => x.OrgId == auth.Org.Id && x.Users.Any(u => u.Id == userId))
            .Select(x => new RoleResponse(x.Id, x.Name.ToLowerInvariant(), x.Name, x.Description, false, true))
            .ToListAsync(ct);
        var builtInRoleKeys = await db.RoleAssignments
            .Where(x => x.OrgId == auth.Org.Id && x.UserId == userId && x.ScopeKind == PermissionScopeKind.Organization && x.ScopeId == auth.Org.Slug)
            .Select(x => x.RoleKey)
            .ToListAsync(ct);
        var builtInRoles = BuiltInRoleStore.Definitions()
            .Where(x => builtInRoleKeys.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
            .Select(x => new RoleResponse(x.Id, x.Key, x.Name, x.Description, true, false));
        var data = builtInRoles.Concat(customRoles).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return TypedResults.Ok(new ApiCollectionEnvelope<RoleResponse>(data, EmptyPagination(data.Count), ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> AddUserRoleAsync(Guid userId, [FromBody] AddUserRoleRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, RoleStore roles, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeUserRoleAsync(userId, req.RoleId, add: true, httpContext, sessions, permissions, roles, db, audit, ct);

    private static async Task<IResult> RemoveUserRoleAsync(Guid userId, Guid roleId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, RoleStore roles, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeUserRoleAsync(userId, roleId, add: false, httpContext, sessions, permissions, roles, db, audit, ct);

    private static async Task<IResult> ChangeUserRoleAsync(Guid userId, Guid roleId, bool add, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, RoleStore roles, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!await ActiveUserExistsAsync(db, auth.Org.Id, userId, ct))
        {
            return NotFoundError(httpContext);
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        var ok = false;
        if (builtInRole is not null)
        {
            if (add)
            {
                await BuiltInRoleStore.AssignAsync(db, auth.Org.Id, auth.Org.Slug, userId, builtInRole.Key, auth.User!.Id, ct);
                ok = true;
            }
            else
            {
                ok = await BuiltInRoleStore.UnassignAsync(db, auth.Org.Id, auth.Org.Slug, userId, builtInRole.Key, ct);
            }
        }
        else
        {
            ok = add ? await roles.AttachUserAsync(auth.Org.Id, roleId, userId, ct) : await roles.DetachUserAsync(auth.Org.Id, roleId, userId, ct);
        }

        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync(add ? "org.users.roles.add" : "org.users.roles.remove", auth.Org.Id, auth.User!.Id, add ? "user.role.add" : "user.role.remove", targetType: "user", targetId: userId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListUserGroupsAsync(Guid userId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!await ActiveUserExistsAsync(db, auth.Org.Id, userId, ct))
        {
            return NotFoundError(httpContext);
        }

        var data = await db.Groups
            .Include(x => x.Members)
            .Where(x => x.OrgId == auth.Org.Id && x.Members.Any(u => u.Id == userId))
            .OrderBy(x => x.NameUpcase)
            .Select(x => new GroupResponse(x.Id, x.Name, x.Email, x.Description))
            .ToListAsync(ct);
        return TypedResults.Ok(new ApiCollectionEnvelope<GroupResponse>(data, EmptyPagination(data.Count), ApiMeta.FromHttpContext(httpContext)));
    }

    private static async Task<IResult> AddUserGroupAsync(Guid userId, [FromBody] AddUserGroupRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeUserGroupAsync(userId, req.GroupId, add: true, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> RemoveUserGroupAsync(Guid userId, Guid groupId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
        => await ChangeUserGroupAsync(userId, groupId, add: false, httpContext, sessions, permissions, groups, db, audit, ct);

    private static async Task<IResult> ChangeUserGroupAsync(Guid userId, Guid groupId, bool add, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, GroupStore groups, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!await ActiveUserExistsAsync(db, auth.Org.Id, userId, ct))
        {
            return NotFoundError(httpContext);
        }

        var ok = add ? await groups.AddUserAsync(auth.Org.Id, groupId, userId, ct) : await groups.RemoveUserAsync(auth.Org.Id, groupId, userId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync(add ? "org.users.groups.add" : "org.users.groups.remove", auth.Org.Id, auth.User!.Id, add ? "user.group.add" : "user.group.remove", targetType: "user", targetId: userId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static ParsedUserListQuery ParseUserListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterEmail, string? filterName, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "limit", "cursor", "filter[email]", "filter[name]", "sort" };
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

        var resolvedSort = string.IsNullOrWhiteSpace(sort) ? "email" : sort.Trim();
        if (resolvedSort is not ("email" or "-email" or "name" or "-name"))
        {
            errors["sort"] = ["Sort must be email, -email, name, or -name."];
            resolvedSort = "email";
        }

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !TryDecodeCursor(cursor, out offset))
        {
            errors["cursor"] = ["Cursor is invalid."];
        }

        if (filterEmail is { Length: > 320 }) errors["filter.email"] = ["Email filter must be 320 characters or fewer."];
        if (filterName is { Length: > 256 }) errors["filter.name"] = ["Name filter must be 256 characters or fewer."];
        return new ParsedUserListQuery(resolvedLimit, offset, filterEmail, filterName, resolvedSort, errors);
    }

    private static async Task<bool> ActiveUserExistsAsync(ShipDb db, Guid orgId, Guid userId, CancellationToken ct)
        => await db.Users.AnyAsync(x => x.Id == userId && x.OrgId == orgId && x.DeletedAt == null && x.StatusId != UserStatus.Deleted.Id, ct);

    private static UserResponse ToUserResponse(User user)
        => new(user.Id, user.Email, user.Name, user.AvatarUrl, user.Status.Name);

    private static ApiPagination EmptyPagination(int count)
        => new(count, nextCursor: null, previousCursor: null, hasMore: false);

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    private static IResult ValidationError(HttpContext httpContext, IReadOnlyDictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static bool TryDecodeCursor(string cursor, out int offset)
    {
        offset = 0;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out offset) && offset >= 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
