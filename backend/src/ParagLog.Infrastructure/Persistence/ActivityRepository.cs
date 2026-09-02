using Dapper;
using Npgsql;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Core.Export;
using ParagLog.Infrastructure.Persistence.TypeHandlers;

namespace ParagLog.Infrastructure.Persistence;

public sealed class ActivityRepository(NpgsqlConnectionFactory connectionFactory) : IActivityRepository
{
    private static readonly string GetByIdSql = SqlLoader.Load("Activities.GetById");
    private static readonly string ListSql = SqlLoader.Load("Activities.List");
    private static readonly string ListAllForExportSql = SqlLoader.Load("Activities.ListAllForExport");
    private static readonly string InsertSql = SqlLoader.Load("Activities.Insert");
    private static readonly string UpdateSql = SqlLoader.Load("Activities.Update");
    private static readonly string DeleteSql = SqlLoader.Load("Activities.Delete");
    private static readonly string SetTrackSql = SqlLoader.Load("Activities.SetTrack");
    private static readonly string SetHasElevationSql = SqlLoader.Load("Activities.SetHasElevation");
    private static readonly string EquipmentListByActivitySql = SqlLoader.Load("ActivityEquipment.ListByActivity");
    private static readonly string EquipmentDeleteByActivitySql = SqlLoader.Load("ActivityEquipment.DeleteByActivity");
    private static readonly string EquipmentInsertSql = SqlLoader.Load("ActivityEquipment.Insert");

    public async Task<Activity?> FindByIdAsync(Guid userId, Guid activityId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var command = new CommandDefinition(
            GetByIdSql, new { ActivityId = activityId, UserId = userId }, cancellationToken: ct);
        var row = await connection.QuerySingleOrDefaultAsync<ActivityRow>(command);
        if (row is null)
            return null;

        var equipmentIds = await ListEquipmentIdsAsync(connection, activityId, ct);
        return row.ToActivity(equipmentIds);
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

        var rows = (await connection.QueryAsync<ActivityRow>(command)).ToList();
        var hasMore = rows.Count > query.Limit;
        var page = hasMore ? rows.Take(query.Limit) : rows;

        return new ActivityPage(page.Select(r => r.ToActivity([])).ToList(), hasMore);
    }

    public async Task<IReadOnlyList<ActivityExportRow>> ListAllForExportAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var command = new CommandDefinition(ListAllForExportSql, new { UserId = userId }, cancellationToken: ct);
        var rows = await connection.QueryAsync<ActivityExportRow_>(command);
        return rows.Select(r => r.ToExportRow()).ToList();
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
                command.TakeoffLat,
                command.TakeoffLon,
                command.Comment,
                command.MaxAltitudeM,
                command.AltitudeGainM,
                command.DistanceKm,
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
            TakeoffLat = command.TakeoffLat,
            TakeoffLon = command.TakeoffLon,
            Comment = command.Comment,
            MaxAltitudeM = command.MaxAltitudeM,
            AltitudeGainM = command.AltitudeGainM,
            DistanceKm = command.DistanceKm,
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

        return await FindByIdAsync(userId, activityId, ct);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid activityId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(DeleteSql, new { Id = activityId, UserId = userId }, cancellationToken: ct);
        var rowsAffected = await connection.ExecuteAsync(command);
        return rowsAffected > 0;
    }

    public async Task<Activity?> SetTrackAsync(Guid userId, Guid activityId, TrackReference? track, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            SetTrackSql,
            new
            {
                Id = activityId,
                UserId = userId,
                track?.Filename,
                Format = track is null ? null : PgEnumTypeHandler<TrackFormat>.ToLabel(track.Format),
                track?.SizeBytes,
                track?.Sha256,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken: ct));

        if (rowsAffected == 0)
            return null;

        var equipmentIds = await ListEquipmentIdsAsync(connection, activityId, ct);
        var row = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
            GetByIdSql, new { ActivityId = activityId, UserId = userId }, cancellationToken: ct));
        return row?.ToActivity(equipmentIds);
    }

    public async Task<Activity?> SetHasElevationAsync(Guid userId, Guid activityId, bool hasElevation, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            SetHasElevationSql,
            new { Id = activityId, UserId = userId, HasElevation = hasElevation, UpdatedAt = DateTimeOffset.UtcNow },
            cancellationToken: ct));

        if (rowsAffected == 0)
            return null;

        var equipmentIds = await ListEquipmentIdsAsync(connection, activityId, ct);
        var row = await connection.QuerySingleOrDefaultAsync<ActivityRow>(new CommandDefinition(
            GetByIdSql, new { ActivityId = activityId, UserId = userId }, cancellationToken: ct));
        return row?.ToActivity(equipmentIds);
    }

    private static async Task<List<Guid>> ListEquipmentIdsAsync(NpgsqlConnection connection, Guid activityId, CancellationToken ct)
    {
        var equipmentCommand = new CommandDefinition(
            EquipmentListByActivitySql, new { ActivityId = activityId }, cancellationToken: ct);
        var equipmentIds = await connection.QueryAsync<Guid>(equipmentCommand);
        return equipmentIds.ToList();
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

    private static int? DurationSeconds(DateTimeOffset startedAt, DateTimeOffset? endedAt) =>
        endedAt is null ? null : (int)(endedAt.Value - startedAt).TotalSeconds;

    /// <summary>Row shape for the export query - same enum-as-text-column caveat as <see cref="ActivityRow"/>.</summary>
    private sealed class ActivityExportRow_
    {
        public Guid Id { get; init; }
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? EndedAt { get; init; }
        public DateOnly LocalDate { get; init; }
        public string? TakeoffLocation { get; init; }
        public string? LandingLocation { get; init; }
        public int? MaxAltitudeM { get; init; }
        public int? AltitudeGainM { get; init; }
        public double? DistanceKm { get; init; }
        public string? TrackFilename { get; init; }
        public string? TrackFormat { get; init; }
        public long? TrackSizeBytes { get; init; }
        public string? TrackSha256 { get; init; }
        public int? WindSpeedKmh { get; init; }
        public int? WindDirection { get; init; }
        public string? Comment { get; init; }
        public string EquipmentNames { get; init; } = "";

        public ActivityExportRow ToExportRow() => new(
            Id,
            PgEnumTypeHandler<ActivityType>.FromLabel(Type),
            Name,
            StartedAt,
            EndedAt,
            LocalDate,
            TakeoffLocation,
            LandingLocation,
            MaxAltitudeM,
            AltitudeGainM,
            DistanceKm,
            WindSpeedKmh,
            WindDirection,
            Comment,
            EquipmentNames,
            TrackFilename is null
                ? null
                : new TrackReference
                {
                    Filename = TrackFilename,
                    Format = PgEnumTypeHandler<TrackFormat>.FromLabel(TrackFormat!),
                    SizeBytes = TrackSizeBytes!.Value,
                    Sha256 = TrackSha256!,
                });
    }

    /// <summary>
    /// Flat row shape matching the SELECT columns. Enum columns are read as their raw text label
    /// (not the C# enum type): Dapper's generated deserializer calls Enum.Parse directly for an
    /// enum-typed property, bypassing any registered SqlMapper.TypeHandler, the same way its
    /// parameter binder bypasses one on the write side (see PgEnumTypeHandler). That happened to
    /// go unnoticed for single-word labels ("flight", "wing"...), which parse fine by coincidence
    /// - multi-word labels like "ground_handling" don't. Converted explicitly in ToActivity().
    /// </summary>
    private sealed class ActivityRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string Type { get; init; } = "";
        public string Name { get; init; } = "";
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? EndedAt { get; init; }
        public DateOnly LocalDate { get; init; }
        public string? LocalTz { get; init; }
        public string? TakeoffLocation { get; init; }
        public string? LandingLocation { get; init; }
        public double? TakeoffLat { get; init; }
        public double? TakeoffLon { get; init; }
        public string? Comment { get; init; }
        public int? MaxAltitudeM { get; init; }
        public int? AltitudeGainM { get; init; }
        public double? DistanceKm { get; init; }
        public string? TrackFilename { get; init; }
        public string? TrackFormat { get; init; }
        public long? TrackSizeBytes { get; init; }
        public string? TrackSha256 { get; init; }
        public bool HasElevation { get; init; }
        public int? WindSpeedKmh { get; init; }
        public int? WindDirection { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public int? DurationSeconds { get; init; }

        public Activity ToActivity(IReadOnlyList<Guid> equipmentIds) => new()
        {
            Id = Id,
            UserId = UserId,
            Type = PgEnumTypeHandler<ActivityType>.FromLabel(Type),
            Name = Name,
            StartedAt = StartedAt,
            EndedAt = EndedAt,
            LocalDate = LocalDate,
            LocalTz = LocalTz,
            TakeoffLocation = TakeoffLocation,
            LandingLocation = LandingLocation,
            TakeoffLat = TakeoffLat,
            TakeoffLon = TakeoffLon,
            Comment = Comment,
            MaxAltitudeM = MaxAltitudeM,
            AltitudeGainM = AltitudeGainM,
            DistanceKm = DistanceKm,
            Track = TrackFilename is null
                ? null
                : new TrackReference
                {
                    Filename = TrackFilename,
                    Format = PgEnumTypeHandler<TrackFormat>.FromLabel(TrackFormat!),
                    SizeBytes = TrackSizeBytes!.Value,
                    Sha256 = TrackSha256!,
                },
            HasElevation = HasElevation,
            WindSpeedKmh = WindSpeedKmh,
            WindDirection = WindDirection,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DurationSeconds = DurationSeconds,
            EquipmentIds = equipmentIds,
        };
    }
}
