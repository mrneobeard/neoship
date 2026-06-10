using System.Text.Json;

using NeoShip.ApiSvc.Models;

namespace NeoShip.ApiSvc.Tests;

/// <summary>
/// Tests the language-neutral API response contract models.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// dotnet test apps/api/ApiSvc.Tests/NeoShip.ApiSvc.Tests.csproj
/// </code>
/// </remarks>
[Trait(Traits.Category, Traits.Unit)]
public class ApiContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Verifies that a single-resource envelope serializes with camel-case `data` and `meta` properties.
    /// </summary>
    [Fact]
    public void ApiEnvelope_SerializesWithCamelCaseDataAndMeta()
    {
        var meta = new ApiMeta("req_1", "trace_1", DateTimeOffset.Parse("2026-05-28T12:00:00Z"));
        var envelope = new ApiEnvelope<TestResource>(new TestResource("role", "admin"), meta);

        var json = JsonSerializer.Serialize(envelope, Options);

        Assert.Contains("\"data\"", json, StringComparison.Ordinal);
        Assert.Contains("\"meta\"", json, StringComparison.Ordinal);
        Assert.Contains("\"requestId\":\"req_1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"traceId\":\"trace_1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"serverTime\":\"2026-05-28T12:00:00+00:00\"", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a collection envelope serializes with the canonical `pagination` object.
    /// </summary>
    [Fact]
    public void ApiCollectionEnvelope_SerializesWithPagination()
    {
        var meta = new ApiMeta("req_1", null, DateTimeOffset.Parse("2026-05-28T12:00:00Z"));
        var page = new ApiPagination(50, "next", null, hasMore: true);
        var envelope = new ApiCollectionEnvelope<TestResource>(
            [new TestResource("role", "admin")],
            page,
            meta);

        var json = JsonSerializer.Serialize(envelope, Options);

        Assert.Contains("\"pagination\"", json, StringComparison.Ordinal);
        Assert.Contains("\"limit\":50", json, StringComparison.Ordinal);
        Assert.Contains("\"nextCursor\":\"next\"", json, StringComparison.Ordinal);
        Assert.Contains("\"hasMore\":true", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that an error envelope serializes with stable `error` and `meta` properties.
    /// </summary>
    [Fact]
    public void ApiErrorEnvelope_SerializesWithStableErrorShape()
    {
        var meta = new ApiMeta("req_1", null, DateTimeOffset.Parse("2026-05-28T12:00:00Z"));
        var error = new ApiError("permission_denied", "Permission denied.");
        var envelope = new ApiErrorEnvelope(error, meta);

        var json = JsonSerializer.Serialize(envelope, Options);

        Assert.Contains("\"error\"", json, StringComparison.Ordinal);
        Assert.Contains("\"code\":\"permission_denied\"", json, StringComparison.Ordinal);
        Assert.Contains("\"message\":\"Permission denied.\"", json, StringComparison.Ordinal);
        Assert.Contains("\"meta\"", json, StringComparison.Ordinal);
    }

    private sealed record TestResource(string Type, string Name);
}