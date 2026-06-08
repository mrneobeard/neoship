using System.Diagnostics;

namespace NeoShip.ApiSvc;

public class RequestContext
{
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? RequestId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrgId { get; set; }
    public Guid? SessionId { get; set; }

    public Dictionary<string, object?> ToOtelBaggage()
    {
        var bag = new Dictionary<string, object?>();
        if (IpAddress is not null) bag[OTelConstants.ClientAddress] = IpAddress;
        if (UserAgent is not null) bag[OTelConstants.UserAgentOriginal] = UserAgent;
        if (UserId.HasValue) bag[OTelConstants.UserId] = UserId.Value.ToString();
        if (OrgId.HasValue) bag[OTelConstants.OrgId] = OrgId.Value.ToString();
        if (SessionId.HasValue) bag[OTelConstants.SessionId] = SessionId.Value.ToString();
        if (TraceId is not null) bag[OTelConstants.TraceId] = TraceId;
        if (SpanId is not null) bag[OTelConstants.SpanId] = SpanId;
        return bag;
    }

    public void EnrichActivity(Activity? activity)
    {
        if (activity is null) return;
        if (IpAddress is not null) activity.SetTag(OTelConstants.ClientAddress, IpAddress);
        if (UserAgent is not null) activity.SetTag(OTelConstants.UserAgentOriginal, UserAgent);
        if (UserId.HasValue) activity.SetTag(OTelConstants.UserId, UserId.Value.ToString());
        if (OrgId.HasValue) activity.SetTag(OTelConstants.OrgId, OrgId.Value.ToString());
    }
}