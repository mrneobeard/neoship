using System.Collections.Generic;
using System.Diagnostics;

namespace NeoShip.ApiSvc;

/// <summary>
/// Carries request-scoped data and custom metadata.
/// </summary>
/// <example>
/// <code>
/// requestContext["tenant.plan"] = "enterprise";
/// var bag = requestContext.ToOtelBaggage();
/// </code>
/// </example>
public class RequestContext
{
    /// <summary>
    /// Gets or sets the request IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the request user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets the span identifier.
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the organization identifier.
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Gets custom request properties.
    /// </summary>
    public Dictionary<string, object?> Properties { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a custom request property.
    /// </summary>
    /// <param name="key">The property name.</param>
    /// <returns>The property value, or <see langword="null"/> when not set.</returns>
    public object? this[string key]
    {
        get => this.Properties.TryGetValue(key, out var value) ? value : null;
        set => this.Properties[key] = value;
    }

    /// <summary>
    /// Builds an OpenTelemetry baggage dictionary for the current request.
    /// </summary>
    /// <returns>A dictionary of baggage values.</returns>
    public Dictionary<string, object?> ToOtelBaggage()
    {
        var bag = new Dictionary<string, object?>(this.Properties.Count + 8, StringComparer.Ordinal);

        foreach (var (key, value) in this.Properties)
        {
            bag[key] = value;
        }

        if (IpAddress is not null) bag[OTelConstants.ClientAddress] = IpAddress;
        if (UserAgent is not null) bag[OTelConstants.UserAgentOriginal] = UserAgent;
        if (UserId.HasValue) bag[OTelConstants.UserId] = UserId.Value.ToString();
        if (OrgId.HasValue) bag[OTelConstants.OrgId] = OrgId.Value.ToString();
        if (SessionId.HasValue) bag[OTelConstants.SessionId] = SessionId.Value.ToString();
        if (RequestId is not null) bag[OTelConstants.RequestId] = RequestId;
        if (TraceId is not null) bag[OTelConstants.TraceId] = TraceId;
        if (SpanId is not null) bag[OTelConstants.SpanId] = SpanId;
        return bag;
    }

    /// <summary>
    /// Enriches the current activity with request-scoped tags.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    public void EnrichActivity(Activity? activity)
    {
        if (activity is null) return;
        if (IpAddress is not null) activity.SetTag(OTelConstants.ClientAddress, IpAddress);
        if (UserAgent is not null) activity.SetTag(OTelConstants.UserAgentOriginal, UserAgent);
        if (UserId.HasValue) activity.SetTag(OTelConstants.UserId, UserId.Value.ToString());
        if (OrgId.HasValue) activity.SetTag(OTelConstants.OrgId, OrgId.Value.ToString());
    }
}