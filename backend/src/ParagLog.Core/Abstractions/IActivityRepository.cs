using ParagLog.Core.Activities;

namespace ParagLog.Core.Abstractions;

public interface IActivityRepository
{
    Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct);
    Task<ActivityPage> ListAsync(Guid userId, ActivityListQuery query, CancellationToken ct);
    Task<Activity> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct);
    Task<Activity?> UpdateAsync(Guid userId, Guid activityId, UpdateActivityCommand command, CancellationToken ct);
    Task<bool> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct);

    /// <summary>Sets or clears (when <paramref name="track"/> is null) the activity's track reference.</summary>
    Task<Activity?> SetTrackAsync(Guid userId, Guid activityId, TrackReference? track, CancellationToken ct);
}
