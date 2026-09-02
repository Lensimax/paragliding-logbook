namespace ParagLog.Core.Activities;

public sealed class Activity
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required ActivityType Type { get; init; }

    public required string Name { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public required DateOnly LocalDate { get; init; }
    public string? LocalTz { get; init; }

    public string? TakeoffLocation { get; init; }
    public string? LandingLocation { get; init; }
    public double? TakeoffLat { get; init; }
    public double? TakeoffLon { get; init; }

    public string? Comment { get; init; }

    // Derived from the track. Null until Step 8 wires track upload/parsing.
    public int? MaxAltitudeM { get; init; }
    public int? AltitudeGainM { get; init; }
    public double? DistanceKm { get; init; }

    public string? TrackFilename { get; init; }
    public TrackFormat? TrackFormat { get; init; }
    public long? TrackSizeBytes { get; init; }
    public string? TrackSha256 { get; init; }
    public bool HasElevation { get; init; }

    public int? WindSpeedKmh { get; init; }
    public int? WindDirection { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public int? DurationSeconds { get; init; }

    public IReadOnlyList<Guid> EquipmentIds { get; init; } = [];
}
