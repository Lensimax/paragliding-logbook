namespace ParagLog.Core.Activities;

/// <summary>
/// Identity of the uploaded track blob. Either every field is present or the activity has no
/// track at all - mirrors the DB's <c>activities_track_all_or_none</c> CHECK constraint.
/// </summary>
public sealed class TrackReference
{
    /// <summary>Stored blob filename, e.g. "track.gpx" - never the user's original filename.</summary>
    public required string Filename { get; init; }
    public required TrackFormat Format { get; init; }
    public required long SizeBytes { get; init; }
    public required string Sha256 { get; init; }
}
