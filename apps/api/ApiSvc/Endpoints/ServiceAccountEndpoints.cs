using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

using static NeoShip.ApiSvc.Endpoints.EndpointResults;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization service-account endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapServiceAccountEndpoints();
/// </code>
/// </example>
public static class ServiceAccountEndpoints
{
    private const double DefaultStepUpWindowMinutes = 15;
    private const int DefaultApiKeyLifetimeDays = 90;
    private const int MaxApiKeyLifetimeDays = 365;

    /// <summary>
    /// Maps current-organization service-account endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapServiceAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/service-accounts");
        group.MapGet("", ListServiceAccountsAsync);
        group.MapPost("", CreateServiceAccountAsync);
        group.MapGet("/{serviceAccountId:guid}", GetServiceAccountAsync);
        group.MapPatch("/{serviceAccountId:guid}", UpdateServiceAccountAsync);
        group.MapPost("/{serviceAccountId:guid}/disable", DisableServiceAccountAsync);
        group.MapPost("/{serviceAccountId:guid}/enable", EnableServiceAccountAsync);
        group.MapGet("/{serviceAccountId:guid}/api-keys", ListServiceAccountApiKeysAsync);
        group.MapPost("/{serviceAccountId:guid}/api-keys", CreateServiceAccountApiKeyAsync);
        group.MapPost("/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/rotate", RotateServiceAccountApiKeyAsync);
        group.MapPost("/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/revoke", RevokeServiceAccountApiKeyAsync);
        group.MapGet("/{serviceAccountId:guid}/claims", ListServiceAccountClaimsAsync);
        group.MapPost("/{serviceAccountId:guid}/claims", AddServiceAccountClaimAsync);
        group.MapDelete("/{serviceAccountId:guid}/claims/{claimId:guid}", RemoveServiceAccountClaimAsync);
        group.MapGet("/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims", ListServiceAccountApiKeyClaimsAsync);
        group.MapPost("/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims", AddServiceAccountApiKeyClaimAsync);
        group.MapDelete("/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/claims/{claimId:long}", RemoveServiceAccountApiKeyClaimAsync);
        return routes;
    }

    private sealed record ServiceAccountResponse(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record CreateServiceAccountRequest(string Name, string? Description);

    private sealed record UpdateServiceAccountRequest(string? Name, string? Description);

    private sealed record ServiceAccountApiKeyResponse(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? ExpiresAt);

    private sealed record CreateServiceAccountApiKeyRequest(string Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private sealed record RotateServiceAccountApiKeyRequest(string? Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private sealed record CreateServiceAccountApiKeyResponse(Guid Id, string Name, string PlaintextKey, DateTime CreatedAt);

    private sealed record ServiceAccountClaimResponse(Guid Id, string Type, string Value);

    private sealed record ServiceAccountApiKeyClaimResponse(long Id, string Type, string Value);

    private sealed record AddServiceAccountClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    private sealed record AddServiceAccountApiKeyClaimRequest(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);

    private sealed record AuthContext(User? User, ServiceAccountApiKey? ServiceAccountKey, Organization Org, IResult? Failure);

    private sealed record ParsedNamedListQuery(int Limit, int Offset, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

    private sealed record ParsedClaimListQuery(int Limit, int Offset, string? FilterType, string Sort, Dictionary<string, string[]> Errors);

    private static async Task<IResult> ListServiceAccountsAsync([FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[name]")] string? filterName, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<ServiceAccount> filtered = await store.ListAsync(auth.Org.Id, ct);
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
        var data = page.Take(query.Limit).Select(ToServiceAccountResponse).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<ServiceAccountResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> GetServiceAccountAsync(Guid serviceAccountId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var sa = await store.GetAsync(auth.Org.Id, serviceAccountId, ct);
        return sa is null ? NotFoundError(httpContext) : TypedResults.Ok(Envelope(httpContext, ToServiceAccountResponse(sa)));
    }

    private static async Task<IResult> CreateServiceAccountAsync([FromBody] CreateServiceAccountRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountInput(req.Name, req.Description, requireName: true);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var sa = await store.CreateAsync(auth.Org.Id, auth.User!.Id, req.Name, req.Description, ct);
        await audit.RecordAsync("org.service_accounts.create", auth.Org.Id, auth.User.Id, "service_account.create", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/service-accounts/{sa.Id}", Envelope(httpContext, ToServiceAccountResponse(sa)));
    }

    private static async Task<IResult> UpdateServiceAccountAsync(Guid serviceAccountId, [FromBody] UpdateServiceAccountRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountInput(req.Name, req.Description, requireName: false);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var sa = await store.UpdateAsync(auth.Org.Id, serviceAccountId, req.Name, req.Description, ct);
        if (sa is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.update", auth.Org.Id, auth.User!.Id, "service_account.update", targetType: "service_account", targetId: sa.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToServiceAccountResponse(sa)));
    }

    private static async Task<IResult> DisableServiceAccountAsync(Guid serviceAccountId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
        => await SetServiceAccountEnabledAsync(serviceAccountId, enabled: false, httpContext, sessions, permissions, db, store, audit, ct);

    private static async Task<IResult> EnableServiceAccountAsync(Guid serviceAccountId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
        => await SetServiceAccountEnabledAsync(serviceAccountId, enabled: true, httpContext, sessions, permissions, db, store, audit, ct);

    private static async Task<IResult> SetServiceAccountEnabledAsync(Guid serviceAccountId, bool enabled, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var ok = enabled ? await store.EnableAsync(auth.Org.Id, serviceAccountId, ct) : await store.DisableAsync(auth.Org.Id, serviceAccountId, ct);
        if (!ok)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync($"org.service_accounts.{(enabled ? "enable" : "disable")}", auth.Org.Id, auth.User!.Id, $"service_account.{(enabled ? "enable" : "disable")}", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListServiceAccountApiKeysAsync(Guid serviceAccountId, [FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[name]")] string? filterName, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        if (await store.GetAsync(auth.Org.Id, serviceAccountId, ct) is null)
        {
            return NotFoundError(httpContext);
        }

        var query = ParseNamedListQuery(httpContext, limit, cursor, filterName, sort, allowCreatedAtSort: true);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<ServiceAccountApiKey> filtered = await store.ListApiKeysAsync(serviceAccountId, ct);
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
        var data = page.Take(query.Limit).Select(x => new ServiceAccountApiKeyResponse(x.Id, x.Name, x.Description, x.CreatedAt, x.ExpiresAt)).ToList();
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterName) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["name"] = query.FilterName };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<ServiceAccountApiKeyResponse>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static async Task<IResult> CreateServiceAccountApiKeyAsync(Guid serviceAccountId, [FromBody] CreateServiceAccountApiKeyRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, IConfiguration configuration, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateServiceAccountApiKey(req, configuration);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var created = await store.CreateApiKeyAsync(auth.Org.Id, serviceAccountId, req.Name, req.Description, req.ScopesJson, ResolveApiKeyExpiresAt(req.ExpiresAt, configuration), ct);
        if (created is null)
        {
            return NotFoundError(httpContext);
        }

        var (plaintextKey, apiKey) = created.Value;
        await audit.RecordAsync("org.service_accounts.api_key.create", auth.Org.Id, auth.User!.Id, "service_account.api_key.create", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/service-accounts/{serviceAccountId}/api-keys/{apiKey.Id}", Envelope(httpContext, new CreateServiceAccountApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt)));
    }

    private static async Task<IResult> RotateServiceAccountApiKeyAsync(Guid serviceAccountId, Guid apiKeyId, [FromBody] RotateServiceAccountApiKeyRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, IConfiguration configuration, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidateRotateServiceAccountApiKey(req, configuration);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var rotated = await store.RotateApiKeyAsync(auth.Org.Id, serviceAccountId, apiKeyId, req.Name, req.Description, req.ScopesJson, req.ExpiresAt is null ? null : ResolveApiKeyExpiresAt(req.ExpiresAt, configuration), ResolveApiKeyExpiresAt(null, configuration), ct);
        if (rotated is null)
        {
            return NotFoundError(httpContext);
        }

        var (plaintextKey, newKey) = rotated.Value;
        await audit.RecordAsync("org.service_accounts.api_key.rotate", auth.Org.Id, auth.User!.Id, "service_account.api_key.rotate", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/service-accounts/{serviceAccountId}/api-keys/{newKey.Id}", Envelope(httpContext, new CreateServiceAccountApiKeyResponse(newKey.Id, newKey.Name, plaintextKey, newKey.CreatedAt)));
    }

    private static async Task<IResult> RevokeServiceAccountApiKeyAsync(Guid serviceAccountId, Guid apiKeyId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var success = await store.RevokeApiKeyAsync(auth.Org.Id, apiKeyId, serviceAccountId, ct);
        if (!success)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.revoke", auth.Org.Id, auth.User!.Id, "service_account.api_key.revoke", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListServiceAccountClaimsAsync(Guid serviceAccountId, [FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[type]")] string? filterType, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var claims = await store.ListClaimsAsync(auth.Org.Id, serviceAccountId, ct);
        if (claims is null)
        {
            return NotFoundError(httpContext);
        }

        var query = ParseClaimListQuery(httpContext, limit, cursor, filterType, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<ServiceAccountClaim> filtered = claims;
        if (!string.IsNullOrWhiteSpace(query.FilterType))
        {
            filtered = filtered.Where(x => x.Type.Contains(query.FilterType, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SortClaims(filtered, query.Sort);
        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var data = page.Take(query.Limit).Select(x => new ServiceAccountClaimResponse(x.Id, x.Type, x.Value)).ToList();
        return ClaimCollection(httpContext, data, query, page.Count > query.Limit, cursor);
    }

    private static async Task<IResult> AddServiceAccountClaimAsync(Guid serviceAccountId, [FromBody] AddServiceAccountClaimRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidatePermissionGrant(req.Permission, req.ScopeKind, req.ScopeId, out var key);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var claim = await store.AddClaimAsync(auth.Org.Id, serviceAccountId, new PermissionGrant(key, req.ScopeKind, req.ScopeId), ct);
        if (claim is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.claim.add", auth.Org.Id, auth.User!.Id, "service_account.claim.add", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountClaimResponse(claim.Id, claim.Type, claim.Value)));
    }

    private static async Task<IResult> RemoveServiceAccountClaimAsync(Guid serviceAccountId, Guid claimId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var removed = await store.RemoveClaimAsync(auth.Org.Id, serviceAccountId, claimId, ct);
        if (!removed)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.claim.remove", auth.Org.Id, auth.User!.Id, "service_account.claim.remove", targetType: "service_account", targetId: serviceAccountId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static async Task<IResult> ListServiceAccountApiKeyClaimsAsync(Guid serviceAccountId, Guid apiKeyId, [FromQuery] int? limit, [FromQuery] string? cursor, [FromQuery(Name = "filter[type]")] string? filterType, [FromQuery] string? sort, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var claims = await store.ListApiKeyClaimsAsync(auth.Org.Id, serviceAccountId, apiKeyId, ct);
        if (claims is null)
        {
            return NotFoundError(httpContext);
        }

        var query = ParseClaimListQuery(httpContext, limit, cursor, filterType, sort);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
        }

        IEnumerable<ServiceAccountApiKeyClaim> filtered = claims;
        if (!string.IsNullOrWhiteSpace(query.FilterType))
        {
            filtered = filtered.Where(x => x.Type.Contains(query.FilterType, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SortClaims(filtered, query.Sort);
        var page = filtered.Skip(query.Offset).Take(query.Limit + 1).ToList();
        var data = page.Take(query.Limit).Select(x => new ServiceAccountApiKeyClaimResponse(x.Id, x.Type, x.Value)).ToList();
        return ClaimCollection(httpContext, data, query, page.Count > query.Limit, cursor);
    }

    private static async Task<IResult> AddServiceAccountApiKeyClaimAsync(Guid serviceAccountId, Guid apiKeyId, [FromBody] AddServiceAccountApiKeyClaimRequest req, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var validation = ValidatePermissionGrant(req.Permission, req.ScopeKind, req.ScopeId, out var key);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var claim = await store.AddApiKeyClaimAsync(auth.Org.Id, serviceAccountId, apiKeyId, new PermissionGrant(key, req.ScopeKind, req.ScopeId), ct);
        if (claim is null)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.claim.add", auth.Org.Id, auth.User!.Id, "service_account.api_key.claim.add", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, new ServiceAccountApiKeyClaimResponse(claim.Id, claim.Type, claim.Value)));
    }

    private static async Task<IResult> RemoveServiceAccountApiKeyClaimAsync(Guid serviceAccountId, Guid apiKeyId, long claimId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, ServiceAccountStore store, AuditStore audit, CancellationToken ct)
    {
        var auth = await RequireCurrentOrgPermissionAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.service_accounts", "write"), ct);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var removed = await store.RemoveApiKeyClaimAsync(auth.Org.Id, serviceAccountId, apiKeyId, claimId, ct);
        if (!removed)
        {
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync("org.service_accounts.api_key.claim.remove", auth.Org.Id, auth.User!.Id, "service_account.api_key.claim.remove", targetType: "service_account_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok(Envelope<object?>(httpContext, null));
    }

    private static ServiceAccountResponse ToServiceAccountResponse(ServiceAccount sa)
        => new(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt);

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

    private static DateTime ResolveApiKeyExpiresAt(DateTime? requestedExpiresAt, IConfiguration configuration)
        => requestedExpiresAt ?? DateTime.UtcNow.AddDays(configuration.GetValue("Auth:ApiKeys:DefaultLifetimeDays", DefaultApiKeyLifetimeDays));

    private static DateTime MaxApiKeyExpiresAt(IConfiguration configuration)
        => DateTime.UtcNow.AddDays(configuration.GetValue("Auth:ApiKeys:MaxLifetimeDays", MaxApiKeyLifetimeDays));

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

    private static IEnumerable<ServiceAccountClaim> SortClaims(IEnumerable<ServiceAccountClaim> claims, string sort)
        => sort == "-type"
            ? claims.OrderByDescending(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            : claims.OrderBy(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);

    private static IEnumerable<ServiceAccountApiKeyClaim> SortClaims(IEnumerable<ServiceAccountApiKeyClaim> claims, string sort)
        => sort == "-type"
            ? claims.OrderByDescending(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id)
            : claims.OrderBy(x => x.Type, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id);

    private static IResult ClaimCollection<T>(HttpContext httpContext, IReadOnlyList<T> data, ParsedClaimListQuery query, bool hasMore, string? cursor)
    {
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = string.IsNullOrWhiteSpace(query.FilterType) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["type"] = query.FilterType };
        var queryMeta = new ApiQueryMeta(filters, [query.Sort], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<T>(data, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
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
}
