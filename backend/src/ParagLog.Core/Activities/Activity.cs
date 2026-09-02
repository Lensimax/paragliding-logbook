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

    // Derived client-side from the track and sent as ordinary field values at create time
    // (the backend never parses tracks itself).
    public int? MaxAltitudeM { get; init; }
    public int? AltitudeGainM { get; init; }
    public double? DistanceKm { get; init; }

    public TrackReference? Track { get; init; }
    public bool HasElevation { get; init; }

    public int? WindSpeedKmh { get; init; }
    public int? WindDirection { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public int? DurationSeconds { get; init; }

    public IReadOnlyList<Guid> EquipmentIds { get; init; } = [];
}
