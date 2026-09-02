using Microsoft.AspNetCore.Diagnostics;

namespace ParagLog.Api.Middleware;

/// <summary>
/// Catches anything an endpoint didn't turn into a Result (a bug, not an expected failure) and
/// renders it as a ProblemDetails response instead of leaking a stack trace to the client.
/// </summary>
public sealed class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new { title = "An unexpected error occurred.", status = 500 }, cancellationToken: ct);

        return true;
    }
}
