using Dapper;
using Npgsql;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Infrastructure.Persistence.TypeHandlers;

namespace ParagLog.Infrastructure.Persistence;

public sealed class ActivityRepository(NpgsqlConnectionFactory connectionFactory) : IActivityRepository
{
    private static readonly string GetByIdSql = SqlLoader.Load("Activities.GetById");
    private static readonly string ListSql = SqlLoader.Load("Activities.List");
    private static readonly string InsertSql = SqlLoader.Load("Activities.Insert");
    private static readonly string UpdateSql = SqlLoader.Load("Activities.Update");
    private static readonly string DeleteSql = SqlLoader.Load("Activities.Delete");
    private static readonly string EquipmentListByActivitySql = SqlLoader.Load("ActivityEquipment.ListByActivity");
    private static readonly string EquipmentDeleteByActivitySql = SqlLoader.Load("ActivityEquipment.DeleteByActivity");
    private static readonly string EquipmentInsertSql = SqlLoader.Load("ActivityEquipment.Insert");

    public async Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var command = new CommandDefinition(
            GetByIdSql, new { ActivityId = activityId, UserId = userId }, cancellationToken: ct);
        var activity = await connection.QuerySingleOrDefaultAsync<Activity>(command);
        if (activity is null)
            return null;

        var equipmentCommand = new CommandDefinition(
            EquipmentListByActivitySql, new { ActivityId = activityId }, cancellationToken: ct);
        var equipmentIds = (await connection.QueryAsync<Guid>(equipmentCommand)).ToList();

        return WithEquipmentIds(activity, equipmentIds);
    }

    public async Task<ActivityPage> ListAsync(Guid userId, ActivityListQuery query, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var command = new CommandDefinition(
            ListSql,
            new
            {
                UserId = userId,
                Type = query.Type is null ? null : PgEnumTypeHandler<ActivityType>.ToLabel(query.Type.Value),
                query.BeforeStartedAt,
                query.BeforeId,
                Limit = query.Limit + 1,
            },
            cancellationToken: ct);

        var rows = (await connection.QueryAsync<Activity>(command)).ToList();
        var hasMore = rows.Count > query.Limit;
        var items = hasMore ? rows.Take(query.Limit).ToList() : rows;

        return new ActivityPage(items, hasMore);
    }

    public async Task<Activity> CreateAsync(Guid userId, CreateActivityCommand command, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            new
            {
                Id = id,
                UserId = userId,
                Type = PgEnumTypeHandler<ActivityType>.ToLabel(command.Type),
                command.Name,
                command.StartedAt,
                command.EndedAt,
                command.LocalDate,
                command.LocalTz,
                command.TakeoffLocation,
                command.LandingLocation,
                command.Comment,
                command.WindSpeedKmh,
                command.WindDirection,
                CreatedAt = now,
                UpdatedAt = now,
            },
            transaction,
            cancellationToken: ct));

        await InsertEquipmentLinksAsync(connection, transaction, id, command.EquipmentIds, ct);

        await transaction.CommitAsync(ct);

        return new Activity
        {
            Id = id,
            UserId = userId,
            Type = command.Type,
            Name = command.Name,
            StartedAt = command.StartedAt,
            EndedAt = command.EndedAt,
            LocalDate = command.LocalDate,
            LocalTz = command.LocalTz,
            TakeoffLocation = command.TakeoffLocation,
            LandingLocation = command.LandingLocation,
            Comment = command.Comment,
            WindSpeedKmh = command.WindSpeedKmh,
            WindDirection = command.WindDirection,
            CreatedAt = now,
            UpdatedAt = now,
            DurationSeconds = DurationSeconds(command.StartedAt, command.EndedAt),
            EquipmentIds = command.EquipmentIds,
        };
    }

    public async Task<Activity?> UpdateAsync(
        Guid userId, Guid activityId, UpdateActivityCommand command, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            new
            {
                Id = activityId,
                UserId = userId,
                Type = PgEnumTypeHandler<ActivityType>.ToLabel(command.Type),
                command.Name,
                command.StartedAt,
                command.EndedAt,
                command.LocalDate,
                command.LocalTz,
                command.TakeoffLocation,
                command.LandingLocation,
                command.Comment,
                command.WindSpeedKmh,
                command.WindDirection,
                UpdatedAt = now,
            },
            transaction,
            cancellationToken: ct));

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync(ct);
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            EquipmentDeleteByActivitySql, new { ActivityId = activityId }, transaction, cancellationToken: ct));
        await InsertEquipmentLinksAsync(connection, transaction, activityId, command.EquipmentIds, ct);

        await transaction.CommitAsync(ct);

        return new Activity
        {
            Id = activityId,
            UserId = userId,
            Type = command.Type,
            Name = command.Name,
            StartedAt = command.StartedAt,
            EndedAt = command.EndedAt,
            LocalDate = command.LocalDate,
            LocalTz = command.LocalTz,
            TakeoffLocation = command.TakeoffLocation,
            LandingLocation = command.LandingLocation,
            Comment = command.Comment,
            WindSpeedKmh = command.WindSpeedKmh,
            WindDirection = command.WindDirection,
            CreatedAt = now,
            UpdatedAt = now,
            DurationSeconds = DurationSeconds(command.StartedAt, command.EndedAt),
            EquipmentIds = command.EquipmentIds,
        };
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(DeleteSql, new { Id = activityId, UserId = userId }, cancellationToken: ct);
        var rowsAffected = await connection.ExecuteAsync(command);
        return rowsAffected > 0;
    }

    private async Task InsertEquipmentLinksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid activityId,
        IReadOnlyList<Guid> equipmentIds,
        CancellationToken ct)
    {
        if (equipmentIds.Count == 0)
            return;

        var rows = equipmentIds.Select(equipmentId => new { ActivityId = activityId, EquipmentId = equipmentId });
        await connection.ExecuteAsync(new CommandDefinition(EquipmentInsertSql, rows, transaction, cancellationToken: ct));
    }

    private static Activity WithEquipmentIds(Activity activity, IReadOnlyList<Guid> equipmentIds) => new()
    {
        Id = activity.Id,
        UserId = activity.UserId,
        Type = activity.Type,
        Name = activity.Name,
        StartedAt = activity.StartedAt,
        EndedAt = activity.EndedAt,
        LocalDate = activity.LocalDate,
        LocalTz = activity.LocalTz,
        TakeoffLocation = activity.TakeoffLocation,
        LandingLocation = activity.LandingLocation,
        TakeoffLat = activity.TakeoffLat,
        TakeoffLon = activity.TakeoffLon,
        Comment = activity.Comment,
        MaxAltitudeM = activity.MaxAltitudeM,
        AltitudeGainM = activity.AltitudeGainM,
        DistanceKm = activity.DistanceKm,
        TrackFilename = activity.TrackFilename,
        TrackFormat = activity.TrackFormat,
        TrackSizeBytes = activity.TrackSizeBytes,
        TrackSha256 = activity.TrackSha256,
        HasElevation = activity.HasElevation,
        WindSpeedKmh = activity.WindSpeedKmh,
        WindDirection = activity.WindDirection,
        CreatedAt = activity.CreatedAt,
        UpdatedAt = activity.UpdatedAt,
        DurationSeconds = activity.DurationSeconds,
        EquipmentIds = equipmentIds,
    };

    private static int? DurationSeconds(DateTimeOffset startedAt, DateTimeOffset? endedAt) =>
        endedAt is null ? null : (int)(endedAt.Value - startedAt).TotalSeconds;
}
