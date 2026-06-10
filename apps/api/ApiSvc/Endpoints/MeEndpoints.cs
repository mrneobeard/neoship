using System.Security.Cryptography;
using System.Text.Json;

using Fido2NetLib;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

public static class MeEndpoints
{
    /// <summary>
    /// The HTTP context item key that stores the authenticated user API key identifier.
    /// </summary>
    public const string UserApiKeyItemKey = "neoship.user_api_key_id";

    public static RouteGroupBuilder MapMeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/me");

        group.MapGet("/", GetMeAsync);
        group.MapPost("/orgs/{orgId:guid}/switch", SwitchOrgAsync);
        group.MapGet("/sessions", GetSessionsAsync);
        group.MapPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync);

        group.MapGet("/api-keys", GetApiKeysAsync);
        group.MapPost("/api-keys", CreateApiKeyAsync);
        group.MapPost("/api-keys/{apiKeyId:guid}/revoke", RevokeApiKeyAsync);

        group.MapGet("/external-identities", GetExternalIdentitiesAsync);
        group.MapDelete("/external-identities/{externalIdentityId:guid}", UnlinkExternalIdentityAsync);
        group.MapPost("/invites/accept", AcceptInviteAsync);

        group.MapPost("/mfa/totp/start", StartTotpAsync);
        group.MapPost("/mfa/totp/confirm", ConfirmTotpAsync);
        group.MapPost("/mfa/totp/disable", DisableTotpAsync);
        group.MapPost("/mfa/recovery-codes/regenerate", RegenerateRecoveryCodesAsync);
        group.MapDelete("/mfa/recovery-codes", RevokeRecoveryCodesAsync);

        group.MapGet("/passkeys", GetPasskeysAsync);
        group.MapPost("/passkeys/begin-registration", BeginPasskeyRegistrationAsync);
        group.MapPost("/passkeys/finish-registration", FinishPasskeyRegistrationAsync);
        group.MapPost("/passkeys/{factorId:guid}/revoke", RevokePasskeyAsync);

        return group;
    }

    /// <summary>
    /// Authenticates a user from a session cookie or bearer user API key.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="sessions">The session store.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated <see cref="User"/>, or <see langword="null"/>.</returns>
    public static async Task<User?> AuthenticateAsync(HttpContext httpContext, SessionStore sessions, CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[AuthEndpoints.SessionCookieName];
        if (rawToken is not null)
        {
            var session = await sessions.ValidateSessionAsync(rawToken, ct);
            if (session is not null)
            {
                return await httpContext.RequestServices.GetRequiredService<ShipDb>()
                    .Users.FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
            }

            return null;
        }

        var bearerToken = ReadBearerToken(httpContext.Request);
        if (bearerToken is null)
        {
            return null;
        }

        var apiKeys = httpContext.RequestServices.GetRequiredService<ApiKeyStore>();
        var apiKey = await apiKeys.AuthenticateUserApiKeyAsync(bearerToken, ct);
        if (apiKey?.User is null)
        {
            return null;
        }

        httpContext.Items[UserApiKeyItemKey] = apiKey.Id;
        return apiKey.User;
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

    public record UserResponse(Guid Id, string Email, string Name, string? AvatarUrl);

    private sealed record OrganizationResponse(Guid Id, string Name, string Slug);

    private static async Task<Results<Ok<UserResponse>, UnauthorizedHttpResult>> GetMeAsync(
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new UserResponse(user.Id, user.Email, user.Name, user.AvatarUrl));
    }

    private static async Task<Results<Ok<OrganizationResponse>, UnauthorizedHttpResult, NotFound>> SwitchOrgAsync(
        Guid orgId,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationStore organizations,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var org = await organizations.SwitchCurrentAsync(user.Id, orgId, ct);
        if (org is null)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.switch", org.Id, user.Id, "org.switch", targetType: "org", targetId: org.Id.ToString(), ct: ct);
        return TypedResults.Ok(new OrganizationResponse(org.Id, org.Name, org.Slug));
    }

    public record SessionResponse(
        Guid Id,
        string? IpAddress,
        string? UserAgent,
        DateTime CreatedAt,
        DateTime LastUsedAt,
        DateTime ExpiresAt);

    private static async Task<Results<Ok<List<SessionResponse>>, UnauthorizedHttpResult>> GetSessionsAsync(
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var userSessions = await sessions.ListSessionsAsync(user.Id, ct);

        var result = userSessions.Select(s => new SessionResponse(
            s.Id,
            s.IpAddress,
            s.UserAgent,
            s.CreatedAt,
            s.LastUsedAt,
            s.ExpiresAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokeSessionAsync(
        Guid sessionId,
        HttpContext httpContext,
        SessionStore sessions,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var db = httpContext.RequestServices.GetRequiredService<ShipDb>();
        var session = await db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id, ct);

        if (session is null)
        {
            return TypedResults.NotFound();
        }

        await sessions.RevokeSessionAsync(sessionId, "user_revoked", ct);

        return TypedResults.Ok();
    }

    public record ApiKeyResponse(
        Guid Id,
        string Name,
        string? Description,
        string? ScopesJson,
        DateTime CreatedAt,
        DateTime? LastUsedAt,
        DateTime? ExpiresAt);

    public record CreateApiKeyResponse(
        Guid Id,
        string Name,
        string PlaintextKey,
        DateTime CreatedAt);

    private static async Task<Results<Ok<List<ApiKeyResponse>>, UnauthorizedHttpResult>> GetApiKeysAsync(
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var keys = await apiKeys.ListUserApiKeysAsync(user.Id, ct);

        var result = keys.Select(k => new ApiKeyResponse(
            k.Id, k.Name, k.Description, k.ScopesJson, k.CreatedAt, k.LastUsedAt, k.ExpiresAt
        )).ToList();

        return TypedResults.Ok(result);
    }

    public record CreateApiKeyRequest(string Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private static async Task<IResult> CreateApiKeyAsync(
        [FromBody] CreateApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        ShipDb db,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateCreateApiKey(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(
            user.Id, req.Name, req.Description, req.ScopesJson, req.ExpiresAt);

        db.UserApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/v1/me/api-keys/{apiKey.Id}",
            new CreateApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokeApiKeyAsync(
        Guid apiKeyId,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var success = await apiKeys.RevokeUserApiKeyAsync(apiKeyId, user.Id, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok();
    }

    private sealed record StartTotpRequest(string? Name);

    private sealed record StartTotpResponse(Guid FactorId, string Secret, string OtpAuthUri);

    private sealed record ConfirmTotpRequest(Guid FactorId, string Code);

    private sealed record DisableTotpRequest(Guid FactorId);

    private sealed record RegenerateRecoveryCodesRequest(int? Count);

    private sealed record RegenerateRecoveryCodesResponse(IReadOnlyList<string> Codes);

    private sealed record PasskeyResponse(Guid Id, string Name, DateTime CreatedAt, DateTime? LastUsedAt);

    private sealed record BeginPasskeyRegistrationResponse(Guid ChallengeId, string OptionsJson);

    private sealed record FinishPasskeyRegistrationRequest(Guid ChallengeId, string? Name, AuthenticatorAttestationRawResponse Response);

    private sealed record ExternalIdentityResponse(Guid Id, long ProviderId, string? ProviderName, string Subject, string? Email, DateTime CreatedAt, DateTime? LastUsedAt);

    private sealed record AcceptInviteRequest(string Token);

    private static async Task<IResult> StartTotpAsync(
        [FromBody] StartTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateOptionalName(req.Name);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (factor, secret) = await mfa.StartTotpAsync(user.Id, req.Name, ct);
        var issuer = Uri.EscapeDataString("NeoShip");
        var label = Uri.EscapeDataString($"NeoShip:{user.Email}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits=6&period=30";

        return TypedResults.Ok(new StartTotpResponse(factor.Id, secret, uri));
    }

    private static async Task<IResult> ConfirmTotpAsync(
        [FromBody] ConfirmTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateTotpConfirm(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var confirmed = await mfa.ConfirmTotpAsync(user.Id, req.FactorId, req.Code, ct);
        return confirmed ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<IResult> DisableTotpAsync(
        [FromBody] DisableTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateFactorId(req.FactorId);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var disabled = await mfa.DisableTotpAsync(user.Id, req.FactorId, ct);
        return disabled ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync(
        [FromBody] RegenerateRecoveryCodesRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateRecoveryCodeCount(req.Count);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var codes = await mfa.RegenerateRecoveryCodesAsync(user.Id, req.Count ?? 10, ct);
        return TypedResults.Ok(new RegenerateRecoveryCodesResponse(codes));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> RevokeRecoveryCodesAsync(
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        await mfa.RevokeRecoveryCodesAsync(user.Id, ct);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<List<PasskeyResponse>>, UnauthorizedHttpResult>> GetPasskeysAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var factors = await passkeys.ListAsync(user.Id, ct);
        var response = factors.Select(x => new PasskeyResponse(x.Id, x.Name, x.CreatedAt, x.LastUsedAt)).ToList();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<List<ExternalIdentityResponse>>, UnauthorizedHttpResult>> GetExternalIdentitiesAsync(
        HttpContext httpContext,
        SessionStore sessions,
        SsoStore sso,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var identities = await sso.ListExternalIdentitiesAsync(user.Id, ct);
        return TypedResults.Ok(identities.Select(x => new ExternalIdentityResponse(
            x.Id,
            x.ProviderId,
            x.Provider?.Name,
            x.Subject,
            x.Email,
            x.CreatedAt,
            x.LastUsedAt)).ToList());
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound, StatusCodeHttpResult, Conflict<string>>> UnlinkExternalIdentityAsync(
        Guid externalIdentityId,
        HttpContext httpContext,
        SessionStore sessions,
        SsoStore sso,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await sso.UnlinkExternalIdentityAsync(user.Id, externalIdentityId, ct);
        return result switch
        {
            SsoExternalIdentityUnlinkResult.Success => TypedResults.Ok(),
            SsoExternalIdentityUnlinkResult.NotFound => TypedResults.NotFound(),
            SsoExternalIdentityUnlinkResult.PolicyDenied => TypedResults.StatusCode(StatusCodes.Status403Forbidden),
            _ => TypedResults.Conflict("Configure another sign-in method before unlinking this identity."),
        };
    }

    private static async Task<IResult> AcceptInviteAsync(
        [FromBody] AcceptInviteRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        OrganizationInviteStore invites,
        AuditStore audit,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateAcceptInvite(req);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var invite = await invites.AcceptAsync(req.Token, user.Id, ct);
        if (invite is null)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("org.invites.accept", invite.OrgId, user.Id, "org.invite.accept", targetType: "organization_invite", targetId: invite.Id.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static IResult ValidationError(HttpContext httpContext, Dictionary<string, string[]> fields)
        => Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = fields });

    private static Dictionary<string, string[]> ValidateCreateApiKey(CreateApiKeyRequest req)
    {
        var errors = ValidateNameDescription(req.Name, req.Description, requireName: true);

        if (!IsValidJsonArray(req.ScopesJson))
        {
            errors["scopesJson"] = ["Scopes JSON must be a valid JSON array when provided."];
        }

        if (req.ExpiresAt is not null && req.ExpiresAt <= DateTime.UtcNow)
        {
            errors["expiresAt"] = ["Expiration must be in the future when provided."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateOptionalName(string? name)
        => ValidateNameDescription(name, description: null, requireName: false);

    private static Dictionary<string, string[]> ValidateNameDescription(string? name, string? description, bool requireName)
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

    private static Dictionary<string, string[]> ValidateTotpConfirm(ConfirmTotpRequest req)
    {
        var errors = ValidateFactorId(req.FactorId);
        if (string.IsNullOrWhiteSpace(req.Code) || req.Code.Length != 6 || !req.Code.All(char.IsAsciiDigit))
        {
            errors["code"] = ["Code must be a 6 digit TOTP code."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateFactorId(Guid factorId)
    {
        var errors = new Dictionary<string, string[]>();
        if (factorId == Guid.Empty)
        {
            errors["factorId"] = ["Factor ID is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRecoveryCodeCount(int? count)
    {
        var errors = new Dictionary<string, string[]>();
        if (count is < 1 or > 24)
        {
            errors["count"] = ["Count must be between 1 and 24 when provided."];
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

    private static Dictionary<string, string[]> ValidateAcceptInvite(AcceptInviteRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(req.Token))
        {
            errors["token"] = ["Token is required."];
        }

        return errors;
    }

    private static async Task<Results<Ok<BeginPasskeyRegistrationResponse>, UnauthorizedHttpResult>> BeginPasskeyRegistrationAsync(
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var options = await passkeys.BeginRegistrationAsync(user, ct);
        var challengeId = httpContext.RequestServices.GetRequiredService<PasskeyChallengeStore>().StoreRegistration(user.Id, options);
        return TypedResults.Ok(new BeginPasskeyRegistrationResponse(challengeId, options.ToJson()));
    }

    private static async Task<IResult> FinishPasskeyRegistrationAsync(
        [FromBody] FinishPasskeyRegistrationRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        PasskeyChallengeStore challenges,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var validation = ValidateFinishPasskeyRegistration(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var options = challenges.TakeRegistration(req.ChallengeId, user.Id);
        if (options is null)
        {
            return TypedResults.NotFound();
        }

        var factor = await passkeys.FinishRegistrationAsync(user.Id, req.Name, options, req.Response, ct);
        return TypedResults.Ok(new PasskeyResponse(factor.Id, factor.Name, factor.CreatedAt, factor.LastUsedAt));
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult, NotFound>> RevokePasskeyAsync(
        Guid factorId,
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        CancellationToken ct)
    {
        var user = await AuthenticateAsync(httpContext, sessions, ct);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var revoked = await passkeys.RevokeAsync(user.Id, factorId, ct);
        return revoked ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static Dictionary<string, string[]> ValidateFinishPasskeyRegistration(FinishPasskeyRegistrationRequest req)
    {
        var errors = ValidateOptionalName(req.Name);
        if (req.ChallengeId == Guid.Empty)
        {
            errors["challengeId"] = ["Challenge ID is required."];
        }

        if (req.Response is null)
        {
            errors["response"] = ["Passkey attestation response is required."];
        }

        return errors;
    }
}
