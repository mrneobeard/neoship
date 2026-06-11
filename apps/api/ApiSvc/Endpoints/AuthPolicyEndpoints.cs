using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

using static NeoShip.ApiSvc.Endpoints.EndpointResults;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps organization authentication policy endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapAuthPolicyEndpoints();
/// </code>
/// </example>
public static class AuthPolicyEndpoints
{
    /// <summary>
    /// Maps organization authentication policy endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapAuthPolicyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/org/{orgId:guid}/auth/policy");
        group.MapGet("", GetAuthPolicyAsync);
        group.MapPatch("", UpdateAuthPolicyAsync);
        return routes;
    }

    private sealed record AuthPolicyResponse(bool AllowPasswordAuth, bool AllowPasskeyAuth, bool AllowOidcSso, bool AllowSamlSso, bool RequireSso, string MfaPolicy, bool AllowSelfServiceExternalIdentityUnlink);

    private sealed record UpdateAuthPolicyRequest(bool? AllowPasswordAuth, bool? AllowPasskeyAuth, bool? AllowOidcSso, bool? AllowSamlSso, bool? RequireSso, string? MfaPolicy, bool? AllowSelfServiceExternalIdentityUnlink);

    private static async Task<IResult> GetAuthPolicyAsync(Guid orgId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.settings", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (auth.Org.Id != orgId)
        {
            return NotFound(httpContext);
        }

        return TypedResults.Ok(Envelope(httpContext, ToAuthPolicyResponse(auth.Org)));
    }

    private static async Task<IResult> UpdateAuthPolicyAsync(Guid orgId, [FromBody] UpdateAuthPolicyRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, OrganizationStore orgs, ShipDb db, AuditStore audit, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.settings", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (auth.Org.Id != orgId)
        {
            return NotFound(httpContext);
        }

        if (!TryParseMfaPolicy(req.MfaPolicy, out var mfaPolicy))
        {
            return ValidationError(httpContext, new Dictionary<string, string[]> { ["mfaPolicy"] = ["MFA policy must be off, admins_owners, or all_members."] });
        }

        var updated = await orgs.UpdateAuthPolicyAsync(auth.Org.Id, req.AllowPasswordAuth, req.AllowPasskeyAuth, req.AllowOidcSso, req.AllowSamlSso, req.RequireSso, mfaPolicy, req.AllowSelfServiceExternalIdentityUnlink, ct);
        if (updated is null)
        {
            return NotFound(httpContext);
        }

        await audit.RecordAsync("org.auth_policy.update", auth.Org.Id, auth.User!.Id, "org.auth_policy.update", targetType: "organization", targetId: auth.Org.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToAuthPolicyResponse(updated)));
    }

    private static AuthPolicyResponse ToAuthPolicyResponse(Organization org)
        => new(org.AllowPasswordAuth, org.AllowPasskeyAuth, org.AllowOidcSso, org.AllowSamlSso, org.RequireSso, org.MfaPolicy.Name, org.AllowSelfServiceExternalIdentityUnlink);

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

}
