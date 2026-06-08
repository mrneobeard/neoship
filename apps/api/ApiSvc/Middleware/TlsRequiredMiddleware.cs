namespace NeoShip.ApiSvc.Middleware;

public class TlsRequiredMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enabled;

    public TlsRequiredMiddleware(RequestDelegate next, bool enabled = true)
    {
        _next = next;
        _enabled = enabled;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_enabled)
        {
            var authHeader = context.Request.Headers.Authorization.ToString();

            if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                if (!context.Request.IsHttps)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"basic_auth_requires_tls\",\"message\":\"Basic authentication is only permitted over TLS.\"}");
                    return;
                }

                var path = context.Request.Path.Value ?? "";

                if (!path.StartsWith("/api/v1/auth/token", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/api/v1/service-accounts", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"error\":\"basic_auth_not_allowed\",\"message\":\"Basic authentication is restricted to token and service account endpoints.\"}");
                    return;
                }
            }
        }

        await _next(context);
    }
}
