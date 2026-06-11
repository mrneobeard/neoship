using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization permission catalog endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapPermissionEndpoints();
/// </code>
/// </example>
public static class PermissionEndpoints
{
    /// <summary>
    /// Maps current-organization permission catalog endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/v1/permissions", ListPermissionsAsync);
        return routes;
    }

    private sealed record PermissionDefinitionResponse(string Key, string Resource, string Action, string Description, List<PermissionScopeKind> AllowedScopes);

    private sealed record ParsedPermissionListQuery(int Limit, int Offset, string? FilterResource, string? FilterAction, Dictionary<string, string[]> Errors);

    private static async Task<IResult> ListPermissionsAsync(
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
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.roles", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var query = ParsePermissionListQuery(httpContext, limit, cursor, filterResource, filterAction);
        if (query.Errors.Count > 0)
        {
            return ValidationError(httpContext, query.Errors);
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
        var pagination = new ApiPagination(query.Limit, hasMore ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, hasMore);
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(query.FilterResource))
        {
            filters["resource"] = query.FilterResource;
        }

        if (!string.IsNullOrWhiteSpace(query.FilterAction))
        {
            filters["action"] = query.FilterAction;
        }

        var queryMeta = new ApiQueryMeta(filters, ["resource", "action"], [], query.Limit, cursor);
        return TypedResults.Ok(new ApiCollectionEnvelope<PermissionDefinitionResponse>(result, pagination, ApiMeta.FromHttpContext(httpContext, queryMeta)));
    }

    private static PermissionDefinitionResponse ToPermissionDefinitionResponse(PermissionDefinition definition)
        => new(definition.Key.ToString(), definition.Key.Resource, definition.Key.Action, definition.Description, definition.AllowedScopes.ToList());

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

        if (filterResource is { Length: > 160 })
        {
            errors["filter.resource"] = ["Resource filter must be 160 characters or fewer."];
        }

        if (filterAction is { Length: > 160 })
        {
            errors["filter.action"] = ["Action filter must be 160 characters or fewer."];
        }

        return new ParsedPermissionListQuery(resolvedLimit, offset, filterResource, filterAction, errors);
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

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });
}
