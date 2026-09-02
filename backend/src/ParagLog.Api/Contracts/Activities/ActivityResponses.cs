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
    string? Comment,
    int? WindSpeedKmh,
    int? WindDirection,
    int? DurationSeconds,
    bool HasTrack,
    IReadOnlyList<Guid> EquipmentIds)
{
    public static ActivityResponse From(Activity activity) => new(
        activity.Id, activity.Type, activity.Name, activity.StartedAt, activity.EndedAt,
        activity.LocalDate, activity.LocalTz, activity.TakeoffLocation, activity.LandingLocation,
        activity.Comment, activity.WindSpeedKmh, activity.WindDirection, activity.DurationSeconds,
        activity.TrackFilename is not null, activity.EquipmentIds);
}

public sealed record ActivityListResponse(IReadOnlyList<ActivitySummaryResponse> Items, string? NextCursor);
