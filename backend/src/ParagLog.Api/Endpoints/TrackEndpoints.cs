using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
using ParagLog.Api.Auth;
using ParagLog.Api.Common;
using ParagLog.Api.Contracts.Activities;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Infrastructure.Elevation;

namespace ParagLog.Api.Endpoints;

public static class TrackEndpoints
{
    private const long MaxTrackSizeBytes = 10 * 1024 * 1024; // spec: typical 100 KB - 5 MB

    public static void MapTrackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activities/{id:guid}/track").RequireAuthorization();

        group.MapPost("/", UploadAsync);
        group.MapDelete("/", DeleteAsync);
        group.MapGet("/", DownloadAsync);

        app.MapGet("/api/activities/{id:guid}/elevation", DownloadElevationAsync).RequireAuthorization();
    }

    private static async Task<IResult> UploadAsync(
        Guid id,
        HttpRequest request,
        ICurrentUser user,
        ActivityService activities,
        ElevationResolver elevationResolver,
        CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { message = "Expected multipart/form-data." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files["file"];
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { message = "No file provided." });

        if (file.Length > MaxTrackSizeBytes)
            return Results.BadRequest(new { message = "File is too large." });

        var format = DetectFormat(file.FileName);
        if (format is null)
            return Results.BadRequest(new { message = "File must be .gpx or .igc." });

        await using var buffer = new MemoryStream();
        await using (var upload = file.OpenReadStream())
        {
            await upload.CopyToAsync(buffer, ct);
        }

        var sha256 = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
        var track = new TrackReference
        {
            Filename = $"track.{(format == TrackFormat.Gpx ? "gpx" : "igc")}",
            Format = format.Value,
            SizeBytes = buffer.Length,
            Sha256 = sha256,
        };
        buffer.Position = 0;

        var result = await activities.UploadTrackAsync(user.Id, user.PublicId, id, track, buffer, ct);
        if (!result.IsSuccess)
            return result.ToProblem();

        // Best-effort and synchronous, per SPEC.md: downsample, batch-query, write elevation.json.
        await elevationResolver.TryResolveAsync(user.Id, user.PublicId, id, track.Format, ct);

        var refreshed = await activities.GetAsync(user.Id, id, ct);
        return Results.Ok(ActivityResponse.From(refreshed ?? result.Value));
    }

    private static async Task<IResult> DeleteAsync(Guid id, ICurrentUser user, ActivityService activities, CancellationToken ct)
    {
        var result = await activities.DeleteTrackAsync(user.Id, user.PublicId, id, ct);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    private static async Task<IResult> DownloadAsync(
        Guid id, ICurrentUser user, ActivityService activities, HttpResponse response, CancellationToken ct)
    {
        var download = await activities.OpenTrackAsync(user.Id, user.PublicId, id, ct);
        if (download is null)
            return Results.NotFound();

        var (stream, track) = download.Value;
        var contentType = track.Format == TrackFormat.Gpx ? "application/gpx+xml" : "application/octet-stream";

        response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(stream, contentType, track.Filename, entityTag: new EntityTagHeaderValue($"\"{track.Sha256}\""));
    }

    private static async Task<IResult> DownloadElevationAsync(
        Guid id, ICurrentUser user, ActivityService activities, IBlobStore blobStore, HttpResponse response, CancellationToken ct)
    {
        var activity = await activities.GetAsync(user.Id, id, ct);
        if (activity is null)
            return Results.NotFound();

        var stream = await blobStore.OpenElevationAsync(user.PublicId, id, ct);
        if (stream is null)
            return Results.NotFound();

        response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(stream, "application/json", "elevation.json");
    }

    private static TrackFormat? DetectFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "gpx" => TrackFormat.Gpx,
            "igc" => TrackFormat.Igc,
            _ => null,
        };
    }
}
