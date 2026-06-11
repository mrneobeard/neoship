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

        var invites = users.MapGroup("/invites");
        invites.MapGet("", ListInvitesAsync);
        invites.MapPost("", CreateInviteAsync);
        invites.MapPost("/{inviteId:guid}/revoke", RevokeInviteAsync);

        var identityProviders = users.MapGroup("/identity-providers");
        identityProviders.MapGet("", ListIdentityProvidersAsync);
        identityProviders.MapPost("", CreateIdentityProviderAsync);
        identityProviders.MapGet("/{providerId:long}", GetIdentityProviderAsync);
        identityProviders.MapPatch("/{providerId:long}", UpdateIdentityProviderAsync);
        identityProviders.MapPost("/{providerId:long}/enable", EnableIdentityProviderAsync);
        identityProviders.MapPost("/{providerId:long}/disable", DisableIdentityProviderAsync);
        return routes;
    }

    private sealed record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl, string Status);

    private sealed record RoleResponse(Guid Id, string Key, string Name, string? Description, bool BuiltIn, bool Editable);

    private sealed record GroupResponse(Guid Id, string Name, string? Email, string? Description);

    private sealed record AddUserRoleRequest(Guid RoleId);

    private sealed record AddUserGroupRequest(Guid GroupId);

    private sealed record ParsedUserListQuery(int Limit, int Offset, string? FilterEmail, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

    private sealed record OrganizationInviteResponse(Guid Id, string Email, DateTime CreatedAt, DateTime ExpiresAt, DateTime? AcceptedAt, DateTime? RevokedAt);

    private sealed record CreateOrganizationInviteResponse(Guid Id, string Email, string Token, DateTime CreatedAt, DateTime ExpiresAt);

    private sealed record CreateOrganizationInviteRequest(string Email, List<Guid>? RoleIds, List<Guid>? GroupIds);

    private sealed record ParsedEmailListQuery(int Limit, int Offset, string? FilterEmail, string Sort, Dictionary<string, string[]> Errors);

    private sealed record IdentityProviderResponse(long Id, string Name, string ProviderType, string Status, string? IssuerUrl, string? ClientId, bool HasClientSecret, string? MetadataJson, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record CreateIdentityProviderRequest(string Name, string? ProviderType, string? Preset, string? IssuerUrl, string? ClientId, string? ClientSecret, string? MetadataJson);

    private sealed record UpdateIdentityProviderRequest(string? Name, string? IssuerUrl, string? ClientId, string? ClientSecret, string? MetadataJson);

    private sealed record IdentityProviderPreset(UserIdentityProviderType ProviderType, string? IssuerUrl, string MetadataJson);

    private sealed record ParsedNamedListQuery(int Limit, int Offset, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

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
            return NotFound(httpContext);
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
            return NotFound(httpContext);
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
            return NotFound(httpContext);
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
            return NotFound(httpContext);
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
            return NotFound(httpContext);
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
            return NotFound(httpContext);
        }

        var ok = add ? await groups.AddUserAsync(auth.Org.Id, groupId, userId, ct) : await groups.RemoveUserAsync(auth.Org.Id, groupId, userId, ct);
        if (!ok)
        {
            return NotFound(httpContext);
        }

        await audit.RecordAsync(add ? "org.users.groups.add" : "org.users.groups.remove", auth.Org.Id, auth.User!.Id, add ? "user.group.add" : "user.group.remove", targetType: "user", targetId: userId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

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
        return TypedResults.Created($"/api/v1/users/invites/{invite.Id}", Envelope(httpContext, new CreateOrganizationInviteResponse(invite.Id, invite.Email, token, invite.CreatedAt, invite.ExpiresAt)));
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
            return NotFound(httpContext);
        }

        await audit.RecordAsync("org.invites.revoke", auth.Org.Id, auth.User!.Id, "org.invite.revoke", targetType: "organization_invite", targetId: inviteId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListIdentityProvidersAsync([FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[name]")] string? filterName, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<UserIdentityProvider> filtered = await users.ListIdentityProvidersAsync(auth.Org.Id, ct);
        if (!string.IsNullOrWhiteSpace(query.FilterName))
        {
            filtered = filtered.Where(x => x.Name.Contains(query.FilterName, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort switch
        {
            "-name" => filtered.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
            "createdAt" => filtered.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            "-createdAt" => filtered.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            _ => filtered.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id),
        };

        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(ToIdentityProviderResponse).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<IdentityProviderResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateIdentityProviderAsync([FromBody] CreateIdentityProviderRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateCreateIdentityProvider(req, out var providerType, out var preset);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var provider = await users.CreateIdentityProviderAsync(auth.Org.Id, auth.User!.Id, req.Name, providerType, req.IssuerUrl ?? preset?.IssuerUrl, req.ClientId, req.ClientSecret, req.MetadataJson ?? preset?.MetadataJson, ct);
        await audit.RecordAsync("org.identity_providers.create", auth.Org.Id, auth.User.Id, "identity_provider.create", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/users/identity-providers/{provider.Id}", Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> GetIdentityProviderAsync(long providerId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var provider = await users.GetIdentityProviderAsync(auth.Org.Id, providerId, ct);
        return provider is null ? NotFound(httpContext) : TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> UpdateIdentityProviderAsync(long providerId, [FromBody] UpdateIdentityProviderRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateUpdateIdentityProvider(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var provider = await users.UpdateIdentityProviderAsync(auth.Org.Id, providerId, req.Name, req.IssuerUrl, req.ClientId, req.ClientSecret, req.MetadataJson, ct);
        if (provider is null)
        {
            return NotFound(httpContext);
        }

        await audit.RecordAsync("org.identity_providers.update", auth.Org.Id, auth.User!.Id, "identity_provider.update", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> EnableIdentityProviderAsync(long providerId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
        => await SetIdentityProviderActiveAsync(providerId, active: true, httpContext, sessions, permissions, db, users, audit, ct);

    private static async Task<IResult> DisableIdentityProviderAsync(long providerId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
        => await SetIdentityProviderActiveAsync(providerId, active: false, httpContext, sessions, permissions, db, users, audit, ct);

    private static async Task<IResult> SetIdentityProviderActiveAsync(long providerId, bool active, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        UserIdentityProvider? provider;
        try
        {
            provider = await users.SetIdentityProviderActiveAsync(auth.Org.Id, providerId, active, ct);
        }
        catch (ArgumentException)
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "invalid_identity_provider", "Invalid identity provider request.");
        }

        if (provider is null)
        {
            return NotFound(httpContext);
        }

        await audit.RecordAsync($"org.identity_providers.{(active ? "enable" : "disable")}", auth.Org.Id, auth.User!.Id, $"identity_provider.{(active ? "enable" : "disable")}", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
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

    private static OrganizationInviteResponse ToInviteResponse(OrganizationInvite invite)
        => new(invite.Id, invite.Email, invite.CreatedAt, invite.ExpiresAt, invite.AcceptedAt, invite.RevokedAt);

    private static IdentityProviderResponse ToIdentityProviderResponse(UserIdentityProvider provider)
        => new(provider.Id, provider.Name, provider.ProviderType.Name, provider.Status.Name, provider.IssuerUrl, provider.ClientId, provider.ClientSecretEncrypted.Length > 0, provider.MetadataJson, provider.CreatedAt, provider.UpdatedAt);

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

    private static Dictionary<string, string[]> ValidateCreateIdentityProvider(CreateIdentityProviderRequest req, out UserIdentityProviderType providerType, out IdentityProviderPreset? preset)
    {
        var errors = new Dictionary<string, string[]>();
        providerType = UserIdentityProviderType.Unknown;
        preset = null;

        if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 160)
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(req.Preset))
        {
            if (!TryGetIdentityProviderPreset(req.Preset, out var parsedPreset))
            {
                errors["preset"] = ["Preset must be github, google, or microsoft when provided."];
            }
            else
            {
                preset = parsedPreset;
                providerType = parsedPreset.ProviderType;
            }
        }

        if (string.IsNullOrWhiteSpace(req.ProviderType) && preset is null)
        {
            errors["providerType"] = ["Provider type must be oidc, oauth2, or saml."];
        }
        else if (!string.IsNullOrWhiteSpace(req.ProviderType))
        {
            if (!TryParseProviderType(req.ProviderType, out var parsedProviderType))
            {
                errors["providerType"] = ["Provider type must be oidc, oauth2, or saml."];
            }
            else if (preset is not null && parsedProviderType.Id != preset.ProviderType.Id)
            {
                errors["providerType"] = ["Provider type must match the selected preset."];
            }
            else
            {
                providerType = parsedProviderType;
            }
        }

        AddIdentityProviderCommonErrors(errors, req.IssuerUrl ?? preset?.IssuerUrl, req.ClientId, req.ClientSecret, req.MetadataJson ?? preset?.MetadataJson);
        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateIdentityProvider(UpdateIdentityProviderRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (req.Name is not null && (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 160))
        {
            errors["name"] = ["Name must be 160 characters or fewer when provided."];
        }

        AddIdentityProviderCommonErrors(errors, req.IssuerUrl, req.ClientId, req.ClientSecret, req.MetadataJson);
        return errors;
    }

    private static void AddIdentityProviderCommonErrors(Dictionary<string, string[]> errors, string? issuerUrl, string? clientId, string? clientSecret, string? metadataJson)
    {
        if (!IsValidIssuerUrl(issuerUrl))
        {
            errors["issuerUrl"] = ["Issuer URL must be an HTTPS URL when provided."];
        }

        if (clientId is not null && (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 512))
        {
            errors["clientId"] = ["Client ID must be 512 characters or fewer when provided."];
        }

        if (clientSecret is not null && clientSecret.Length > 4096)
        {
            errors["clientSecret"] = ["Client secret must be 4096 characters or fewer when provided."];
        }

        if (!IsValidMetadataJson(metadataJson))
        {
            errors["metadataJson"] = ["Metadata JSON must be a valid JSON object when provided."];
        }
    }

    private static ParsedEmailListQuery ParseEmailListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterEmail, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "limit", "cursor", "filter[email]", "sort" };
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

    private static ParsedNamedListQuery ParseNamedListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterName, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "limit", "cursor", "filter[name]", "sort" };
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
        if (resolvedSort is not ("name" or "-name" or "createdAt" or "-createdAt"))
        {
            errors["sort"] = ["Sort must be name, -name, createdAt, or -createdAt."];
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

    private static bool IsValidEmail(string email)
    {
        if (email.Length is < 3 or > 320)
        {
            return false;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1 && email[(at + 1)..].Contains('.', StringComparison.Ordinal);
    }

    private static bool TryParseProviderType(string value, out UserIdentityProviderType providerType)
    {
        providerType = value.Trim().ToLowerInvariant() switch
        {
            "oidc" => UserIdentityProviderType.OIDC,
            "saml" => UserIdentityProviderType.SAML,
            "oauth2" => UserIdentityProviderType.OAUTH2,
            _ => UserIdentityProviderType.Unknown,
        };
        return providerType.Id != UserIdentityProviderType.Unknown.Id;
    }

    private static bool TryGetIdentityProviderPreset(string? value, out IdentityProviderPreset preset)
    {
        preset = value?.Trim().ToLowerInvariant() switch
        {
            "github" => new IdentityProviderPreset(UserIdentityProviderType.OAUTH2, null, "{\"authorization_endpoint\":\"https://github.com/login/oauth/authorize\",\"token_endpoint\":\"https://github.com/login/oauth/access_token\",\"user_endpoint\":\"https://api.github.com/user\",\"email_endpoint\":\"https://api.github.com/user/emails\",\"default_scopes\":[\"read:user\",\"user:email\"]}"),
            "google" => new IdentityProviderPreset(UserIdentityProviderType.OIDC, "https://accounts.google.com", "{\"authorization_endpoint\":\"https://accounts.google.com/o/oauth2/v2/auth\",\"token_endpoint\":\"https://oauth2.googleapis.com/token\",\"default_scopes\":[\"openid\",\"email\",\"profile\"]}"),
            "microsoft" => new IdentityProviderPreset(UserIdentityProviderType.OIDC, "https://login.microsoftonline.com/common/v2.0", "{\"authorization_endpoint\":\"https://login.microsoftonline.com/common/oauth2/v2.0/authorize\",\"token_endpoint\":\"https://login.microsoftonline.com/common/oauth2/v2.0/token\",\"default_scopes\":[\"openid\",\"email\",\"profile\"]}"),
            _ => new IdentityProviderPreset(UserIdentityProviderType.Unknown, null, string.Empty),
        };
        return preset.ProviderType.Id != UserIdentityProviderType.Unknown.Id;
    }

    private static bool IsValidIssuerUrl(string? issuerUrl)
        => issuerUrl is null || (Uri.TryCreate(issuerUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);

    private static bool IsValidMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ApiPagination EmptyPagination(int count)
        => new(count, nextCursor: null, previousCursor: null, hasMore: false);

    private static IResult ValidationError(HttpContext httpContext, IReadOnlyDictionary<string, string[]> fields)
        => EndpointResults.ValidationError(httpContext, fields);
}
