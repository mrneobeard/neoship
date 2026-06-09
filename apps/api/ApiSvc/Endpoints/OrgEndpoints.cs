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

        group.MapGet("/service-accounts", ListServiceAccountsAsync);
        group.MapPost("/service-accounts", CreateServiceAccountAsync);
        group.MapPatch("/service-accounts/{serviceAccountId:guid}", UpdateServiceAccountAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/disable", DisableServiceAccountAsync);

        group.MapGet("/service-accounts/{serviceAccountId:guid}/api-keys", ListServiceAccountApiKeysAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys", CreateServiceAccountApiKeyAsync);
        group.MapPost("/service-accounts/{serviceAccountId:guid}/api-keys/{apiKeyId:guid}/revoke", RevokeServiceAccountApiKeyAsync);

        return group;
    }

    private static async Task<Organization?> ResolveOrgAsync(string orgSlug, ShipDb db, CancellationToken ct)
    {
        return await db.Orgs.FirstOrDefaultAsync(o => o.Slug == orgSlug, ct);
    }

    public record ServiceAccountResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);

    private static async Task<Results<Ok<List<ServiceAccountResponse>>, NotFound>> ListServiceAccountsAsync(
        string orgSlug,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var accounts = await store.ListAsync(org.Id, ct);

        var result = accounts.Select(s => new ServiceAccountResponse(
            s.Id, s.Name, s.Description, s.CreatedAt, s.UpdatedAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    public record CreateServiceAccountRequest(string Name, string? Description);

    private static async Task<Results<Created<ServiceAccountResponse>, NotFound>> CreateServiceAccountAsync(
        string orgSlug,
        [FromBody] CreateServiceAccountRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var user = await MeEndpoints.AuthenticateAsync(httpContext, sessions, ct);
        var createdBy = user?.Id ?? Guid.Empty;

        var sa = await store.CreateAsync(org.Id, createdBy, req.Name, req.Description, ct);

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{sa.Id}",
            new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt));
    }

    public record UpdateServiceAccountRequest(string? Name, string? Description);

    private static async Task<Results<Ok<ServiceAccountResponse>, NotFound>> UpdateServiceAccountAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromBody] UpdateServiceAccountRequest req,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var sa = await store.UpdateAsync(org.Id, serviceAccountId, req.Name, req.Description, ct);
        if (sa is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ServiceAccountResponse(sa.Id, sa.Name, sa.Description, sa.CreatedAt, sa.UpdatedAt));
    }

    private static async Task<Results<Ok, NotFound>> DisableServiceAccountAsync(
        string orgSlug,
        Guid serviceAccountId,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var success = await store.DisableAsync(org.Id, serviceAccountId, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok();
    }

    public record ServiceAccountApiKeyResponse(
        Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime? ExpiresAt);

    public record CreateServiceAccountApiKeyResponse(
        Guid Id, string Name, string PlaintextKey, DateTime CreatedAt);

    private static async Task<Results<Ok<List<ServiceAccountApiKeyResponse>>, NotFound>> ListServiceAccountApiKeysAsync(
        string orgSlug,
        Guid serviceAccountId,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
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

    private static async Task<Results<Created<CreateServiceAccountApiKeyResponse>, NotFound>> CreateServiceAccountApiKeyAsync(
        string orgSlug,
        Guid serviceAccountId,
        [FromBody] CreateServiceAccountApiKeyRequest req,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
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

        return TypedResults.Created(
            $"/api/v1/orgs/{orgSlug}/service-accounts/{serviceAccountId}/api-keys/{apiKey.Id}",
            new CreateServiceAccountApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt));
    }

    private static async Task<Results<Ok, NotFound>> RevokeServiceAccountApiKeyAsync(
        string orgSlug,
        Guid serviceAccountId,
        Guid apiKeyId,
        ShipDb db,
        ServiceAccountStore store,
        CancellationToken ct)
    {
        var org = await ResolveOrgAsync(orgSlug, db, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        var success = await store.RevokeApiKeyAsync(apiKeyId, serviceAccountId, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok();
    }
}
