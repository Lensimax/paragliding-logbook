using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;

namespace ParagLog.Infrastructure.Storage;

/// <summary>
/// /data/blobs/{user_public_id}/activities/{activity_id}/track.{gpx|igc}
/// No user-supplied string ever appears in a path: activity_id is a UUID, and the filename is
/// always the fixed "track.&lt;ext&gt;" rather than whatever the user uploaded.
/// </summary>
public sealed class FileSystemBlobStore(string blobsRoot) : IBlobStore
{
    public async Task SaveTrackAsync(string userPublicId, Guid activityId, TrackFormat format, Stream content, CancellationToken ct)
    {
        var directory = ActivityDirectory(userPublicId, activityId);
        Directory.CreateDirectory(directory);

        var finalPath = TrackPath(userPublicId, activityId, format);
        var tempPath = $"{finalPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(file, ct);
            }

            File.Move(tempPath, finalPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public Task<Stream?> OpenTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct)
    {
        var path = TrackPath(userPublicId, activityId, format);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteTrackAsync(string userPublicId, Guid activityId, TrackFormat format, CancellationToken ct)
    {
        var path = TrackPath(userPublicId, activityId, format);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public Task DeleteActivityFolderAsync(string userPublicId, Guid activityId, CancellationToken ct)
    {
        var directory = ActivityDirectory(userPublicId, activityId);
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
        return Task.CompletedTask;
    }

    private string ActivityDirectory(string userPublicId, Guid activityId) =>
        Path.Combine(blobsRoot, userPublicId, "activities", activityId.ToString());

    private string TrackPath(string userPublicId, Guid activityId, TrackFormat format) =>
        Path.Combine(ActivityDirectory(userPublicId, activityId), $"track.{Extension(format)}");

    private static string Extension(TrackFormat format) => format switch
    {
        TrackFormat.Gpx => "gpx",
        TrackFormat.Igc => "igc",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}
