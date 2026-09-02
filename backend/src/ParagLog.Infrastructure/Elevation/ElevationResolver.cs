using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;

namespace ParagLog.Infrastructure.Elevation;

/// <summary>
/// Runs on upload, per SPEC.md: downsample the track, batch-query ground elevation, write
/// elevation.json. This is the one place the backend reads track file contents - only to pull
/// coordinates for the elevation lookup, never for stats or auto-fill (that stays client-side).
/// Best-effort and non-throwing: a failure here must never fail the track upload itself.
/// </summary>
public sealed class ElevationResolver(
    IBlobStore blobStore,
    IElevationService elevationService,
    IActivityRepository activities,
    ILogger<ElevationResolver> logger)
{
    public async Task TryResolveAsync(Guid userId, string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct)
    {
        try
        {
            await using var trackStream = await blobStore.OpenTrackAsync(userPublicId, activityId, format, ct);
            if (trackStream is null)
                return;

            using var reader = new StreamReader(trackStream);
            var content = await reader.ReadToEndAsync(ct);

            var points = TrackDownsampler.Downsample(content, format);
            if (points.Count == 0)
                return;

            var elevations = await elevationService.GetElevationsAsync(points, ct);

            var samples = points.Zip(elevations, (point, elevation) => new
            {
                lat = point.Lat,
                lon = point.Lon,
                elevationM = elevation,
            });
            var json = JsonSerializer.Serialize(samples);

            await using var elevationStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await blobStore.SaveElevationAsync(userPublicId, activityId, elevationStream, ct);

            await activities.SetHasElevationAsync(userId, activityId, true, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Elevation resolution failed for activity {ActivityId}", activityId);
        }
    }
}
