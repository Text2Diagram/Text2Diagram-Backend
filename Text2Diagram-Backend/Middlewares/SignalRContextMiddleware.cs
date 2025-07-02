namespace Text2Diagram_Backend.Middlewares;

public class SignalRContextMiddleware
{
    private readonly RequestDelegate _next;

    public SignalRContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Connection-Id", out var connectionId))
        {
            SignalRContext.ConnectionId = connectionId;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            SignalRContext.ConnectionId = null;
        }
    }
}
