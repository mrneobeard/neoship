namespace NeoShip.Data.Model;

/// <summary>
/// Encodes and decodes permission grants to and from generic claim fields.
/// </summary>
/// <example>
/// <code>
/// var codec = new PermissionClaimCodec(registry);
/// var grant = new PermissionGrant(PermissionKey.Create("org.roles", "write"), PermissionScopeKind.Organization, "org_1");
/// var (type, value) = codec.Encode(grant);
/// </code>
/// </example>
public sealed class PermissionClaimCodec
{
    private readonly PermissionRegistry registry;

    /// <summary>
    /// Initializes a new <see cref="PermissionClaimCodec"/> instance.
    /// </summary>
    /// <param name="registry">The permission registry.</param>
    public PermissionClaimCodec(PermissionRegistry registry)
    {
        this.registry = registry;
    }

    /// <summary>
    /// Encodes a permission grant into claim fields.
    /// </summary>
    /// <param name="grant">The grant to encode.</param>
    /// <returns>The claim type and value.</returns>
    public (string Type, string Value) Encode(PermissionGrant grant)
    {
        var type = grant.Key.ToString();
        var value = grant.ScopeId is null
            ? grant.ScopeKind.ToString().ToLowerInvariant()
            : $"{grant.ScopeKind.ToString().ToLowerInvariant()}:{grant.ScopeId}";

        return (type, value);
    }

    /// <summary>
    /// Tries to decode a permission grant from claim fields.
    /// </summary>
    /// <param name="type">The claim type.</param>
    /// <param name="value">The claim value.</param>
    /// <param name="grant">The decoded grant when successful.</param>
    /// <returns><see langword="true"/> when the claim is a known permission grant; otherwise <see langword="false"/>.</returns>
    public bool TryDecode(string type, string value, out PermissionGrant grant)
    {
        grant = default;

        if (!PermissionKey.TryParse(type, out var key))
        {
            return false;
        }

        if (!this.registry.TryGet(key, out var definition))
        {
            return false;
        }

        if (definition is null)
        {
            return false;
        }

        if (!TryParseScope(value, out var scopeKind, out var scopeId))
        {
            return false;
        }

        if (!definition.Allows(scopeKind))
        {
            return false;
        }

        grant = new PermissionGrant(key, scopeKind, scopeId);
        return true;
    }

    private static bool TryParseScope(string value, out PermissionScopeKind scopeKind, out string? scopeId)
    {
        scopeKind = PermissionScopeKind.Unknown;
        scopeId = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (!Enum.TryParse(parts[0], ignoreCase: true, out scopeKind) || scopeKind == PermissionScopeKind.Unknown)
        {
            return false;
        }

        if (parts.Length == 2)
        {
            scopeId = parts[1].ToLowerInvariant();
        }

        return true;
    }
}