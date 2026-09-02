using System.Diagnostics;

namespace ParagLog.Api.Middleware;

/// <summary>One structured log line per request: method, path, status and duration.</summary>
public static class RequestLogging
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogging");
            var stopwatch = Stopwatch.StartNew();

            await next(context);

            stopwatch.Stop();
            logger.LogInformation(
                "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        });
    }
}
