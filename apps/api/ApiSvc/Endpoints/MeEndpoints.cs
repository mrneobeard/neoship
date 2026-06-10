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
    private const double DefaultStepUpWindowMinutes = 15;

    /// <summary>
    /// The HTTP context item key that stores the authenticated user API key identifier.
    /// </summary>
    public const string UserApiKeyItemKey = "neoship.user_api_key_id";

    public static RouteGroupBuilder MapMeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/me");

        group.MapGet("/", GetMeAsync);
        group.MapDelete("/", DeleteMeAsync);
        group.MapPost("/orgs/{orgId:guid}/switch", SwitchOrgAsync);
        group.MapGet("/sessions", GetSessionsAsync);
        group.MapPost("/sessions/{sessionId:guid}/revoke", RevokeSessionAsync);

        group.MapGet("/api-keys", GetApiKeysAsync);
        group.MapPost("/api-keys", CreateApiKeyAsync);
        group.MapPost("/api-keys/{apiKeyId:guid}/rotate", RotateApiKeyAsync);
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

    private static async Task<(User? User, UserSession? Session)> AuthenticateSessionAsync(HttpContext httpContext, SessionStore sessions, CancellationToken ct)
    {
        var rawToken = httpContext.Request.Cookies[AuthEndpoints.SessionCookieName];
        if (rawToken is null)
        {
            return (null, null);
        }

        var session = await sessions.ValidateSessionAsync(rawToken, ct);
        if (session is null)
        {
            return (null, null);
        }

        var user = await httpContext.RequestServices.GetRequiredService<ShipDb>()
            .Users.FirstOrDefaultAsync(u => u.Id == session.UserId, ct);

        return (user, session);
    }

    private static async Task<(User? User, IResult? Failure)> RequireRecentSessionAsync(
        HttpContext httpContext,
        SessionStore sessions,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, session) = await AuthenticateSessionAsync(httpContext, sessions, ct);
        if (user is null || session is null)
        {
            return (null, TypedResults.Unauthorized());
        }

        if (!configuration.GetValue("Auth:StepUp:Enabled", true))
        {
            return (user, null);
        }

        var windowMinutes = configuration.GetValue("Auth:StepUp:WindowMinutes", DefaultStepUpWindowMinutes);
        var window = TimeSpan.FromMinutes(windowMinutes <= 0 ? DefaultStepUpWindowMinutes : windowMinutes);
        var lastVerifiedAt = session.MfaVerifiedAt is not null && session.MfaVerifiedAt > session.CreatedAt
            ? session.MfaVerifiedAt.Value
            : session.CreatedAt;

        if (lastVerifiedAt < DateTime.UtcNow.Subtract(window))
        {
            return (null, StepUpRequired(httpContext));
        }

        return (user, null);
    }

    private static IResult StepUpRequired(HttpContext httpContext)
        => Error(httpContext, StatusCodes.Status403Forbidden, "step_up_required", "Recent authentication is required for this operation.");

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

    private static async Task<IResult> DeleteMeAsync(
        HttpContext httpContext,
        SessionStore sessions,
        ShipDb db,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var now = DateTime.UtcNow;
        actor.StatusId = UserStatus.Deleted.Id;

        await db.UserSessions
            .Where(x => x.UserId == actor.Id && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x
                .SetProperty(s => s.RevokedAt, now)
                .SetProperty(s => s.RevokeReason, "user_deleted"), ct);
        await db.UserApiKeys
            .Where(x => x.UserId == actor.Id && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(k => k.RevokedAt, now), ct);
        await db.OrganizationMemberships
            .Where(x => x.UserId == actor.Id && x.DeletedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(m => m.DeletedAt, now), ct);

        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("auth.user.delete", actor.OrgId, actor.Id, "user.delete", targetType: "user", targetId: actor.Id.ToString(), ct: ct);
        return TypedResults.Ok();
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

    public record RotateApiKeyRequest(string? Name, string? Description, string? ScopesJson, DateTime? ExpiresAt);

    private static async Task<IResult> CreateApiKeyAsync(
        [FromBody] CreateApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        ShipDb db,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateCreateApiKey(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (plaintextKey, apiKey) = apiKeys.GenerateUserApiKey(
            actor.Id, req.Name, req.Description, req.ScopesJson, req.ExpiresAt);

        db.UserApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("auth.api_key.create", actor.OrgId, actor.Id, "api_key.create", targetType: "user_api_key", targetId: apiKey.Id.ToString(), ct: ct);

        return TypedResults.Created($"/api/v1/me/api-keys/{apiKey.Id}",
            new CreateApiKeyResponse(apiKey.Id, apiKey.Name, plaintextKey, apiKey.CreatedAt));
    }

    private static async Task<IResult> RevokeApiKeyAsync(
        Guid apiKeyId,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var success = await apiKeys.RevokeUserApiKeyAsync(apiKeyId, actor.Id, ct);
        if (!success)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("auth.api_key.revoke", actor.OrgId, actor.Id, "api_key.revoke", targetType: "user_api_key", targetId: apiKeyId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> RotateApiKeyAsync(
        Guid apiKeyId,
        [FromBody] RotateApiKeyRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        ApiKeyStore apiKeys,
        ShipDb db,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateRotateApiKey(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var oldKey = await db.UserApiKeys.FirstOrDefaultAsync(x => x.Id == apiKeyId && x.UserId == actor.Id && x.DeletedAt == null && x.RevokedAt == null, ct);
        if (oldKey is null || oldKey.ExpiresAt <= DateTime.UtcNow)
        {
            return TypedResults.NotFound();
        }

        var (plaintextKey, newKey) = apiKeys.GenerateUserApiKey(
            actor.Id,
            string.IsNullOrWhiteSpace(req.Name) ? oldKey.Name : req.Name.Trim(),
            req.Description ?? oldKey.Description,
            req.ScopesJson ?? oldKey.ScopesJson,
            req.ExpiresAt ?? oldKey.ExpiresAt);

        oldKey.RevokedAt = DateTime.UtcNow;
        db.UserApiKeys.Add(newKey);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync("auth.api_key.rotate", actor.OrgId, actor.Id, "api_key.rotate", targetType: "user_api_key", targetId: oldKey.Id.ToString(), ct: ct);
        return TypedResults.Created($"/api/v1/me/api-keys/{newKey.Id}", new CreateApiKeyResponse(newKey.Id, newKey.Name, plaintextKey, newKey.CreatedAt));
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
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateOptionalName(req.Name);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var (factor, secret) = await mfa.StartTotpAsync(actor.Id, req.Name, ct);
        var issuer = Uri.EscapeDataString("NeoShip");
        var label = Uri.EscapeDataString($"NeoShip:{actor.Email}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={issuer}&digits=6&period=30";

        await audit.RecordAsync("auth.mfa.totp.start", actor.OrgId, actor.Id, "mfa.totp.start", targetType: "mfa_factor", targetId: factor.Id.ToString(), ct: ct);
        return TypedResults.Ok(new StartTotpResponse(factor.Id, secret, uri));
    }

    private static async Task<IResult> ConfirmTotpAsync(
        [FromBody] ConfirmTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        AuditStore audit,
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
        if (!confirmed)
        {
            await audit.RecordAsync("auth.mfa.totp.confirm_failed", user.OrgId, user.Id, "mfa.totp.confirm", targetType: "mfa_factor", targetId: req.FactorId.ToString(), ct: ct);
            return TypedResults.NotFound();
        }

        var sessionId = httpContext.RequestServices.GetRequiredService<RequestContext>().SessionId;
        if (sessionId is not null)
        {
            await sessions.MarkMfaVerifiedAsync(sessionId.Value, ct);
        }

        await audit.RecordAsync("auth.mfa.totp.confirm", user.OrgId, user.Id, "mfa.totp.confirm", targetType: "mfa_factor", targetId: req.FactorId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> DisableTotpAsync(
        [FromBody] DisableTotpRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateFactorId(req.FactorId);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var disabled = await mfa.DisableTotpAsync(actor.Id, req.FactorId, ct);
        if (!disabled)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("auth.mfa.totp.disable", actor.OrgId, actor.Id, "mfa.totp.disable", targetType: "mfa_factor", targetId: req.FactorId.ToString(), ct: ct);
        return TypedResults.Ok();
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync(
        [FromBody] RegenerateRecoveryCodesRequest req,
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateRecoveryCodeCount(req.Count);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var codes = await mfa.RegenerateRecoveryCodesAsync(actor.Id, req.Count ?? 10, ct);
        await audit.RecordAsync("auth.mfa.recovery_codes.regenerate", actor.OrgId, actor.Id, "mfa.recovery_codes.regenerate", ct: ct);
        return TypedResults.Ok(new RegenerateRecoveryCodesResponse(codes));
    }

    private static async Task<IResult> RevokeRecoveryCodesAsync(
        HttpContext httpContext,
        SessionStore sessions,
        MfaStore mfa,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        await mfa.RevokeRecoveryCodesAsync(actor.Id, ct);
        await audit.RecordAsync("auth.mfa.recovery_codes.revoke", actor.OrgId, actor.Id, "mfa.recovery_codes.revoke", ct: ct);
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

    private static async Task<IResult> UnlinkExternalIdentityAsync(
        Guid externalIdentityId,
        HttpContext httpContext,
        SessionStore sessions,
        SsoStore sso,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var result = await sso.UnlinkExternalIdentityAsync(actor.Id, externalIdentityId, ct);
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
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 64)
        {
            errors["name"] = ["Name is required and must be 64 characters or fewer."];
        }

        if (req.Description is not null && req.Description.Length > 256)
        {
            errors["description"] = ["Description must be 256 characters or fewer when provided."];
        }

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

    private static Dictionary<string, string[]> ValidateRotateApiKey(RotateApiKeyRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        if (req.Name is not null && (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 64))
        {
            errors["name"] = ["Name must be 64 characters or fewer when provided."];
        }

        if (req.Description is not null && req.Description.Length > 256)
        {
            errors["description"] = ["Description must be 256 characters or fewer when provided."];
        }

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
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var validation = ValidateFinishPasskeyRegistration(req);
        if (validation.Count > 0)
        {
            return ValidationError(httpContext, validation);
        }

        var options = challenges.TakeRegistration(req.ChallengeId, actor.Id);
        if (options is null)
        {
            return TypedResults.NotFound();
        }

        var factor = await passkeys.FinishRegistrationAsync(actor.Id, req.Name, options, req.Response, ct);
        await audit.RecordAsync("auth.passkey.register", actor.OrgId, actor.Id, "passkey.register", targetType: "mfa_factor", targetId: factor.Id.ToString(), ct: ct);
        return TypedResults.Ok(new PasskeyResponse(factor.Id, factor.Name, factor.CreatedAt, factor.LastUsedAt));
    }

    private static async Task<IResult> RevokePasskeyAsync(
        Guid factorId,
        HttpContext httpContext,
        SessionStore sessions,
        PasskeyStore passkeys,
        AuditStore audit,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var (user, failure) = await RequireRecentSessionAsync(httpContext, sessions, configuration, ct);
        if (failure is not null)
        {
            return failure;
        }

        var actor = user!;

        var revoked = await passkeys.RevokeAsync(actor.Id, factorId, ct);
        if (!revoked)
        {
            return TypedResults.NotFound();
        }

        await audit.RecordAsync("auth.passkey.revoke", actor.OrgId, actor.Id, "passkey.revoke", targetType: "mfa_factor", targetId: factorId.ToString(), ct: ct);
        return TypedResults.Ok();
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
