namespace NeoShip.Data.Model;

/// <summary>
/// Core permission definitions shipped with the product.
/// </summary>
public static class CorePermissions
{
    /// <summary>
    /// Gets the core permission definitions.
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> All { get; } = new[]
    {
        new PermissionDefinition(PermissionKey.Create("auth.sessions", "read"), "Read sessions", PermissionScopeKind.Resource, PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("auth.sessions", "revoke"), "Revoke sessions", PermissionScopeKind.Resource, PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("auth.api_keys", "read"), "Read API keys", PermissionScopeKind.Resource, PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("auth.api_keys", "write"), "Create and revoke API keys", PermissionScopeKind.Resource, PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("auth.passkeys", "read"), "Read passkeys", PermissionScopeKind.Resource),
        new PermissionDefinition(PermissionKey.Create("auth.passkeys", "write"), "Create and revoke passkeys", PermissionScopeKind.Resource),
        new PermissionDefinition(PermissionKey.Create("auth.mfa", "write"), "Manage MFA settings", PermissionScopeKind.Resource),
        new PermissionDefinition(PermissionKey.Create("org.roles", "read"), "Read roles", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.roles", "write"), "Create and update roles", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.groups", "read"), "Read groups", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.groups", "write"), "Create and update groups", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.service_accounts", "read"), "Read service accounts", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.service_accounts", "write"), "Create and update service accounts", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.identity_providers", "read"), "Read identity providers", PermissionScopeKind.Organization),
        new PermissionDefinition(PermissionKey.Create("org.identity_providers", "write"), "Create and update identity providers", PermissionScopeKind.Organization),
    };
}
