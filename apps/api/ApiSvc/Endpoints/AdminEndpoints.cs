using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NeoShip.ApiSvc.Lib.Iam;
using NeoShip.ApiSvc.Models;
using NeoShip.ApiSvc.Stores;
using NeoShip.Data.Model;

namespace NeoShip.ApiSvc.Endpoints;

/// <summary>
/// Maps root-token protected administrative endpoints for bootstrap tooling.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// app.MapAdminEndpoints();
/// </code>
/// </remarks>
public static class AdminEndpoints
{
    private const string RootTokenHeaderName = "X-NeoShip-Root-Token";

    /// <summary>
    /// Maps root-token protected administrative routes.
    /// </summary>
    /// <param name="routes">The route builder.</param>
    /// <returns>The mapped <see cref="RouteGroupBuilder"/>.</returns>
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin");
        group.MapPost("/bootstrap-admin", BootstrapAdminAsync);
        return group;
    }

    private sealed record BootstrapAdminRequest(string Email, string Password, string? Name, string? Org, string? OrgName);

    private sealed record BootstrapAdminResponse(Guid UserId, Guid OrgId, string Email, string OrgSlug);

    private static async Task<IResult> BootstrapAdminAsync(
        [FromBody] BootstrapAdminRequest req,
        HttpContext httpContext,
        IConfiguration configuration,
        ShipDb db,
        AuditStore audit,
        CancellationToken ct)
    {
        if (!RootTokenMatches(httpContext, configuration))
        {
            return Error(httpContext, StatusCodes.Status403Forbidden, "permission_denied", "Permission denied.");
        }

        var validation = Validate(req);
        if (validation.Count > 0)
        {
            return Error(httpContext, StatusCodes.Status422UnprocessableEntity, "validation_failed", "Validation failed.", new Dictionary<string, object?> { ["fields"] = validation });
        }

        var now = DateTime.UtcNow;
        var email = req.Email.Trim();
        var name = string.IsNullOrWhiteSpace(req.Name) ? email : req.Name.Trim();
        var orgSlug = OrganizationStore.NormalizeSlug(req.Org ?? "default");
        var orgName = string.IsNullOrWhiteSpace(req.OrgName) ? "Default" : req.OrgName.Trim();

        var org = await db.Orgs.FirstOrDefaultAsync(x => x.Slug == orgSlug, ct);
        if (org is null)
        {
            org = new Organization
            {
                Id = orgSlug == "default" ? Constants.DefaultOrganizationId : Guid.CreateVersion7(),
                Name = orgName,
                NameUpcase = orgName.ToUpperInvariant(),
                Slug = orgSlug,
                StatusId = OrganizationStatus.Active.Id,
                TenantModeId = TenantMode.Multi.Id,
                OrganizationPlanId = 1,
                CreatedAt = now,
            };
            db.Orgs.Add(org);
        }

        var emailUpcase = email.ToUpperInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.EmailUpcase == emailUpcase, ct);
        if (user is null)
        {
            user = new User(Guid.CreateVersion7(), email, name)
            {
                OrgId = org.Id,
                StatusId = UserStatus.Active.Id,
            };
            db.Users.Add(user);
            db.UserEmails.Add(new UserEmail
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                Email = email,
                EmailUpcase = emailUpcase,
                EmailDigest = TokenGenerator.ComputeDigestBase64(email),
                StatusId = UserEmailStatus.Active.Id,
                CreatedBy = user.Id,
                CreatedAt = now,
                VerifiedAt = now,
            });
        }

        user.Name = name;
        user.NameUpcase = name.ToUpperInvariant();
        user.OrgId = org.Id;
        user.StatusId = UserStatus.Active.Id;

        var passwordAuth = await db.UserPasswordAuths.FirstOrDefaultAsync(x => x.UserId == user.Id, ct);
        if (passwordAuth is null)
        {
            db.UserPasswordAuths.Add(new UserPasswordAuth
            {
                UserId = user.Id,
                PasswordHash = PasswordHashing.Hash(req.Password),
                CreatedAt = now,
            });
        }
        else
        {
            passwordAuth.PasswordHash = PasswordHashing.Hash(req.Password);
            passwordAuth.PasswordChangedAt = now;
            passwordAuth.FailedAttempts = 0;
            passwordAuth.LockedUntil = null;
        }

        var membership = await db.OrganizationMemberships.FirstOrDefaultAsync(x => x.OrgId == org.Id && x.UserId == user.Id, ct);
        if (membership is null)
        {
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                OrgId = org.Id,
                UserId = user.Id,
                CreatedAt = now,
                AcceptedAt = now,
            });
        }
        else
        {
            membership.DeletedAt = null;
            membership.AcceptedAt = membership.AcceptedAt == default ? now : membership.AcceptedAt;
        }

        await BuiltInRoleStore.AssignAsync(db, org.Id, org.Slug, user.Id, BuiltInRoleStore.OwnerRoleName);
        foreach (var permission in BootstrapPermissions())
        {
            if (!await db.UserClaims.AnyAsync(x => x.UserId == user.Id && x.Type == permission && x.Value == $"organization:{org.Slug}", ct))
            {
                db.UserClaims.Add(new UserClaim
                {
                    UserId = user.Id,
                    Type = permission,
                    Value = $"organization:{org.Slug}",
                });
            }
        }

        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("admin.bootstrap", org.Id, user.Id, "admin.bootstrap", targetType: "user", targetId: user.Id.ToString(), ct: ct);

        return TypedResults.Ok(Envelope(httpContext, new BootstrapAdminResponse(user.Id, org.Id, user.Email, org.Slug)));
    }

    private static bool RootTokenMatches(HttpContext httpContext, IConfiguration configuration)
    {
        var configured = configuration["NEO_ROOT_TOKEN"] ?? configuration["Auth:RootToken"];
        var provided = httpContext.Request.Headers[RootTokenHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(configured) || string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        return CryptographicOperations.FixedTimeEquals(configuredHash, providedHash);
    }

    private static Dictionary<string, string[]> Validate(BootstrapAdminRequest req)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsValidEmail(req.Email?.Trim() ?? string.Empty))
        {
            errors["email"] = ["Email must be a valid email address and 320 characters or fewer."];
        }

        if (string.IsNullOrEmpty(req.Password) || req.Password.Length is < 12 or > 256)
        {
            errors["password"] = ["Password must be 12 to 256 characters."];
        }

        if (req.Name is not null && (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length > 160))
        {
            errors["name"] = ["Name must be 160 characters or fewer when provided."];
        }

        var orgSlug = OrganizationStore.NormalizeSlug(req.Org ?? "default");
        if (!IsValidSlug(orgSlug))
        {
            errors["org"] = ["Organization slug must be 3-64 lowercase letters, numbers, or hyphens, and start and end with a letter or number."];
        }

        if (req.OrgName is not null && (string.IsNullOrWhiteSpace(req.OrgName) || req.OrgName.Trim().Length > 160))
        {
            errors["orgName"] = ["Organization name must be 160 characters or fewer when provided."];
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

    private static bool IsValidSlug(string slug)
    {
        if (slug.Length is < 3 or > 64 || !char.IsAsciiLetterOrDigit(slug[0]) || !char.IsAsciiLetterOrDigit(slug[^1]))
        {
            return false;
        }

        return slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
    }

    private static IReadOnlyList<string> BootstrapPermissions()
        =>
        [
            "org.settings.read",
            "org.settings.write",
            "org.members.read",
            "org.members.write",
            "org.roles.read",
            "org.roles.write",
            "org.groups.read",
            "org.groups.write",
            "org.service_accounts.read",
            "org.service_accounts.write",
            "org.identity_providers.read",
            "org.identity_providers.write",
        ];

    private static IResult Error(HttpContext httpContext, int statusCode, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
        => TypedResults.Json(new ApiErrorEnvelope(new ApiError(code, message, details), ApiMeta.FromHttpContext(httpContext)), statusCode: statusCode);

    private static ApiEnvelope<T> Envelope<T>(HttpContext httpContext, T? data)
        => new(data, ApiMeta.FromHttpContext(httpContext));
}
