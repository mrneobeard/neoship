using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

using static NeoShip.ApiSvc.Endpoints.EndpointResults;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization role and role-assignment endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapRoleEndpoints();
/// </code>
/// </example>
public static class RoleEndpoints
{
    private const double DefaultStepUpWindowMinutes = 15;

    /// <summary>
    /// Maps current-organization role endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder routes)
    {
        var roles = routes.MapGroup("/api/v1/roles");
        roles.MapGet("", ListRolesAsync);
        roles.MapPost("", CreateRoleAsync);
        roles.MapGet("/{roleId:guid}", GetRoleAsync);
        roles.MapPatch("/{roleId:guid}", UpdateRoleAsync);
        roles.MapDelete("/{roleId:guid}", DeleteRoleAsync);
        roles.MapPost("/{roleId:guid}/claims", AddRoleClaimAsync);
        roles.MapDelete("/{roleId:guid}/claims/{claimId:long}", RemoveRoleClaimAsync);

        var assignments = routes.MapGroup("/api/v1/role-assignments");
        assignments.MapPost("", AssignRoleAsync);
        assignments.MapDelete("/{principalType}/{principalId:guid}/roles/{roleId:guid}", UnassignRoleAsync);
        return routes;
    }

    /// <summary>
    /// Represents a claim summary on a role.
    /// </summary>
    public sealed record RoleClaimResponse(long Id, string Type, string Value);

    /// <summary>
    /// Represents a role response.
    /// </summary>
    public sealed record RoleResponse(Guid Id, string Key, string Name, string? Description, bool BuiltIn, bool Editable, List<RoleClaimResponse> Claims);

    /// <summary>
    /// Represents a role creation request.
    /// </summary>
    public sealed record CreateRoleRequest(string Name, string? Description);

    /// <summary>
    /// Represents a role update request.
    /// </summary>
    public sealed record UpdateRoleRequest(string? Name, string? Description);

    /// <summary>
    /// Represents a role permission grant request.
    /// </summary>
    public sealed record AddRoleClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    /// <summary>
    /// Represents a role assignment request.
    /// </summary>
    public sealed record RoleAssignmentRequest(Guid RoleId, string PrincipalType, Guid PrincipalId);

    private sealed record AuthContext(User? User, ServiceAccountApiKey? ServiceAccountKey, Organization Org, IResult? Failure);

    private sealed record RoleListEntry(RoleResponse Response, DateTime CreatedAt);

    private static async Task<IResult> ListRolesAsync(
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        var builtInRoles = BuiltInRoleStore.Definitions()
            .Select(x => new RoleListEntry(ToBuiltInRoleResponse(x, auth.Org.Slug), DateTime.MinValue));
        var customRoles = (await roles.ListAsync(auth.Org.Id, ct))
            .Select(x => new RoleListEntry(ToRoleResponse(x), x.CreatedAt));
        IEnumerable<RoleListEntry> filtered = builtInRoles.Concat(customRoles);
        if (!string.IsNullOrWhiteSpace(query.FilterName))
        {
            filtered = filtered.Where(x => x.Response.Name.Contains(query.FilterName, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort switch
        {
            "-name" => filtered.OrderByDescending(x => x.Response.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Response.Id),
            "createdAt" => filtered.OrderBy(x => x.CreatedAt).ThenBy(x => x.Response.Id),
            "-createdAt" => filtered.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Response.Id),
            _ => filtered.OrderBy(x => x.Response.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Response.Id),
        };

        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(x => x.Response).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<RoleResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateRoleAsync(
        [FromBody] CreateRoleRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleInput(req.Name, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var role = await roles.CreateAsync(auth.Org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.roles.create", auth.Org.Id, auth.User.Id, "role.create", targetType: "role", targetId: role.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/roles/{role.Id}", Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            return TypedResults.Ok(Envelope(httpContext, ToBuiltInRoleResponse(builtInRole, auth.Org.Slug)));
        }

        var role = await roles.GetAsync(auth.Org.Id, roleId, ct);
        return role is null ? NotFoundError(httpContext) : TypedResults.Ok(Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        [FromBody] UpdateRoleRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleInput(req.Name, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var role = await roles.UpdateAsync(auth.Org.Id, roleId, req.Name, req.Description, ct);
        if (role is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.update", auth.Org.Id, auth.User!.Id, "role.update", targetType: "role", targetId: role.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> DeleteRoleAsync(
        Guid roleId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        if (!await roles.DeleteAsync(auth.Org.Id, roleId, ct))
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.delete", auth.Org.Id, auth.User!.Id, "role.delete", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> AddRoleClaimAsync(
        Guid roleId,
        [FromBody] AddRoleClaimRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleClaim(req, out var key);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var added = await roles.AddClaimAsync(auth.Org.Id, roleId, new PermissionGrant(key, req.ScopeKind, req.ScopeId), auth.User!.Id, ct);
        if (!added)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.claim.add", auth.Org.Id, auth.User.Id, "role.claim.add", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> RemoveRoleClaimAsync(
        Guid roleId,
        long claimId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        if (!await roles.RemoveClaimAsync(auth.Org.Id, roleId, claimId, ct))
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.claim.remove", auth.Org.Id, auth.User!.Id, "role.claim.remove", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> AssignRoleAsync(
        [FromBody] RoleAssignmentRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleAssignment(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var ok = await AssignRoleToPrincipalAsync(req, auth.Org, auth.User!.Id, roles, groups, db, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.role_assignments.create", auth.Org.Id, auth.User.Id, "role_assignment.create", targetType: "role", targetId: req.RoleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> UnassignRoleAsync(
        string principalType,
        Guid principalId,
        Guid roleId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var req = new RoleAssignmentRequest(roleId, principalType, principalId);
        var validation = ValidateRoleAssignment(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var ok = await UnassignRoleFromPrincipalAsync(req, auth.Org, roles, groups, db, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.role_assignments.delete", auth.Org.Id, auth.User!.Id, "role_assignment.delete", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<bool> AssignRoleToPrincipalAsync(RoleAssignmentRequest req, Organization org, Guid actorUserId, RoleStore roles, GroupStore groups, ShipDb db, CancellationToken ct)
    {
        var builtInRole = BuiltInRoleStore.FindById(req.RoleId);
        if (string.Equals(req.PrincipalType, "user", StringComparison.OrdinalIgnoreCase))
        {
            if (builtInRole is not null)
            {
                await BuiltInRoleStore.AssignAsync(db, org.Id, org.Slug, req.PrincipalId, builtInRole.Key, actorUserId, ct);
                return await db.Users.AnyAsync(x => x.Id == req.PrincipalId && x.OrgId == org.Id, ct);
            }

            return await roles.AttachUserAsync(org.Id, req.RoleId, req.PrincipalId, ct);
        }

        if (builtInRole is not null)
        {
            return await BuiltInRoleStore.AssignGroupAsync(db, org.Id, org.Slug, req.PrincipalId, builtInRole.Key, actorUserId, ct);
        }

        return await groups.AttachRoleAsync(org.Id, req.PrincipalId, req.RoleId, ct);
    }

    private static async Task<bool> UnassignRoleFromPrincipalAsync(RoleAssignmentRequest req, Organization org, RoleStore roles, GroupStore groups, ShipDb db, CancellationToken ct)
    {
        var builtInRole = BuiltInRoleStore.FindById(req.RoleId);
        if (string.Equals(req.PrincipalType, "user", StringComparison.OrdinalIgnoreCase))
        {
            return builtInRole is not null
                ? await BuiltInRoleStore.UnassignAsync(db, org.Id, org.Slug, req.PrincipalId, builtInRole.Key, ct)
                : await roles.DetachUserAsync(org.Id, req.RoleId, req.PrincipalId, ct);
        }

        return builtInRole is not null
            ? await BuiltInRoleStore.UnassignGroupAsync(db, org.Id, org.Slug, req.PrincipalId, builtInRole.Key, ct)
            : await groups.DetachRoleAsync(org.Id, req.PrincipalId, req.RoleId, ct);
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

    private static async Task<ServiceAccountApiKey?> AuthenticateServiceAccountApiKeyAsync(HttpContext httpContext, CancellationToken ct)
    {
        var token = ReadBearerToken(httpContext.Request);
        return token is null ? null : await httpContext.RequestServices.GetRequiredService<ServiceAccountStore>().AuthenticateApiKeyAsync(token, ct);
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

    private static RoleResponse ToRoleResponse(Role role)
        => new(role.Id, role.Name.ToLowerInvariant(), role.Name, role.Description, false, true, role.Claims.Select(c => new RoleClaimResponse(c.Id, c.Type, c.Value)).ToList());

    private static RoleResponse ToBuiltInRoleResponse(BuiltInRoleDefinition role, string orgSlug)
        => new(
            role.Id,
            role.Key,
            role.Name,
            role.Description,
            true,
            false,
            BuiltInRoleStore.GrantsFor(role.Key, PermissionScopeKind.Organization, orgSlug)
                .Select((grant, index) => new RoleClaimResponse(-(index + 1), grant.Key.ToString(), grant.ScopeId is null ? grant.ScopeKind.ToString().ToLowerInvariant() : $"{grant.ScopeKind.ToString().ToLowerInvariant()}:{grant.ScopeId}"))
                .ToList());

    private static Dictionary<string, string[]> ValidateRoleInput(string? name, string? description, bool requireName)
    {
        var errors = new Dictionary<string, string[]>();
        if ((requireName && string.IsNullOrWhiteSpace(name)) || (name is not null && (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)))
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }
        else if (name is not null && BuiltInRoleStore.IsBuiltInRoleName(name))
        {
            errors["name"] = ["Name is reserved for a built-in role."];
        }

        if (description is not null && description.Length > 1024)
        {
            errors["description"] = ["Description must be 1024 characters or fewer when provided."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRoleClaim(AddRoleClaimRequest req, out PermissionKey key)
    {
        var errors = new Dictionary<string, string[]>();
        if (!PermissionKey.TryParse(req.Permission, out key))
        {
            errors["permission"] = ["Permission must be a registered resource.action key."];
        }

        if (!Enum.IsDefined(req.ScopeKind))
        {
            errors["scopeKind"] = ["Scope kind is invalid."];
        }

        if (req.ScopeId is not null && req.ScopeId.Length > 160)
        {
            errors["scopeId"] = ["Scope ID must be 160 characters or fewer when provided."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRoleAssignment(RoleAssignmentRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (req.RoleId == Guid.Empty)
        {
            errors["roleId"] = ["Role ID is required."];
        }

        if (req.PrincipalId == Guid.Empty)
        {
            errors["principalId"] = ["Principal ID is required."];
        }

        if (!string.Equals(req.PrincipalType, "user", StringComparison.OrdinalIgnoreCase) && !string.Equals(req.PrincipalType, "group", StringComparison.OrdinalIgnoreCase))
        {
            errors["principalType"] = ["Principal type must be user or group."];
        }

        return errors;
    }

    private sealed record ParsedNamedListQuery(int Limit, int Offset, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

    private static ParsedNamedListQuery ParseNamedListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterName, string? sort, bool allowCreatedAtSort)
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
        var sortIsValid = resolvedSort is "name" or "-name" || (allowCreatedAtSort && resolvedSort is "createdAt" or "-createdAt");
        if (!sortIsValid)
        {
            errors["sort"] = [allowCreatedAtSort ? "Sort must be name, -name, createdAt, or -createdAt." : "Sort must be name or -name."];
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

    private static IResult StepUpRequired(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "step_up_required", "Recent authentication is required for this operation.");

    private static IResult Unauthenticated(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");

    private static IResult Forbidden(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");

    private static IResult NotFoundError(HttpContext httpContext)
        => NotFound(httpContext);

    private static IResult BuiltInRoleConflict(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status409Conflict, "conflict", "Built-in roles are code-owned and cannot be modified.");
}
