using ParagLog.Core.Activities;

namespace ParagLog.Api.Contracts.Activities;

public sealed record CreateActivityRequest(
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
    IReadOnlyList<Guid>? EquipmentIds,
    double? TakeoffLat = null,
    double? TakeoffLon = null,
    int? MaxAltitudeM = null,
    int? AltitudeGainM = null,
    double? DistanceKm = null)
{
    public CreateActivityCommand ToCommand() => new(
        Type, Name, StartedAt, EndedAt, LocalDate, LocalTz, TakeoffLocation, LandingLocation,
        Comment, WindSpeedKmh, WindDirection, EquipmentIds ?? [],
        TakeoffLat, TakeoffLon, MaxAltitudeM, AltitudeGainM, DistanceKm);
}

public sealed record UpdateActivityRequest(
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
    IReadOnlyList<Guid>? EquipmentIds)
{
    public UpdateActivityCommand ToCommand() => new(
        Type, Name, StartedAt, EndedAt, LocalDate, LocalTz, TakeoffLocation, LandingLocation,
        Comment, WindSpeedKmh, WindDirection, EquipmentIds ?? []);
}
