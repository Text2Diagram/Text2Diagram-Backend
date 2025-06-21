namespace Text2Diagram_Backend.HttpHandlers;

public class ReprLoggingHandler

{
    private readonly RequestDelegate next;
    private readonly ILogger<ReprLoggingHandler> logger;

    public ReprLoggingHandler(RequestDelegate next, ILogger<ReprLoggingHandler> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Request: {method} {url}", context.Request.Method, context.Request.Path);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await next.Invoke(context);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
        logger.LogInformation("Response: {statusCode} {content}", context.Response.StatusCode, responseContent);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }
}

public static class LoggingHandlerExtensions
{
    public static IApplicationBuilder UseReprLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ReprLoggingHandler>();
    }
}
