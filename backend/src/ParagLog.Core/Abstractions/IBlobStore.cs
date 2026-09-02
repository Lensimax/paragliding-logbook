using ParagLog.Core.Activities;

namespace ParagLog.Core.Abstractions;

public interface IBlobStore
{
    Task SaveTrackAsync(string userPublicId, Guid activityId, TrackFormat format, Stream content, CancellationToken ct);
    Task<Stream?> OpenTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct);
    Task DeleteTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct);

    Task SaveElevationAsync(string userPublicId, Guid activityId, Stream content, CancellationToken ct);
    Task<Stream?> OpenElevationAsync(string userPublicId, Guid activityId, CancellationToken ct);

    /// <summary>Removes the activity's whole blob folder (track.*, elevation.json). No-op if absent.</summary>
    Task DeleteActivityFolderAsync(string userPublicId, Guid activityId, CancellationToken ct);
}
