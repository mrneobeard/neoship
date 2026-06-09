using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class OrgEndpoints
{
    public static RouteGroupBuilder MapOrgEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}");

        group.MapGet("/roles", ListRolesAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapPost("/roles/{roleId:guid}/claims", AddRoleClaimAsync);
        group.MapDelete("/roles/{roleId:guid}/claims/{claimId:ulong}", RemoveRoleClaimAsync);

        group.MapGet("/groups", ListGroupsAsync);
        group.MapPost("/groups", CreateGroupAsync);
        group.MapPost("/groups/{groupId:guid}/members/users/{userId:guid}", AddGroupUserAsync);
        group.MapDelete("/groups/{groupId:guid}/members/users/{userId:guid}", RemoveGroupUserAsync);
        group.MapPost("/groups/{groupId:guid}/members/service-accounts/{serviceAccountId:guid}", AddGroupServiceAccountAsync);
        group.MapDelete("/groups/{groupId:guid}/members/service-accounts/{serviceAccountId:guid}", RemoveGroupServiceAccountAsync);
        group.MapPost("/groups/{groupId:guid}/roles/{roleId:guid}", AttachGroupRoleAsync);
        group.MapDelete("/groups/{groupId:guid}/roles/{roleId:guid}", DetachGroupRoleAsync);

        group.MapGet("/service-accounts", ListServiceAccountsAsync);
        group.MapPost("/service-accounts", CreateServiceAccountAsync);
        group.MapPatch("/service-accounts/{serviceAccountId:guid}", UpdateServiceAccountAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/disable", DisableServiceAccountAsync);

        group.MapGet("/service-accounts/{serviceAccountId:guid}/api-keys", ListServiceAccountApiKeysAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys", CreateServiceAccountApiKeyAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/revoke", RevokeServiceAccountApiKeyAsync);

        return group;
    }

    /// <summary>
    /// Represents a claim summary on a role.
    /// </summary>
    public record RoleClaimResponse(ulong Id, string Type, string Value);

    /// <summary>
    /// Represents a role response.
    /// </summary>
    public record RoleResponse(Guid Id, string Name, string? Description, List<RoleClaimResponse> Claims);

    /// <summary>
    /// Represents a role creation request.
    /// </summary>
    public record CreateRoleRequest(string Name, string? Description);

    /// <summary>
    /// Represents a role permission grant request.
    /// </summary>
    public record AddRoleClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    /// <summary>
    /// Represents a group response.
    /// </summary>
    public record GroupResponse(Guid Id, string Name, string? Email, string? Description, int MemberCount, int ServiceAccountMemberCount, int RoleCount);

    /// <summary>
    /// Represents a group creation request.
    /// </summary>
    public record CreateGroupRequest(string Name, string? Email, string? Description);

    private static async Task<Organization?> ResolveOrgAsync(string orgSlug, ShipDb db, CancellationToken ct)
    {
        return await db.Orgs.FirstOrDefaultAsync(o => o.Slug == orgSlug, ct);
    }

    private static async Task<(User? User, IResult? Failure)> RequireOrgPermissionAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        string orgSlug,
        PermissionKey permission,
        CancellationToken ct)
    {
        var user = await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return (null, TypedResults.Unauthorized());
        }

        var allowed = await permissions.UserHasAsync(user.Id, permission, PermissionScopeKind.Organization, orgSlug, ct);
        if (!allowed)
        {
            return (null, TypedResults.Forbid());
        }

        return (user, null);
    }

    private static RoleResponse ToRoleResponse(Role role)
        => new(
            role.Id,
            role.Name,
            role.Description,
            role.Claims.Select(c => new RoleClaimResponse(c.Id, c.Type, c.Value)).ToList());

    private static GroupResponse ToGroupResponse(Group group)
        => new(group.Id, group.Name, group.Email, group.Description, group.Members.Count, group.ServiceAccountMembers.Count, group.Roles.Count);

    private static async Task<IResult> ListRolesAsync(
        string orgSlug,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var list = await roles.ListAsync(org.Id, ct);
        return TypedResults.Ok(list.Select(ToRoleResponse).ToList());
    }

    private static async Task<IResult> CreateRoleAsync(
        string orgSlug,
        [FromBody] CreateRoleRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var role = await roles.CreateAsync(org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.roles.create", org.Id, auth.User.Id, "role.create", targetType: "role", targetId: role.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/roles/{role.Id}", ToRoleResponse(role));
    }

    private static async Task<IResult> AddRoleClaimAsync(
        string orgSlug,
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
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!PermissionKey.TryParse(req.Permission, out var key))
        {
            return TypedResults.BadRequest("Invalid permission key.");
        }

        var grant = new PermissionGrant(key, req.ScopeKind, req.ScopeId);
        var added = await roles.AddClaimAsync(org.Id, roleId, grant, auth.User!.Id, ct);
        if (!added)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.roles.claim.add", org.Id, auth.User.Id, "role.claim.add", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> RemoveRoleClaimAsync(
        string orgSlug,
        Guid roleId,
        ulong claimId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        RoleStore roles,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var removed = await roles.RemoveClaimAsync(org.Id, roleId, claimId, ct);
        if (!removed)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.roles.claim.remove", org.Id, auth.User!.Id, "role.claim.remove", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> ListGroupsAsync(
        string orgSlug,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var list = await groups.ListAsync(org.Id, ct);
        return TypedResults.Ok(list.Select(ToGroupResponse).ToList());
    }

    private static async Task<IResult> CreateGroupAsync(
        string orgSlug,
        [FromBody] CreateGroupRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var group = await groups.CreateAsync(org.Id, req.Name, req.Email, req.Description, ct);
        await audit.RecordAsync("org.groups.create", org.Id, auth.User!.Id, "group.create", targetType: "group", targetId: group.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/groups/{group.Id}", ToGroupResponse(group));
    }

    private static async Task<IResult> AddGroupUserAsync(
        string orgSlug,
        Guid groupId,
        Guid userId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.AddUserAsync(org.Id, groupId, userId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.member.add", org.Id, auth.User!.Id, "group.member.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> RemoveGroupUserAsync(
        string orgSlug,
        Guid groupId,
        Guid userId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.RemoveUserAsync(org.Id, groupId, userId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.member.remove", org.Id, auth.User!.Id, "group.member.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> AddGroupServiceAccountAsync(
        string orgSlug,
        Guid groupId,
        Guid serviceAccountId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.AddServiceAccountAsync(org.Id, groupId, serviceAccountId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.service_account.add", org.Id, auth.User!.Id, "group.service_account.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> RemoveGroupServiceAccountAsync(
        string orgSlug,
        Guid groupId,
        Guid serviceAccountId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.RemoveServiceAccountAsync(org.Id, groupId, serviceAccountId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.service_account.remove", org.Id, auth.User!.Id, "group.service_account.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> AttachGroupRoleAsync(
        string orgSlug,
        Guid groupId,
        Guid roleId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.AttachRoleAsync(org.Id, groupId, roleId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.role.add", org.Id, auth.User!.Id, "group.role.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DetachGroupRoleAsync(
        string orgSlug,
        Guid groupId,
        Guid roleId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        GroupStore groups,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.DetachRoleAsync(org.Id, groupId, roleId, ct);
        if (!ok)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.groups.role.remove", org.Id, auth.User!.Id, "group.role.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    public record ServiceAccountResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);

    private static async Task<IResult> ListServiceAccountsAsync(
        string orgSlug,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var accounts = await store.ListAsync(org.Id, ct);

        var result = accounts.Select(s => new ServiceAccountResponse(
            s.Id, s.Name, s.Description, s.CreatedAt, s.UpdatedAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    public record CreateServiceAccountRequest(string Name, string? Description);

    private static async Task<IResult> CreateServiceAccountAsync(
        string orgSlug,
        [FromBody] CreateServiceAccountRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.CreateAsync(org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.service_accounts.create", org.Id, auth.User.Id, "service_account.create", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{sa.Id}",
            new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt));
    }

    public record UpdateServiceAccountRequest(string? Name, string? Description);

    private static async Task<IResult> UpdateServiceAccountAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromBody] UpdateServiceAccountRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.UpdateAsync(org.Id, serviceAccountId, req.Name, req.Description, ct);
        if (sa is null)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.service_accounts.update", org.Id, auth.User!.Id, "service_account.update", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);

        return TypedResults.Ok(new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt));
    }

    private static async Task<IResult> DisableServiceAccountAsync(
        string orgSlug,
        Guid serviceAccountId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.DisableAsync(org.Id, serviceAccountId, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.service_accounts.disable", org.Id, auth.User!.Id, "service_account.disable", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);

        return TypedResults.Ok();
    }

    public record ServiceAccountApiKeyResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? ExpiresAt);

    public record CreateServiceAccountApiKeyResponse(
        Guid Id, string Name, string PlaintextKey, DateTime CreatedAt);

    private static async Task<IResult> ListServiceAccountApiKeysAsync(
        string orgSlug,
        Guid serviceAccountId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.GetAsync(org.Id, serviceAccountId, ct);
        if (sa is null)
        {
            return TypedResults.NotFound();
        }

        var keys = await store.ListApiKeysAsync(serviceAccountId, ct);

        var result = keys.Select(k => new ServiceAccountApiKeyResponse(
            k.Id, k.Name, k.Description, k.CreatedAt, k.ExpiresAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    public record CreateServiceAccountApiKeyRequest(string Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private static async Task<IResult> CreateServiceAccountApiKeyAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromBody] CreateServiceAccountApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.GetAsync(org.Id, serviceAccountId, ct);
        if (sa is null)
        {
            return TypedResults.NotFound();
        }

        var (plaintextKey, apiKey) = store.GenerateApiKey(
            serviceAccountId, req.Name, req.Description, req.ScopesJson, req.ExpiresAt);

        db.ServiceAccountApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("org.service_accounts.api_key.create", org.Id, auth.User!.Id, "service_account.api_key.create", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{apiKey.Id}",
            new CreateServiceAccountApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt));
    }

    private static async Task<IResult> RevokeServiceAccountApiKeyAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.RevokeApiKeyAsync(apiKeyId, serviceAccountId, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.service_accounts.api_key.revoke", org.Id, auth.User!.Id, "service_account.api_key.revoke", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);

        return TypedResults.Ok();
    }
}
