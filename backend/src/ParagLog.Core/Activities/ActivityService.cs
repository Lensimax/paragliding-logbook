using ParagLog.Core.Abstractions;
using ParagLog.Core.Common;

namespace ParagLog.Core.Activities;

public sealed class ActivityService(IActivityRepository activities, IEquipmentRepository equipment)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<Result<Activity>> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct)
    {
        var validation = Validate(command.Name, command.StartedAt, command.EndedAt, command.WindSpeedKmh, command.WindDirection);
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

    public async Task<Result> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct)
    {
        var deleted = await activities.DeleteAsync(userId, activityId, ct);
        return deleted ? Result.Success() : Result.Failure(DomainError.NotFound("Activity not found."));
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
}
