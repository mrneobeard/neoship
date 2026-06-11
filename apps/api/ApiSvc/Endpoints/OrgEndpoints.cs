using System.Text.Json;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class OrgEndpoints
{
    private const double DefaultStepUpWindowMinutes = 15;
    private const int DefaultApiKeyLifetimeDays = 90;
    private const int MaxApiKeyLifetimeDays = 365;

    public static RouteGroupBuilder MapOrgEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/orgs/{orgSlug}");

        group.MapGet("/auth-policy", GetAuthPolicyAsync);
        group.MapPatch("/auth-policy", UpdateAuthPolicyAsync);

        group.MapGet("/permissions", ListPermissionsAsync);

        group.MapGet("/roles", ListRolesAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapGet("/roles/{roleId:guid}", GetRoleAsync);
        group.MapPatch("/roles/{roleId:guid}", UpdateRoleAsync);
        group.MapDelete("/roles/{roleId:guid}", DeleteRoleAsync);
        group.MapPost("/roles/{roleId:guid}/claims", AddRoleClaimAsync);
        group.MapDelete("/roles/{roleId:guid}/claims/{claimId}", RemoveRoleClaimAsync);
        group.MapPost("/roles/{roleId:guid}/users/{userId:guid}", AttachRoleUserAsync);
        group.MapDelete("/roles/{roleId:guid}/users/{userId:guid}", DetachRoleUserAsync);

        group.MapGet("/groups", ListGroupsAsync);
        group.MapPost("/groups", CreateGroupAsync);
        group.MapGet("/groups/{groupId:guid}", GetGroupAsync);
        group.MapPatch("/groups/{groupId:guid}", UpdateGroupAsync);
        group.MapDelete("/groups/{groupId:guid}", DeleteGroupAsync);
        group.MapPost("/groups/{groupId:guid}/members/users/{userId:guid}", AddGroupUserAsync);
        group.MapDelete("/groups/{groupId:guid}/members/users/{userId:guid}", RemoveGroupUserAsync);
        group.MapPost("/groups/{groupId:guid}/members/service-accounts/{serviceAccountId:guid}", AddGroupServiceAccountAsync);
        group.MapDelete("/groups/{groupId:guid}/members/service-accounts/{serviceAccountId:guid}", RemoveGroupServiceAccountAsync);
        group.MapPost("/groups/{groupId:guid}/roles/{roleId:guid}", AttachGroupRoleAsync);
        group.MapDelete("/groups/{groupId:guid}/roles/{roleId:guid}", DetachGroupRoleAsync);

        group.MapGet("/service-accounts", ListServiceAccountsAsync);
        group.MapPost("/service-accounts", CreateServiceAccountAsync);
        group.MapGet("/service-accounts/{serviceAccountId:guid}", GetServiceAccountAsync);
        group.MapPatch("/service-accounts/{serviceAccountId:guid}", UpdateServiceAccountAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/disable", DisableServiceAccountAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/enable", EnableServiceAccountAsync);

        group.MapGet("/service-accounts/{serviceAccountId:guid}/api-keys", ListServiceAccountApiKeysAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys", CreateServiceAccountApiKeyAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/rotate", RotateServiceAccountApiKeyAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/revoke", RevokeServiceAccountApiKeyAsync);
        group.MapGet("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims", ListServiceAccountApiKeyClaimsAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims", AddServiceAccountApiKeyClaimAsync);
        group.MapDelete("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims/{claimId}", RemoveServiceAccountApiKeyClaimAsync);
        group.MapGet("/service-accounts/{serviceAccountId:guid}/claims", ListServiceAccountClaimsAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/claims", AddServiceAccountClaimAsync);
        group.MapDelete("/service-accounts/{serviceAccountId:guid}/claims/{claimId:guid}", RemoveServiceAccountClaimAsync);

        return group;
    }

    /// <summary>
    /// Represents a claim summary on a role.
    /// </summary>
    public record RoleClaimResponse(long Id, string Type, string Value);

    /// <summary>
    /// Represents a role response.
    /// </summary>
    public record RoleResponse(Guid Id, string Key, string Name, string? Description, bool BuiltIn, bool Editable, List<RoleClaimResponse> Claims);

    /// <summary>
    /// Represents a role creation request.
    /// </summary>
    public record CreateRoleRequest(string Name, string? Description);

    /// <summary>
    /// Represents a role update request.
    /// </summary>
    public record UpdateRoleRequest(string? Name, string? Description);

    /// <summary>
    /// Represents a role permission grant request.
    /// </summary>
    public record AddRoleClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    /// <summary>
    /// Represents a registered permission definition.
    /// </summary>
    public record PermissionDefinitionResponse(string Key, string Resource, string Action, string Description, List<PermissionScopeKind> AllowedScopes);

    /// <summary>
    /// Represents an organization invite response.
    /// </summary>
    public record OrganizationInviteResponse(
        Guid Id,
        string Email,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        DateTime? AcceptedAt,
        DateTime? RevokedAt);

    /// <summary>
    /// Represents an organization invite creation response.
    /// </summary>
    public record CreateOrganizationInviteResponse(
        Guid Id,
        string Email,
        string Token,
        DateTime CreatedAt,
        DateTime ExpiresAt);

    /// <summary>
    /// Represents an organization invite creation request.
    /// </summary>
    public record CreateOrganizationInviteRequest(string Email, List<Guid>? RoleIds, List<Guid>? GroupIds);

    /// <summary>
    /// Represents organization authentication policy settings.
    /// </summary>
    /// <param name="AllowPasswordAuth">Whether password sign-in is allowed.</param>
    /// <param name="AllowPasskeyAuth">Whether passkey sign-in is allowed.</param>
    /// <param name="AllowOidcSso">Whether OIDC SSO sign-in is allowed.</param>
    /// <param name="AllowSamlSso">Whether SAML SSO sign-in is allowed.</param>
    /// <param name="RequireSso">Whether SSO is required.</param>
    /// <param name="MfaPolicy">The MFA enforcement policy.</param>
    /// <param name="AllowSelfServiceExternalIdentityUnlink">Whether users may unlink external identities themselves.</param>
    public record AuthPolicyResponse(
        bool AllowPasswordAuth,
        bool AllowPasskeyAuth,
        bool AllowOidcSso,
        bool AllowSamlSso,
        bool RequireSso,
        string MfaPolicy,
        bool AllowSelfServiceExternalIdentityUnlink);

    /// <summary>
    /// Represents organization authentication policy updates.
    /// </summary>
    /// <param name="AllowPasswordAuth">The optional password sign-in allowance.</param>
    /// <param name="AllowPasskeyAuth">The optional passkey sign-in allowance.</param>
    /// <param name="AllowOidcSso">The optional OIDC SSO sign-in allowance.</param>
    /// <param name="AllowSamlSso">The optional SAML SSO sign-in allowance.</param>
    /// <param name="RequireSso">The optional SSO requirement.</param>
    /// <param name="MfaPolicy">The optional MFA enforcement policy.</param>
    /// <param name="AllowSelfServiceExternalIdentityUnlink">The optional self-service external identity unlink allowance.</param>
    public record UpdateAuthPolicyRequest(
        bool? AllowPasswordAuth,
        bool? AllowPasskeyAuth,
        bool? AllowOidcSso,
        bool? AllowSamlSso,
        bool? RequireSso,
        string? MfaPolicy,
        bool? AllowSelfServiceExternalIdentityUnlink);

    /// <summary>
    /// Represents a group response.
    /// </summary>
    public record GroupResponse(Guid Id, string Name, string? Email, string? Description, int MemberCount, int ServiceAccountMemberCount, int RoleCount);

    /// <summary>
    /// Represents a group creation request.
    /// </summary>
    public record CreateGroupRequest(string Name, string? Email, string? Description);

    /// <summary>
    /// Represents a group update request.
    /// </summary>
    public record UpdateGroupRequest(string? Name, string? Email, string? Description);

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
        CancellationToken ct,
        bool allowServiceAccount = false)
    {
        var user = await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);
        if (user is not null)
        {
            var userAllowed = await HasOrgPermissionAsync(httpContext, permissions, user.Id, permission, orgSlug, ct);
            if (!userAllowed)
            {
                return (null, Forbidden(httpContext));
            }

            if (permission.Action == "write")
            {
                var stepUpFailure = await RequireRecentSessionAsync(httpContext, ct);
                if (stepUpFailure is not null)
                {
                    return (null, stepUpFailure);
                }
            }

            return (user, null);
        }

        var serviceAccountKey = await AuthenticateServiceAccountApiKeyAsync(httpContext, ct);
        if (serviceAccountKey?.ServiceAccount is null)
        {
            return (null, Unauthenticated(httpContext));
        }

        if (!allowServiceAccount)
        {
            return (null, Forbidden(httpContext));
        }

        var allowed = await HasServiceAccountOrgPermissionAsync(permissions, serviceAccountKey.Id, permission, orgSlug, ct);
        if (!allowed)
        {
            return (null, Forbidden(httpContext));
        }

        return (null, null);
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

        var session = await httpContext.RequestServices.GetRequiredService<ShipDb>()
            .UserSessions.FirstOrDefaultAsync(x => x.Id == sessionId.Value, ct);
        if (session is null)
        {
            return StepUpRequired(httpContext);
        }

        var windowMinutes = httpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Auth:StepUp:WindowMinutes", DefaultStepUpWindowMinutes);
        var window = TimeSpan.FromMinutes(windowMinutes <= 0 ? DefaultStepUpWindowMinutes : windowMinutes);
        var lastVerifiedAt = session.MfaVerifiedAt is not null && session.MfaVerifiedAt > session.CreatedAt
            ? session.MfaVerifiedAt.Value
            : session.CreatedAt;

        return lastVerifiedAt < DateTime.UtcNow.Subtract(window) ? StepUpRequired(httpContext) : null;
    }

    private static IResult StepUpRequired(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "step_up_required", "Recent authentication is required for this operation.");

    private static IResult Unauthenticated(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status401Unauthorized, "unauthenticated", "Authentication required.");

    private static IResult Forbidden(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");

    private static IResult BuiltInRoleConflict(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status409Conflict, "conflict", "Built-in roles are code-owned and cannot be modified.");

    private static async Task<ServiceAccountApiKey?> AuthenticateServiceAccountApiKeyAsync(HttpContext httpContext, CancellationToken ct)
    {
        var token = ReadBearerToken(httpContext.Request);
        if (token is null)
        {
            return null;
        }

        var serviceAccounts = httpContext.RequestServices.GetRequiredService<ServiceAccountStore>();
        return await serviceAccounts.AuthenticateApiKeyAsync(token, ct);
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

    private static async Task<bool> HasOrgPermissionAsync(
        HttpContext httpContext,
        PermissionResolver permissions,
        Guid userId,
        PermissionKey permission,
        string orgSlug,
        CancellationToken ct)
    {
        if (httpContext.Items.TryGetValue(MeEndpoints.UserApiKeyItemKey, out var value) && value is Guid apiKeyId)
        {
            return (await permissions.ResolveUserApiKeyAsync(apiKeyId, ct)).Allows(permission, PermissionScopeKind.Organization, orgSlug);
        }

        return await permissions.UserHasAsync(userId, permission, PermissionScopeKind.Organization, orgSlug, ct);
    }

    private static async Task<bool> HasServiceAccountOrgPermissionAsync(
        PermissionResolver permissions,
        Guid apiKeyId,
        PermissionKey permission,
        string orgSlug,
        CancellationToken ct)
    {
        return (await permissions.ResolveServiceAccountApiKeyAsync(apiKeyId, ct)).Allows(permission, PermissionScopeKind.Organization, orgSlug);
    }

    private static PermissionDefinitionResponse ToPermissionDefinitionResponse(PermissionDefinition definition)
        => new(
            definition.Key.ToString(),
            definition.Key.Resource,
            definition.Key.Action,
            definition.Description,
            definition.AllowedScopes.ToList());

    private static OrganizationInviteResponse ToInviteResponse(OrganizationInvite invite)
        => new(invite.Id, invite.Email, invite.CreatedAt, invite.ExpiresAt, invite.AcceptedAt, invite.RevokedAt);

    private static RoleResponse ToRoleResponse(Role role)
        => new(
            role.Id,
            role.Name.ToLowerInvariant(),
            role.Name,
            role.Description,
            false,
            true,
            role.Claims.Select(c => new RoleClaimResponse(c.Id, c.Type, c.Value)).ToList());

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

    private static GroupResponse ToGroupResponse(Group group, int builtInRoleCount = 0)
        => new(group.Id, group.Name, group.Email, group.Description, group.Members.Count, group.ServiceAccountMembers.Count, CustomRoleCount(group) + builtInRoleCount);

    private static int CustomRoleCount(Group group)
        => group.Roles.Count(x => !BuiltInRoleStore.IsBuiltInRoleName(x.Name));

    private static AuthPolicyResponse ToAuthPolicyResponse(Organization org)
        => new(
            org.AllowPasswordAuth,
            org.AllowPasskeyAuth,
            org.AllowOidcSso,
            org.AllowSamlSso,
            org.RequireSso,
            org.MfaPolicy.Name,
            org.AllowSelfServiceExternalIdentityUnlink);

    private static async Task<IResult> GetAuthPolicyAsync(
        string orgSlug,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.settings", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        return TypedResults.Ok(Envelope(httpContext, ToAuthPolicyResponse(org)));
    }

    private static async Task<IResult> UpdateAuthPolicyAsync(
        string orgSlug,
        [FromBody] UpdateAuthPolicyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        OrganizationStore orgs,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.settings", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (!TryParseMfaPolicy(req.MfaPolicy, out var mfaPolicy))
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?>
            {
                ["fields"] = new Dictionary<string, string[]> { ["mfaPolicy"] = ["MFA policy must be off, admins_owners, or all_members."] },
            });
        }

        var updated = await orgs.UpdateAuthPolicyAsync(
            org.Id,
            req.AllowPasswordAuth,
            req.AllowPasskeyAuth,
            req.AllowOidcSso,
            req.AllowSamlSso,
            req.RequireSso,
            mfaPolicy,
            req.AllowSelfServiceExternalIdentityUnlink,
            ct);
        if (updated is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.auth_policy.update", org.Id, auth.User!.Id, "org.auth_policy.update", targetType: "organization", targetId: org.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToAuthPolicyResponse(updated)));
    }

    private static bool TryParseMfaPolicy(string? value, out OrganizationMfaPolicy policy)
    {
        policy = OrganizationMfaPolicy.Off;
        if (value is null)
        {
            return true;
        }

        policy = value.Trim().ToLowerInvariant() switch
        {
            "off" => OrganizationMfaPolicy.Off,
            "admins_owners" => OrganizationMfaPolicy.AdminsAndOwners,
            "all_members" => OrganizationMfaPolicy.AllMembers,
            _ => OrganizationMfaPolicy.Unknown,
        };

        return policy.Id != OrganizationMfaPolicy.Unknown.Id;
    }

    private static async Task<IResult> ListPermissionsAsync(
        string orgSlug,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[resource]")] string? filterResource,
        [FromQuery(Name = "filter[action]")] string? filterAction,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        PermissionRegistry registry,
        ShipDb db,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParsePermissionListQuery(httpContext, limit, cursor, filterResource, filterAction);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<PermissionDefinition> filtered = registry.All;
        if (!string.IsNullOrWhiteSpace(query.FilterResource))
        {
            filtered = filtered.Where(x => x.Key.Resource.Contains(query.FilterResource, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.FilterAction))
        {
            filtered = filtered.Where(x => x.Key.Action.Contains(query.FilterAction, StringComparison.OrdinalIgnoreCase));
        }

        var page = filtered
            .OrderBy(x => x.Key.Resource)
            .ThenBy(x => x.Key.Action)
            .Skip(query.Offset)
            .Take(query.Limit + 1)
            .ToList();
        var hasMore = page.Count > query.Limit;
        var result = page.Take(query.Limit).Select(ToPermissionDefinitionResponse).ToList();
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(query.FilterResource)) filters["resource"] = query.FilterResource;
        if (!string.IsNullOrWhiteSpace(query.FilterAction)) filters["action"] = query.FilterAction;
        var queryMeta = new ApiQueryMeta(filters, ["resource", "action"], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<PermissionDefinitionResponse>(result, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private sealed record ParsedPermissionListQuery(int Limit, int Offset, string? FilterResource, string? FilterAction, Dictionary<string, string[]> Errors);

    private static ParsedPermissionListQuery ParsePermissionListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterResource, string? filterAction)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "limit", "cursor", "filter[resource]", "filter[action]" };
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

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !TryDecodeCursor(cursor, out offset))
        {
            errors["cursor"] = ["Cursor is invalid."];
        }

        if (filterResource is { Length: > 160 }) errors["filter.resource"] = ["Resource filter must be 160 characters or fewer."];
        if (filterAction is { Length: > 160 }) errors["filter.action"] = ["Action filter must be 160 characters or fewer."];

        return new ParsedPermissionListQuery(resolvedLimit, offset, filterResource, filterAction, errors);
    }

    private static async Task<IResult> ListInvitesAsync(
        string orgSlug,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[email]")] string? filterEmail,
        [FromQuery] string? sort,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        OrganizationStore orgs,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.members", "read"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var list = await orgs.ListInvitesAsync(org.Id, ct);
        var query = ParseEmailListQuery(httpContext, limit, cursor, filterEmail, sort);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<OrganizationInvite> filtered = list;
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
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterEmail) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["email"] = query.FilterEmail };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<OrganizationInviteResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private sealed record ParsedEmailListQuery(int Limit, int Offset, string? FilterEmail, string Sort, Dictionary<string, string[]> Errors);

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

    private static async Task<IResult> CreateInviteAsync(
        string orgSlug,
        [FromBody] CreateOrganizationInviteRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        OrganizationStore orgs,
        AuditStore audit,
        IEmailSender emails,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.members", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateCreateInvite(req);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var (invite, token) = await orgs.CreateInviteAsync(org.Id, auth.User!.Id, req.Email, req.RoleIds ?? [], req.GroupIds ?? [], ct);
        await audit.RecordAsync("org.invites.create", org.Id, auth.User.Id, "org.invite.create", targetType: "organization_invite", targetId: invite.Id.ToString(), ct: ct);
        await emails.SendAsync(new EmailMessage(
            invite.Email,
            $"You're invited to {org.Name}",
            $"You've been invited to join {org.Name}. Accept the invite at /accept-invite?token={token}"), ct);
        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/invites/{invite.Id}", Envelope(httpContext, new CreateOrganizationInviteResponse(invite.Id, invite.Email, token, invite.CreatedAt, invite.ExpiresAt)));
    }

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

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

    private static async Task<IResult> RevokeInviteAsync(
        string orgSlug,
        Guid inviteId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        OrganizationStore orgs,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.members", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var revoked = await orgs.RevokeInviteAsync(org.Id, inviteId, ct);
        if (!revoked)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.invites.revoke", org.Id, auth.User!.Id, "org.invite.revoke", targetType: "organization_invite", targetId: inviteId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> ListRolesAsync(
        string orgSlug,
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
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        var builtInRoles = BuiltInRoleStore.Definitions()
            .Select(x => new RoleListEntry(ToBuiltInRoleResponse(x, orgSlug), DateTime.MinValue))
            .ToList();
        var customRoles = (await roles.ListAsync(org.Id, ct))
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
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<RoleResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private sealed record RoleListEntry(RoleResponse Response, DateTime CreatedAt);

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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleInput(req.Name, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var role = await roles.CreateAsync(org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.roles.create", org.Id, auth.User.Id, "role.create", targetType: "role", targetId: role.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/roles/{role.Id}", Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> GetRoleAsync(
        string orgSlug,
        Guid roleId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            return TypedResults.Ok(Envelope(httpContext, ToBuiltInRoleResponse(builtInRole, orgSlug)));
        }

        var role = await roles.GetAsync(org.Id, roleId, ct);
        if (role is null)
        {
            return NotFoundError(httpContext);
        }

        return TypedResults.Ok(Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> UpdateRoleAsync(
        string orgSlug,
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
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleInput(req.Name, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var role = await roles.UpdateAsync(org.Id, roleId, req.Name, req.Description, ct);
        if (role is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.update", org.Id, auth.User!.Id, "role.update", targetType: "role", targetId: role.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToRoleResponse(role)));
    }

    private static async Task<IResult> DeleteRoleAsync(
        string orgSlug,
        Guid roleId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var deleted = await roles.DeleteAsync(org.Id, roleId, ct);
        if (!deleted)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.delete", org.Id, auth.User!.Id, "role.delete", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRoleClaim(req, out var key);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var grant = new PermissionGrant(key, req.ScopeKind, req.ScopeId);
        var added = await roles.AddClaimAsync(org.Id, roleId, grant, auth.User!.Id, ct);
        if (!added)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.claim.add", org.Id, auth.User.Id, "role.claim.add", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

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

    private static async Task<IResult> RemoveRoleClaimAsync(
        string orgSlug,
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
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (BuiltInRoleStore.FindById(roleId) is not null)
        {
            return BuiltInRoleConflict(httpContext);
        }

        var removed = await roles.RemoveClaimAsync(org.Id, roleId, claimId, ct);
        if (!removed)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.claim.remove", org.Id, auth.User!.Id, "role.claim.remove", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> AttachRoleUserAsync(
        string orgSlug,
        Guid roleId,
        Guid userId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            var userExists = await db.Users.AnyAsync(x => x.Id == userId && x.OrgId == org.Id, ct);
            if (!userExists)
            {
                return NotFoundError(httpContext);
            }

            await BuiltInRoleStore.AssignAsync(db, org.Id, orgSlug, userId, builtInRole.Key, auth.User!.Id, ct);
            await audit.RecordAsync("org.roles.user.add", org.Id, auth.User.Id, "role.user.add", targetType: "role", targetId: roleId.ToString(), ct: ct);
            return TypedResults.Ok(Envelope<object>(httpContext, null));
        }

        var attached = await roles.AttachUserAsync(org.Id, roleId, userId, ct);
        if (!attached)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.user.add", org.Id, auth.User!.Id, "role.user.add", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> DetachRoleUserAsync(
        string orgSlug,
        Guid roleId,
        Guid userId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.roles", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            var detachedBuiltIn = await BuiltInRoleStore.UnassignAsync(db, org.Id, orgSlug, userId, builtInRole.Key, ct);
            if (!detachedBuiltIn)
            {
                return NotFoundError(httpContext);
            }

            await audit.RecordAsync("org.roles.user.remove", org.Id, auth.User!.Id, "role.user.remove", targetType: "role", targetId: roleId.ToString(), ct: ct);
            return TypedResults.Ok(Envelope<object>(httpContext, null));
        }

        var detached = await roles.DetachUserAsync(org.Id, roleId, userId, ct);
        if (!detached)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.roles.user.remove", org.Id, auth.User!.Id, "role.user.remove", targetType: "role", targetId: roleId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> ListGroupsAsync(
        string orgSlug,
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
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var list = await groups.ListAsync(org.Id, ct);
        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: false);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<Group> filtered = list;
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
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<GroupResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateGroupInput(req.Name, req.Email, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var group = await groups.CreateAsync(org.Id, req.Name, req.Email, req.Description, ct);
        await audit.RecordAsync("org.groups.create", org.Id, auth.User!.Id, "group.create", targetType: "group", targetId: group.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/groups/{group.Id}", Envelope(httpContext, ToGroupResponse(group)));
    }

    private static async Task<IResult> GetGroupAsync(
        string orgSlug,
        Guid groupId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var group = await groups.GetAsync(org.Id, groupId, ct);
        if (group is null)
        {
            return NotFoundError(httpContext);
        }

        var builtInRoleCount = await db.RoleAssignments.CountAsync(x => x.GroupId == group.Id, ct);
        return TypedResults.Ok(Envelope(httpContext, ToGroupResponse(group, builtInRoleCount)));
    }

    private static async Task<IResult> UpdateGroupAsync(
        string orgSlug,
        Guid groupId,
        [FromBody] UpdateGroupRequest req,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateGroupInput(req.Name, req.Email, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var group = await groups.UpdateAsync(org.Id, groupId, req.Name, req.Email, req.Description, ct);
        if (group is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.update", org.Id, auth.User!.Id, "group.update", targetType: "group", targetId: group.Id.ToString(), ct: ct);
        var builtInRoleCount = await db.RoleAssignments.CountAsync(x => x.GroupId == group.Id, ct);
        return TypedResults.Ok(Envelope(httpContext, ToGroupResponse(group, builtInRoleCount)));
    }

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

    private static async Task<IResult> DeleteGroupAsync(
        string orgSlug,
        Guid groupId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var deleted = await groups.DeleteAsync(org.Id, groupId, ct);
        if (!deleted)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.delete", org.Id, auth.User!.Id, "group.delete", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.AddUserAsync(org.Id, groupId, userId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.member.add", org.Id, auth.User!.Id, "group.member.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.RemoveUserAsync(org.Id, groupId, userId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.member.remove", org.Id, auth.User!.Id, "group.member.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.AddServiceAccountAsync(org.Id, groupId, serviceAccountId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.service_account.add", org.Id, auth.User!.Id, "group.service_account.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = await groups.RemoveServiceAccountAsync(org.Id, groupId, serviceAccountId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.service_account.remove", org.Id, auth.User!.Id, "group.service_account.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            var assigned = await BuiltInRoleStore.AssignGroupAsync(db, org.Id, orgSlug, groupId, builtInRole.Key, auth.User!.Id, ct);
            if (!assigned)
            {
                return NotFoundError(httpContext);
            }

            await audit.RecordAsync("org.groups.role.add", org.Id, auth.User.Id, "group.role.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
            return TypedResults.Ok(Envelope<object>(httpContext, null));
        }

        var ok = await groups.AttachRoleAsync(org.Id, groupId, roleId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.role.add", org.Id, auth.User!.Id, "group.role.add", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.groups", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var builtInRole = BuiltInRoleStore.FindById(roleId);
        if (builtInRole is not null)
        {
            var detachedBuiltIn = await BuiltInRoleStore.UnassignGroupAsync(db, org.Id, orgSlug, groupId, builtInRole.Key, ct);
            if (!detachedBuiltIn)
            {
                return NotFoundError(httpContext);
            }

            await audit.RecordAsync("org.groups.role.remove", org.Id, auth.User!.Id, "group.role.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
            return TypedResults.Ok(Envelope<object>(httpContext, null));
        }

        var ok = await groups.DetachRoleAsync(org.Id, groupId, roleId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.groups.role.remove", org.Id, auth.User!.Id, "group.role.remove", targetType: "group", targetId: groupId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record ServiceAccountResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);

    private static async Task<IResult> ListServiceAccountsAsync(
        string orgSlug,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var accounts = await store.ListAsync(org.Id, ct);
        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<ServiceAccount> filtered = accounts;
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

        var result = page.Take(query.Limit).Select(s => new ServiceAccountResponse(
            s.Id, s.Name, s.Description, s.CreatedAt, s.UpdatedAt
        )).ToList();

        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<ServiceAccountResponse>(result, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    public record CreateServiceAccountRequest(string Name, string? Description);

    private static async Task<IResult> GetServiceAccountAsync(
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.GetAsync(org.Id, serviceAccountId, ct);
        if (sa is null)
        {
            return NotFoundError(httpContext);
        }

        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt)));
    }

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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountInput(req.Name, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var sa = await store.CreateAsync(org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.service_accounts.create", org.Id, auth.User.Id, "service_account.create", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{sa.Id}",
            Envelope(httpContext, new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt)));
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountInput(req.Name, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var sa = await store.UpdateAsync(org.Id, serviceAccountId, req.Name, req.Description, ct);
        if (sa is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.update", org.Id, auth.User!.Id, "service_account.update", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);

        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt)));
    }

    private static Dictionary<string, string[]> ValidateServiceAccountInput(string? name, string? description, bool requireName)
    {
        var errors = new Dictionary<string, string[]>();

        if ((requireName && string.IsNullOrWhiteSpace(name)) || (name is not null && (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)))
        {
            errors["name"] = ["Name is required and must be 160 characters or fewer."];
        }

        if (description is not null && description.Length > 1024)
        {
            errors["description"] = ["Description must be 1024 characters or fewer when provided."];
        }

        return errors;
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.DisableAsync(org.Id, serviceAccountId, ct);
        if (!success)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.disable", org.Id, auth.User!.Id, "service_account.disable", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);

        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> EnableServiceAccountAsync(
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.EnableAsync(org.Id, serviceAccountId, ct);
        if (!success)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.enable", org.Id, auth.User!.Id, "service_account.enable", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);

        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record ServiceAccountApiKeyResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? ExpiresAt);

    public record ServiceAccountClaimResponse(Guid Id, string Type, string Value);

    public record AddServiceAccountClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    public record ServiceAccountApiKeyClaimResponse(long Id, string Type, string Value);

    public record AddServiceAccountApiKeyClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    public record CreateServiceAccountApiKeyResponse(
        Guid Id, string Name, string PlaintextKey, DateTime CreatedAt);

    private static async Task<IResult> ListServiceAccountApiKeysAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.GetAsync(org.Id, serviceAccountId, ct);
        if (sa is null)
        {
            return NotFoundError(httpContext);
        }

        var keys = await store.ListApiKeysAsync(serviceAccountId, ct);
        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<ServiceAccountApiKey> filtered = keys;
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

        var result = page.Take(query.Limit).Select(k => new ServiceAccountApiKeyResponse(
            k.Id, k.Name, k.Description, k.CreatedAt, k.ExpiresAt
        )).ToList();

        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<ServiceAccountApiKeyResponse>(result, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    public record CreateServiceAccountApiKeyRequest(string Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    public record RotateServiceAccountApiKeyRequest(string? Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

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
        IConfiguration configuration,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountApiKey(req, configuration);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var created = await store.CreateApiKeyAsync(org.Id, serviceAccountId, req.Name, req.Description, req.ScopesJson, ResolveApiKeyExpiresAt(req.ExpiresAt, configuration), ct);
        if (created is null)
        {
            return NotFoundError(httpContext);
        }

        var (plaintextKey, apiKey) = created.Value;
        await audit.RecordAsync("org.service_accounts.api_key.create", org.Id, auth.User!.Id, "service_account.api_key.create", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{apiKey.Id}",
            Envelope(httpContext, new CreateServiceAccountApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt)));
    }

    private static Dictionary<string, string[]> ValidateServiceAccountApiKey(CreateServiceAccountApiKeyRequest req, IConfiguration configuration)
    {
        var errors = ValidateServiceAccountInput(req.Name, req.Description, requireName: true);

        if (!IsValidJsonArray(req.ScopesJson))
        {
            errors["scopesJson"] = ["Scopes JSON must be a valid JSON array when provided."];
        }

        if (req.ExpiresAt is not null && req.ExpiresAt <= DateTime.UtcNow)
        {
            errors["expiresAt"] = ["Expiration must be in the future when provided."];
        }
        else if (req.ExpiresAt is not null && req.ExpiresAt > MaxApiKeyExpiresAt(configuration))
        {
            errors["expiresAt"] = ["Expiration exceeds the maximum API key lifetime."];
        }

        return errors;
    }

    private static bool IsValidJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.RevokeApiKeyAsync(org.Id, apiKeyId, serviceAccountId, ct);
        if (!success)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.revoke", org.Id, auth.User!.Id, "service_account.api_key.revoke", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);

        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> RotateServiceAccountApiKeyAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        [FromBody] RotateServiceAccountApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        ServiceAccountStore store,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRotateServiceAccountApiKey(req, configuration);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var rotated = await store.RotateApiKeyAsync(org.Id, serviceAccountId, apiKeyId, req.Name, req.Description, req.ScopesJson, req.ExpiresAt is null ? null : ResolveApiKeyExpiresAt(req.ExpiresAt, configuration), ResolveApiKeyExpiresAt(null, configuration), ct);
        if (rotated is null)
        {
            return NotFoundError(httpContext);
        }

        var (plaintextKey, newKey) = rotated.Value;
        await audit.RecordAsync("org.service_accounts.api_key.rotate", org.Id, auth.User!.Id, "service_account.api_key.rotate", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{newKey.Id}",
            Envelope(httpContext, new CreateServiceAccountApiKeyResponse(newKey.Id, newKey.Name, plaintextKey, newKey.CreatedAt)));
    }

    private static Dictionary<string, string[]> ValidateRotateServiceAccountApiKey(RotateServiceAccountApiKeyRequest req, IConfiguration configuration)
    {
        var errors = new Dictionary<string, string[]>();

        if (req.Name is not null && (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 160))
        {
            errors["name"] = ["Name must be 160 characters or fewer when provided."];
        }

        if (req.Description is not null && req.Description.Length > 1024)
        {
            errors["description"] = ["Description must be 1024 characters or fewer when provided."];
        }

        if (!IsValidJsonArray(req.ScopesJson))
        {
            errors["scopesJson"] = ["Scopes JSON must be a valid JSON array when provided."];
        }

        if (req.ExpiresAt is not null && req.ExpiresAt <= DateTime.UtcNow)
        {
            errors["expiresAt"] = ["Expiration must be in the future when provided."];
        }
        else if (req.ExpiresAt is not null && req.ExpiresAt > MaxApiKeyExpiresAt(configuration))
        {
            errors["expiresAt"] = ["Expiration exceeds the maximum API key lifetime."];
        }

        return errors;
    }

    private static DateTime ResolveApiKeyExpiresAt(DateTime? requestedExpiresAt, IConfiguration configuration)
        => requestedExpiresAt ?? DateTime.UtcNow.AddDays(configuration.GetValue("Auth:ApiKeys:DefaultLifetimeDays", DefaultApiKeyLifetimeDays));

    private static DateTime MaxApiKeyExpiresAt(IConfiguration configuration)
        => DateTime.UtcNow.AddDays(configuration.GetValue("Auth:ApiKeys:MaxLifetimeDays", MaxApiKeyLifetimeDays));

    private static async Task<IResult> ListServiceAccountApiKeyClaimsAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[type]")] string? filterType,
        [FromQuery] string? sort,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var claims = await store.ListApiKeyClaimsAsync(org.Id, serviceAccountId, apiKeyId, ct);
        if (claims is null)
        {
            return NotFoundError(httpContext);
        }

        var query = ParseClaimListQuery(httpContext, limit, cursor, filterType, sort);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<ServiceAccountApiKeyClaim> filtered = claims;
        if (!string.IsNullOrWhiteSpace(query.FilterType))
        {
            filtered = filtered.Where(x => x.Type.Contains(query.FilterType, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort == "-type"
            ? filtered.OrderByDescending(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            : filtered.OrderBy(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);
        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(x => new ServiceAccountApiKeyClaimResponse(x.Id, x.Type, x.Value)).ToList();
        return ClaimCollection(httpContext, data, query, hasMore, cursor);
    }

    private static async Task<IResult> AddServiceAccountApiKeyClaimAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        [FromBody] AddServiceAccountApiKeyClaimRequest req,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidatePermissionGrant(req.Permission, req.ScopeKind, req.ScopeId, out var key);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var grant = new PermissionGrant(key, req.ScopeKind, req.ScopeId);
        var claim = await store.AddApiKeyClaimAsync(org.Id, serviceAccountId, apiKeyId, grant, ct);
        if (claim is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.claim.add", org.Id, auth.User!.Id, "service_account.api_key.claim.add", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountApiKeyClaimResponse(claim.Id, claim.Type, claim.Value)));
    }

    private static async Task<IResult> RemoveServiceAccountApiKeyClaimAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        long claimId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var removed = await store.RemoveApiKeyClaimAsync(org.Id, serviceAccountId, apiKeyId, claimId, ct);
        if (!removed)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.claim.remove", org.Id, auth.User!.Id, "service_account.api_key.claim.remove", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    private static async Task<IResult> ListServiceAccountClaimsAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[type]")] string? filterType,
        [FromQuery] string? sort,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var claims = await store.ListClaimsAsync(org.Id, serviceAccountId, ct);
        if (claims is null)
        {
            return NotFoundError(httpContext);
        }

        var query = ParseClaimListQuery(httpContext, limit, cursor, filterType, sort);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<ServiceAccountClaim> filtered = claims;
        if (!string.IsNullOrWhiteSpace(query.FilterType))
        {
            filtered = filtered.Where(x => x.Type.Contains(query.FilterType, StringComparison.OrdinalIgnoreCase));
        }

        filtered = query.Sort == "-type"
            ? filtered.OrderByDescending(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            : filtered.OrderBy(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);
        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var hasMore = page.Count > query.Limit;
        var data = page.Take(query.Limit).Select(x => new ServiceAccountClaimResponse(x.Id, x.Type, x.Value)).ToList();
        return ClaimCollection(httpContext, data, query, hasMore, cursor);
    }

    private sealed record ParsedClaimListQuery(int Limit, int Offset, string? FilterType, string Sort, Dictionary<string, string[]> Errors);

    private static IResult ClaimCollection<T>(HttpContext httpContext, IReadOnlyList<T> data, ParsedClaimListQuery query, bool hasMore, string? cursor)
    {
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterType) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["type"] = query.FilterType };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<T>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static ParsedClaimListQuery ParseClaimListQuery(HttpContext httpContext, int? limit, string? cursor, string? filterType, string? sort)
    {
        var errors = new Dictionary<string, string[]>();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "limit", "cursor", "filter[type]", "sort" };
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

        var resolvedSort = string.IsNullOrWhiteSpace(sort) ? "type" : sort.Trim();
        if (resolvedSort is not ("type" or "-type"))
        {
            errors["sort"] = ["Sort must be type or -type."];
            resolvedSort = "type";
        }

        var offset = 0;
        if (!string.IsNullOrWhiteSpace(cursor) && !TryDecodeCursor(cursor, out offset))
        {
            errors["cursor"] = ["Cursor is invalid."];
        }

        if (filterType is { Length: > 160 })
        {
            errors["filter.type"] = ["Type filter must be 160 characters or fewer."];
        }

        return new ParsedClaimListQuery(resolvedLimit, offset, filterType, resolvedSort, errors);
    }

    private static async Task<IResult> AddServiceAccountClaimAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromBody] AddServiceAccountClaimRequest req,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidatePermissionGrant(req.Permission, req.ScopeKind, req.ScopeId, out var key);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var grant = new PermissionGrant(key, req.ScopeKind, req.ScopeId);
        var claim = await store.AddClaimAsync(org.Id, serviceAccountId, grant, ct);
        if (claim is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.claim.add", org.Id, auth.User!.Id, "service_account.claim.add", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountClaimResponse(claim.Id, claim.Type, claim.Value)));
    }

    private static Dictionary<string, string[]> ValidatePermissionGrant(string permission, PermissionScopeKind scopeKind, string? scopeId, out PermissionKey key)
    {
        var errors = new Dictionary<string, string[]>();
        if (!PermissionKey.TryParse(permission, out key))
        {
            errors["permission"] = ["Permission must be a registered resource.action key."];
        }

        if (!Enum.IsDefined(scopeKind))
        {
            errors["scopeKind"] = ["Scope kind is invalid."];
        }

        if (scopeId is not null && scopeId.Length > 160)
        {
            errors["scopeId"] = ["Scope ID must be 160 characters or fewer when provided."];
        }

        return errors;
    }

    private static async Task<IResult> RemoveServiceAccountClaimAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid claimId,
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
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var removed = await store.RemoveClaimAsync(org.Id, serviceAccountId, claimId, ct);
        if (!removed)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.claim.remove", org.Id, auth.User!.Id, "service_account.claim.remove", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object>(httpContext, null));
    }

    public record IdentityProviderResponse(
        long Id,
        string Name,
        string ProviderType,
        string Status,
        string? IssuerUrl,
        string? ClientId,
        bool HasClientSecret,
        string? MetadataJson,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    public record CreateIdentityProviderRequest(
        string Name,
        string? ProviderType,
        string? Preset,
        string? IssuerUrl,
        string? ClientId,
        string? ClientSecret,
        string? MetadataJson);

    public record UpdateIdentityProviderRequest(
        string? Name,
        string? IssuerUrl,
        string? ClientId,
        string? ClientSecret,
        string? MetadataJson);

    private static IdentityProviderResponse ToIdentityProviderResponse(UserIdentityProvider provider)
        => new(
            provider.Id,
            provider.Name,
            provider.ProviderType.Name,
            provider.Status.Name,
            provider.IssuerUrl,
            provider.ClientId,
            provider.ClientSecretEncrypted.Length > 0,
            provider.MetadataJson,
            provider.CreatedAt,
            provider.UpdatedAt);

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

    private sealed record IdentityProviderPreset(UserIdentityProviderType ProviderType, string? IssuerUrl, string MetadataJson);

    private static bool TryGetIdentityProviderPreset(string? value, out IdentityProviderPreset preset)
    {
        preset = value?.Trim().ToLowerInvariant() switch
        {
            "github" => new IdentityProviderPreset(
                UserIdentityProviderType.OAUTH2,
                null,
                "{\"authorization_endpoint\":\"https://github.com/login/oauth/authorize\",\"token_endpoint\":\"https://github.com/login/oauth/access_token\",\"user_endpoint\":\"https://api.github.com/user\",\"email_endpoint\":\"https://api.github.com/user/emails\",\"default_scopes\":[\"read:user\",\"user:email\"]}"),
            "google" => new IdentityProviderPreset(
                UserIdentityProviderType.OIDC,
                "https://accounts.google.com",
                "{\"authorization_endpoint\":\"https://accounts.google.com/o/oauth2/v2/auth\",\"token_endpoint\":\"https://oauth2.googleapis.com/token\",\"default_scopes\":[\"openid\",\"email\",\"profile\"]}"),
            "microsoft" => new IdentityProviderPreset(
                UserIdentityProviderType.OIDC,
                "https://login.microsoftonline.com/common/v2.0",
                "{\"authorization_endpoint\":\"https://login.microsoftonline.com/common/oauth2/v2.0/authorize\",\"token_endpoint\":\"https://login.microsoftonline.com/common/oauth2/v2.0/token\",\"default_scopes\":[\"openid\",\"email\",\"profile\"]}"),
            _ => new IdentityProviderPreset(UserIdentityProviderType.Unknown, null, string.Empty),
        };

        return preset.ProviderType.Id != UserIdentityProviderType.Unknown.Id;
    }

    private static bool IsValidIssuerUrl(string? issuerUrl)
    {
        return issuerUrl is null || (Uri.TryCreate(issuerUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
    }

    private static async Task<IResult> ListIdentityProvidersAsync(
        string orgSlug,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery(Name = "filter[name]")] string? filterName,
        [FromQuery] string? sort,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.identity_providers", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var list = await users.ListIdentityProvidersAsync(org.Id, ct);
        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = query.Errors });
        }

        IEnumerable<UserIdentityProvider> filtered = list;
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
        var nextCursor = hasMore ? EncodeCursor(query.Offset + query.Limit) : null;
        var pagination = new ApiPagination(query.Limit, nextCursor, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);

        return TypedResults.Ok(new ApiCollectionEnvelope<IdentityProviderResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateIdentityProviderAsync(
        string orgSlug,
        [FromBody] CreateIdentityProviderRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateCreateIdentityProvider(req, out var providerType, out var preset);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var provider = await users.CreateIdentityProviderAsync(
            org.Id,
            auth.User!.Id,
            req.Name,
            providerType,
            req.IssuerUrl ?? preset?.IssuerUrl,
            req.ClientId,
            req.ClientSecret,
            req.MetadataJson ?? preset?.MetadataJson,
            ct);
        await audit.RecordAsync("org.identity_providers.create", org.Id, auth.User.Id, "identity_provider.create", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);

        return TypedResults.Created($"/api/v1/orgs/{orgSlug}/identity-providers/{provider.Id}", Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> GetIdentityProviderAsync(
        string orgSlug,
        long providerId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.identity_providers", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var provider = await users.GetIdentityProviderAsync(org.Id, providerId, ct);
        if (provider is null)
        {
            return NotFoundError(httpContext);
        }

        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> UpdateIdentityProviderAsync(
        string orgSlug,
        long providerId,
        [FromBody] UpdateIdentityProviderRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateUpdateIdentityProvider(req);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var provider = await users.UpdateIdentityProviderAsync(org.Id, providerId, req.Name, req.IssuerUrl, req.ClientId, req.ClientSecret, req.MetadataJson, ct);
        if (provider is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.identity_providers.update", org.Id, auth.User!.Id, "identity_provider.update", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static Dictionary<string, string[]> ValidateCreateIdentityProvider(
        CreateIdentityProviderRequest req,
        out UserIdentityProviderType providerType,
        out IdentityProviderPreset? preset)
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

    private static void AddIdentityProviderCommonErrors(
        Dictionary<string, string[]> errors,
        string? issuerUrl,
        string? clientId,
        string? clientSecret,
        string? metadataJson)
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

    private static async Task<IResult> EnableIdentityProviderAsync(
        string orgSlug,
        long providerId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        AuditStore audit,
        CancellationToken ct)
        => await SetIdentityProviderActiveAsync(orgSlug, providerId, active: true, httpContext, sessions, permissions, db, users, audit, ct);

    private static async Task<IResult> DisableIdentityProviderAsync(
        string orgSlug,
        long providerId,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        AuditStore audit,
        CancellationToken ct)
        => await SetIdentityProviderActiveAsync(orgSlug, providerId, active: false, httpContext, sessions, permissions, db, users, audit, ct);

    private static async Task<IResult> SetIdentityProviderActiveAsync(
        string orgSlug,
        long providerId,
        bool active,
        HttpContext httpContext,
        SessionStore sessions,
        PermissionResolver permissions,
        ShipDb db,
        UserStore users,
        AuditStore audit,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return NotFoundError(httpContext);
        }

        var auth = await RequireOrgPermissionAsync(httpContext, sessions, permissions, orgSlug, PermissionKey.Create("org.identity_providers", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        UserIdentityProvider? provider;
        try
        {
            provider = await users.SetIdentityProviderActiveAsync(org.Id, providerId, active, ct);
        }
        catch (ArgumentException)
        {
            return Error(httpContext, StatusCodes.Status400BadRequest, "invalid_identity_provider", "Invalid identity provider request.");
        }

        if (provider is null)
        {
            return NotFoundError(httpContext);
        }

        var action = active ? "identity_provider.enable" : "identity_provider.disable";
        await audit.RecordAsync($"org.identity_providers.{(active ? "enable" : "disable")}", org.Id, auth.User!.Id, action, targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }
}
