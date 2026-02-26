namespace MatchMaking.Service.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[HeaderName] = correlationId;
        context.Items["CorrelationId"] = correlationId;
        await next(context);
    }
}
