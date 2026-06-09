using System.Diagnostics;

namespace NeoShip.ApiSvc.Middleware;

public class RequestContextMiddleware
{
    private readonly RequestDelegate next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestContext requestContext)
    {
        var conn = httpContext.Connection;
        requestContext.IpAddress = conn.RemoteIpAddress?.ToString();
        requestContext.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
        requestContext.RequestId = httpContext.TraceIdentifier;

        var activity = Activity.Current;
        if (activity is not null)
        {
            requestContext.TraceId = activity.TraceId.ToString();
            requestContext.SpanId = activity.SpanId.ToString();

            activity.SetTag(OTelConstants.ClientAddress, requestContext.IpAddress);
            activity.SetTag(OTelConstants.UserAgentOriginal, requestContext.UserAgent);
            activity.SetTag(OTelConstants.RequestId, requestContext.RequestId);
        }

        await this.next(httpContext);
    }
}