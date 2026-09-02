namespace ParagLog.Core.Activities;

public sealed record CreateActivityCommand(
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
    IReadOnlyList<Guid> EquipmentIds
);

public sealed record UpdateActivityCommand(
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
    IReadOnlyList<Guid> EquipmentIds
);

public sealed record ActivityListQuery(
    ActivityType? Type,
    DateTimeOffset? BeforeStartedAt,
    Guid? BeforeId,
    int Limit
);

public sealed record ActivityPage(IReadOnlyList<Activity> Items, bool HasMore);
