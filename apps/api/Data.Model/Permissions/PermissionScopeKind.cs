namespace NeoShip.Data.Model;

/// <summary>
/// Describes the scope level a permission grant can apply to.
/// </summary>
public enum PermissionScopeKind
{
    /// <summary>
    /// Unknown or unset scope.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Applies across the whole tenant or installation.
    /// </summary>
    Global = 1,

    /// <summary>
    /// Applies to a single organization.
    /// </summary>
    Organization = 2,

    /// <summary>
    /// Applies to a single resource instance.
    /// </summary>
    Resource = 3,
}
