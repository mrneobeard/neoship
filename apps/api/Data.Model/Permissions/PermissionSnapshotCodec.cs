using System.Text.Json;

namespace NeoShip.Data.Model;

/// <summary>
/// Serializes and deserializes compact permission snapshots for sessions and tokens.
/// </summary>
/// <example>
/// <code>
/// var codec = new PermissionSnapshotCodec();
/// var json = codec.Serialize(permissionSet);
/// var set = codec.Deserialize(json);
/// </code>
/// </example>
public sealed class PermissionSnapshotCodec
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes a permission set to JSON.
    /// </summary>
    /// <param name="permissionSet">The permission set to serialize.</param>
    /// <returns>The JSON snapshot.</returns>
    public string Serialize(PermissionSet permissionSet)
    {
        var entries = permissionSet.All.Select(grant => new PermissionSnapshotEntry(
            grant.Key.ToString(),
            grant.ScopeKind,
            grant.ScopeId)).ToArray();

        return JsonSerializer.Serialize(entries, jsonOptions);
    }

    /// <summary>
    /// Deserializes a permission snapshot from JSON.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The reconstructed permission set.</returns>
    public PermissionSet Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PermissionSet();
        }

        try
        {
            var entries = JsonSerializer.Deserialize<PermissionSnapshotEntry[]>(json, jsonOptions) ?? [];
            var grants = new List<PermissionGrant>(entries.Length);

            foreach (var entry in entries)
            {
                if (!PermissionKey.TryParse(entry.Permission, out var key))
                {
                    continue;
                }

                grants.Add(new PermissionGrant(key, entry.ScopeKind, entry.ScopeId));
            }

            return new PermissionSet(grants);
        }
        catch (JsonException)
        {
            return new PermissionSet();
        }
    }

    private readonly record struct PermissionSnapshotEntry(string Permission, PermissionScopeKind ScopeKind, string? ScopeId);
}