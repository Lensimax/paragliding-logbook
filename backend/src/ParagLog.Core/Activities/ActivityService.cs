using ParagLog.Core.Abstractions;
using ParagLog.Core.Common;

namespace ParagLog.Core.Activities;

public sealed class ActivityService(IActivityRepository activities, IEquipmentRepository equipment, IBlobStore blobStore)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<Result<Activity>> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct)
    {
        var validation = Validate(command.Name, command.StartedAt, command.EndedAt, command.WindSpeedKmh, command.WindDirection)
            ?? ValidateGroundHandlingHasNoFlightStats(command.Type, command.MaxAltitudeM, command.AltitudeGainM, command.DistanceKm);
        if (validation is not null)
            return Result<Activity>.Failure(validation);

        var equipmentError = await ValidateEquipmentOwnershipAsync(userId, command.EquipmentIds, ct);
        if (equipmentError is not null)
            return Result<Activity>.Failure(equipmentError);

        var created = await activities.CreateAsync(userId, command, ct);
        return Result<Activity>.Success(created);
    }

    public async Task<Result<Activity>> UpdateAsync(
        Guid userId, Guid activityId, UpdateActivityCommand command, CancellationToken ct)
    {
        var validation = Validate(command.Name, command.StartedAt, command.EndedAt, command.WindSpeedKmh, command.WindDirection);
        if (validation is not null)
            return Result<Activity>.Failure(validation);

        var equipmentError = await ValidateEquipmentOwnershipAsync(userId, command.EquipmentIds, ct);
        if (equipmentError is not null)
            return Result<Activity>.Failure(equipmentError);

        var updated = await activities.UpdateAsync(userId, activityId, command, ct);
        return updated is null
            ? Result<Activity>.Failure(DomainError.NotFound("Activity not found."))
            : Result<Activity>.Success(updated);
    }

    public Task<Activity?> GetAsync(Guid userId, Guid activityId, CancellationToken ct) =>
        activities.FindByIdAsync(userId, activityId, ct);

    public Task<ActivityPage> ListAsync(
        Guid userId, ActivityType? type, DateTimeOffset? beforeStartedAt, Guid? beforeId, int? limit, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var query = new ActivityListQuery(type, beforeStartedAt, beforeId, clampedLimit);
        return activities.ListAsync(userId, query, ct);
    }

    /// <summary>Deletes the row, then the blob folder - an orphan file is acceptable, a dangling reference is not.</summary>
    public async Task<Result> DeleteAsync(Guid userId, string userPublicId, Guid activityId, CancellationToken ct)
    {
        var deleted = await activities.DeleteAsync(userId, activityId, ct);
        if (!deleted)
            return Result.Failure(DomainError.NotFound("Activity not found."));

        await blobStore.DeleteActivityFolderAsync(userPublicId, activityId, ct);
        return Result.Success();
    }

    public async Task<Result<Activity>> UploadTrackAsync(
        Guid userId, string userPublicId, Guid activityId, TrackReference track, Stream content, CancellationToken ct)
    {
        var activity = await activities.FindByIdAsync(userId, activityId, ct);
        if (activity is null)
            return Result<Activity>.Failure(DomainError.NotFound("Activity not found."));

        if (activity.Type == ActivityType.GroundHandling)
            return Result<Activity>.Failure(DomainError.Validation("Ground handling activities cannot have a track.", "track"));

        // Write the blob first, then commit the row - a crash in between leaves a harmless orphan file.
        await blobStore.SaveTrackAsync(userPublicId, activityId, track.Format, content, ct);

        var updated = await activities.SetTrackAsync(userId, activityId, track, ct);
        return updated is null
            ? Result<Activity>.Failure(DomainError.NotFound("Activity not found."))
            : Result<Activity>.Success(updated);
    }

    /// <summary>Clears the row's track reference, then deletes the blob - never the reverse.</summary>
    public async Task<Result> DeleteTrackAsync(Guid userId, string userPublicId, Guid activityId, CancellationToken ct)
    {
        var activity = await activities.FindByIdAsync(userId, activityId, ct);
        if (activity is null)
            return Result.Failure(DomainError.NotFound("Activity not found."));

        if (activity.Track is null)
            return Result.Failure(DomainError.NotFound("Activity has no track."));

        await activities.SetTrackAsync(userId, activityId, null, ct);
        await blobStore.DeleteTrackAsync(userPublicId, activityId, activity.Track.Format, ct);
        return Result.Success();
    }

    public async Task<(Stream Stream, TrackReference Track)?> OpenTrackAsync(
        Guid userId, string userPublicId, Guid activityId, CancellationToken ct)
    {
        var activity = await activities.FindByIdAsync(userId, activityId, ct);
        if (activity?.Track is null)
            return null;

        var stream = await blobStore.OpenTrackAsync(userPublicId, activityId, activity.Track.Format, ct);
        return stream is null ? null : (stream, activity.Track);
    }

    private async Task<DomainError?> ValidateEquipmentOwnershipAsync(
        Guid userId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct)
    {
        if (equipmentIds.Count == 0)
            return null;

        var distinctIds = equipmentIds.Distinct().ToList();
        var ownedCount = await equipment.CountOwnedAsync(userId, distinctIds, ct);
        return ownedCount == distinctIds.Count
            ? null
            : DomainError.Validation("One or more selected equipment items don't exist.", "equipmentIds");
    }

    private static DomainError? Validate(
        string name, DateTimeOffset startedAt, DateTimeOffset? endedAt, int? windSpeedKmh, int? windDirection)
    {
        if (string.IsNullOrWhiteSpace(name))
            return DomainError.Validation("Name is required.", "name");

        if (endedAt is not null && endedAt <= startedAt)
            return DomainError.Validation("End datetime must be after the start datetime.", "endedAt");

        if (windSpeedKmh is < 0)
            return DomainError.Validation("Wind speed cannot be negative.", "windSpeedKmh");

        if (windDirection is < 0 or > 359)
            return DomainError.Validation("Wind direction must be between 0 and 359 degrees.", "windDirection");

        return null;
    }

    /// <summary>Mirrors the DB's activities_ground_handling_has_no_track CHECK constraint.</summary>
    private static DomainError? ValidateGroundHandlingHasNoFlightStats(
        ActivityType type, int? maxAltitudeM, int? altitudeGainM, double? distanceKm)
    {
        if (type == ActivityType.GroundHandling && (maxAltitudeM is not null || altitudeGainM is not null || distanceKm is not null))
            return DomainError.Validation("Ground handling activities cannot have flight statistics.", "maxAltitudeM");

        return null;
    }
}
