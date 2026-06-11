using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps current-organization identity-provider endpoints.
/// </summary>
/// <example>
/// <code>
/// app.MapIdentityProviderEndpoints();
/// </code>
/// </example>
public static class IdentityProviderEndpoints
{
    /// <summary>
    /// Maps current-organization identity-provider endpoints.
    /// </summary>
    /// <param name="routes">The endpoint route builder.</param>
    /// <returns>The mapped <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapIdentityProviderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/identity-providers");
        group.MapGet("", ListIdentityProvidersAsync);
        group.MapPost("", CreateIdentityProviderAsync);
        group.MapGet("/{providerId:long}", GetIdentityProviderAsync);
        group.MapPatch("/{providerId:long}", UpdateIdentityProviderAsync);
        group.MapPost("/{providerId:long}/enable", EnableIdentityProviderAsync);
        group.MapPost("/{providerId:long}/disable", DisableIdentityProviderAsync);
        return routes;
    }

    private sealed record IdentityProviderResponse(long Id, string Name, string ProviderType, string Status, string? IssuerUrl, string? ClientId, bool HasClientSecret, string? MetadataJson, DateTime CreatedAt, DateTime? UpdatedAt);

    private sealed record CreateIdentityProviderRequest(string Name, string? ProviderType, string? Preset, string? IssuerUrl, string? ClientId, string? ClientSecret, string? MetadataJson);

    private sealed record UpdateIdentityProviderRequest(string? Name, string? IssuerUrl, string? ClientId, string? ClientSecret, string? MetadataJson);

    private sealed record IdentityProviderPreset(UserIdentityProviderType ProviderType, string? IssuerUrl, string MetadataJson);

    private sealed record ParsedNamedListQuery(int Limit, int Offset, string? FilterName, string Sort, Dictionary<string, string[]> Errors);

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
        var data = page.Take(query.Limit).Select(ToIdentityProviderResponse).ToList();
        var pagination = new ApiPagination(query.Limit, page.Count > query.Limit ? EncodeCursor(query.Offset + query.Limit) : null, previousCursor: null, page.Count > query.Limit);
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
        return TypedResults.Created($"/api/v1/identity-providers/{provider.Id}", Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static async Task<IResult> GetIdentityProviderAsync(long providerId, HttpContext httpContext, SessionStore sessions, PermissionResolver permissions, ShipDb db, UserStore users, CancellationToken ct)
    {
        var auth = await CurrentOrgEndpointAuth.RequireAsync(httpContext, sessions, permissions, db, PermissionKey.Create("org.identity_providers", "read"), ct, allowServiceAccount: true);
        if (auth.Failure is not null)
        {
            return auth.Failure;
        }

        var provider = await users.GetIdentityProviderAsync(auth.Org.Id, providerId, ct);
        return provider is null ? NotFoundError(httpContext) : TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
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
            return NotFoundError(httpContext);
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
            return NotFoundError(httpContext);
        }

        await audit.RecordAsync($"org.identity_providers.{(active ? "enable" : "disable")}", auth.Org.Id, auth.User!.Id, $"identity_provider.{(active ? "enable" : "disable")}", targetType: "identity_provider", targetId: provider.Id.ToString(), ct: ct);
        return TypedResults.Ok(Envelope(httpContext, ToIdentityProviderResponse(provider)));
    }

    private static IdentityProviderResponse ToIdentityProviderResponse(UserIdentityProvider provider)
        => new(provider.Id, provider.Name, provider.ProviderType.Name, provider.Status.Name, provider.IssuerUrl, provider.ClientId, provider.ClientSecretEncrypted.Length > 0, provider.MetadataJson, provider.CreatedAt, provider.UpdatedAt);

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

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    private static IResult NotFoundError(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status404NotFound, "not_found", "Resource not found.");
}
