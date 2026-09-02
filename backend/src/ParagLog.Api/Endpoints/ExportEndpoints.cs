using Microsoft.AspNetCore.Http.Features;
using ParagLog.Api.Auth;
using ParagLog.Core.Export;

namespace ParagLog.Api.Endpoints;

public static class ExportEndpoints
{
    public static void MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/export", ExportAsync).RequireAuthorization();
    }

    private static async Task ExportAsync(
        ICurrentUser user, ExportService exportService, HttpContext httpContext, CancellationToken ct)
    {
        // ZipArchive.Dispose() writes the central directory synchronously; Kestrel (and TestServer)
        // disallow synchronous response writes by default, so this has to be opted back in here.
        httpContext.Features.Get<IHttpBodyControlFeature>()!.AllowSynchronousIO = true;

        var response = httpContext.Response;
        response.ContentType = "application/zip";
        response.Headers.ContentDisposition = "attachment; filename=\"paraglog-export.zip\"";

        await exportService.ExportAsync(user.Id, user.PublicId, response.Body, ct);
    }
}
