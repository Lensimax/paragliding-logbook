using ParagLog.Core.Activities;

namespace ParagLog.Api.Contracts.Activities;

public sealed record ActivitySummaryResponse(
    Guid Id,
    ActivityType Type,
    string Name,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int? DurationSeconds)
{
    public static ActivitySummaryResponse From(Activity activity) => new(
        activity.Id, activity.Type, activity.Name, activity.StartedAt, activity.EndedAt, activity.DurationSeconds);
}

public sealed record TrackReferenceResponse(string Filename, TrackFormat Format, long SizeBytes, string Sha256)
{
    public static TrackReferenceResponse From(TrackReference track) =>
        new(track.Filename, track.Format, track.SizeBytes, track.Sha256);
}

public sealed record ActivityResponse(
    Guid Id,
    ActivityType Type,
    string Name,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    DateOnly LocalDate,
    string? LocalTz,
    string? TakeoffLocation,
    string? LandingLocation,
    double? TakeoffLat,
    double? TakeoffLon,
    string? Comment,
    int? MaxAltitudeM,
    int? AltitudeGainM,
    double? DistanceKm,
    int? WindSpeedKmh,
    int? WindDirection,
    int? DurationSeconds,
    TrackReferenceResponse? Track,
    bool HasElevation,
    IReadOnlyList<Guid> EquipmentIds)
{
    public static ActivityResponse From(Activity activity) => new(
        activity.Id, activity.Type, activity.Name, activity.StartedAt, activity.EndedAt,
        activity.LocalDate, activity.LocalTz, activity.TakeoffLocation, activity.LandingLocation,
        activity.TakeoffLat, activity.TakeoffLon, activity.Comment,
        activity.MaxAltitudeM, activity.AltitudeGainM, activity.DistanceKm,
        activity.WindSpeedKmh, activity.WindDirection, activity.DurationSeconds,
        activity.Track is not null ? TrackReferenceResponse.From(activity.Track) : null,
        activity.HasElevation,
        activity.EquipmentIds);
}

public sealed record ActivityListResponse(IReadOnlyList<ActivitySummaryResponse> Items, string? NextCursor);
