namespace NeoShip.ApiSvc;

public static class OTelConstants
{
    public const string ClientAddress = "client.address";
    public const string UserAgentOriginal = "user_agent.original";
    public const string SessionId = "session.id";
    public const string UserId = "enduser.id";
    public const string OrgId = "organization.id";
    public const string RequestId = "http.request.id";
    public const string TraceId = "trace.id";
    public const string SpanId = "span.id";
    public const string AuthAction = "auth.action";
    public const string AuthResult = "auth.result";
    public const string TargetType = "target.type";
    public const string TargetId = "target.id";

    public const string ServiceName = "neoship.iam";
    public const string ActivitySourceName = "NeoShip.Iam";
}