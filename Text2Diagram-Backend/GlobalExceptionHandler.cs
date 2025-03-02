using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Promotion.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred. Occurence Time: {time}", DateTime.Now);


        var problemDetails = new ProblemDetails
        {
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        };

        switch (exception)
        {
            case Exception ex:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = ex.ToString();
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                break;
            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred.";
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
