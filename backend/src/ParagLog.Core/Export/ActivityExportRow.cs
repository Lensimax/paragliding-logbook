using ParagLog.Core.Activities;

namespace ParagLog.Core.Export;

/// <summary>Flattened activity shape for the CSV export, joined with equipment names.</summary>
public sealed record ActivityExportRow(
    Guid Id,
    ActivityType Type,
    string Name,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    DateOnly LocalDate,
    string? TakeoffLocation,
    string? LandingLocation,
    int? MaxAltitudeM,
    int? AltitudeGainM,
    double? DistanceKm,
    int? WindSpeedKmh,
    int? WindDirection,
    string? Comment,
    string EquipmentNames,
    TrackReference? Track
);
