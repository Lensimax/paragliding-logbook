using Dapper;
using Npgsql;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Equipment;
using ParagLog.Infrastructure.Persistence.TypeHandlers;

namespace ParagLog.Infrastructure.Persistence;

public sealed class EquipmentRepository(NpgsqlConnectionFactory connectionFactory) : IEquipmentRepository
{
    private static readonly string ListSql = SqlLoader.Load("Equipment.List");
    private static readonly string GetByIdSql = SqlLoader.Load("Equipment.GetById");
    private static readonly string UsageSql = SqlLoader.Load("Equipment.Usage");
    private static readonly string DisplayNameExistsSql = SqlLoader.Load("Equipment.DisplayNameExists");
    private static readonly string CountOwnedSql = SqlLoader.Load("Equipment.CountOwned");
    private static readonly string InsertSql = SqlLoader.Load("Equipment.Insert");
    private static readonly string UpdateSql = SqlLoader.Load("Equipment.Update");
    private static readonly string RetireSql = SqlLoader.Load("Equipment.Retire");
    private static readonly string DeleteSql = SqlLoader.Load("Equipment.Delete");
    private static readonly string RevisionsListSql = SqlLoader.Load("EquipmentRevisions.ListByEquipment");
    private static readonly string RevisionsDeleteSql = SqlLoader.Load("EquipmentRevisions.DeleteByEquipment");
    private static readonly string RevisionsInsertSql = SqlLoader.Load("EquipmentRevisions.Insert");

    public async Task<IReadOnlyList<Equipment>> ListAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(ListSql, new { UserId = userId }, cancellationToken: ct);
        var rows = await connection.QueryAsync<Equipment>(command);
        return rows.ToList();
    }

    public async Task<Equipment?> FindByIdAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);

        var command = new CommandDefinition(
            GetByIdSql, new { EquipmentId = equipmentId, UserId = userId }, cancellationToken: ct);
        var item = await connection.QuerySingleOrDefaultAsync<Equipment>(command);
        if (item is null)
            return null;

        var revisions = await ListRevisionsAsync(connection, equipmentId, ct);
        return WithRevisions(item, revisions);
    }

    public async Task<EquipmentUsage> GetUsageAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            UsageSql, new { EquipmentId = equipmentId, UserId = userId }, cancellationToken: ct);
        var usage = await connection.QuerySingleOrDefaultAsync<EquipmentUsage>(command);
        return usage ?? new EquipmentUsage(0, 0);
    }

    public async Task<bool> DisplayNameExistsAsync(Guid userId, string displayName, Guid? excludingId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            DisplayNameExistsSql,
            new { UserId = userId, DisplayName = displayName, ExcludingId = excludingId },
            cancellationToken: ct);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<int> CountOwnedAsync(Guid userId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct)
    {
        if (equipmentIds.Count == 0)
            return 0;

        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            CountOwnedSql, new { UserId = userId, EquipmentIds = equipmentIds.ToArray() }, cancellationToken: ct);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<Equipment> CreateAsync(Guid userId, CreateEquipmentCommand command, CancellationToken ct)
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
                command.DisplayName,
                Type = PgEnumTypeHandler<EquipmentType>.ToLabel(command.Type),
                command.Brand,
                command.Model,
                command.PurchaseDate,
                command.NextRevisionDate,
                command.AutoAdd,
                CreatedAt = now,
            },
            transaction,
            cancellationToken: ct));

        var revisions = await InsertRevisionsAsync(connection, transaction, id, command.Revisions, ct);

        await transaction.CommitAsync(ct);

        return new Equipment
        {
            Id = id,
            UserId = userId,
            DisplayName = command.DisplayName,
            Type = command.Type,
            Brand = command.Brand,
            Model = command.Model,
            PurchaseDate = command.PurchaseDate,
            NextRevisionDate = command.NextRevisionDate,
            AutoAdd = command.AutoAdd,
            Retired = false,
            CreatedAt = now,
            Revisions = revisions,
        };
    }

    public async Task<Equipment?> UpdateAsync(
        Guid userId, Guid equipmentId, UpdateEquipmentCommand command, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            UpdateSql,
            new
            {
                Id = equipmentId,
                UserId = userId,
                command.DisplayName,
                Type = PgEnumTypeHandler<EquipmentType>.ToLabel(command.Type),
                command.Brand,
                command.Model,
                command.PurchaseDate,
                command.NextRevisionDate,
                command.AutoAdd,
            },
            transaction,
            cancellationToken: ct));

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync(ct);
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            RevisionsDeleteSql, new { EquipmentId = equipmentId }, transaction, cancellationToken: ct));
        var revisions = await InsertRevisionsAsync(connection, transaction, equipmentId, command.Revisions, ct);

        await transaction.CommitAsync(ct);

        var updated = await FindByIdCoreAsync(connection, userId, equipmentId, ct);
        return updated is null ? null : WithRevisions(updated, revisions);
    }

    public async Task<Equipment?> RetireAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            RetireSql, new { EquipmentId = equipmentId, UserId = userId }, cancellationToken: ct);
        var rowsAffected = await connection.ExecuteAsync(command);
        if (rowsAffected == 0)
            return null;

        var item = await FindByIdCoreAsync(connection, userId, equipmentId, ct);
        if (item is null)
            return null;

        var revisions = await ListRevisionsAsync(connection, equipmentId, ct);
        return WithRevisions(item, revisions);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            DeleteSql, new { EquipmentId = equipmentId, UserId = userId }, cancellationToken: ct);
        var rowsAffected = await connection.ExecuteAsync(command);
        return rowsAffected > 0;
    }

    private static async Task<Equipment?> FindByIdCoreAsync(
        NpgsqlConnection connection, Guid userId, Guid equipmentId, CancellationToken ct)
    {
        var command = new CommandDefinition(
            GetByIdSql, new { EquipmentId = equipmentId, UserId = userId }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Equipment>(command);
    }

    private static async Task<List<EquipmentRevision>> ListRevisionsAsync(
        NpgsqlConnection connection, Guid equipmentId, CancellationToken ct)
    {
        var command = new CommandDefinition(RevisionsListSql, new { EquipmentId = equipmentId }, cancellationToken: ct);
        var revisions = await connection.QueryAsync<EquipmentRevision>(command);
        return revisions.ToList();
    }

    private static async Task<List<EquipmentRevision>> InsertRevisionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid equipmentId,
        IReadOnlyList<RevisionInput> revisions,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = revisions
            .Select(r => new EquipmentRevision
            {
                Id = Guid.NewGuid(),
                EquipmentId = equipmentId,
                RevisionDate = r.RevisionDate,
                Comment = r.Comment,
                CreatedAt = now,
            })
            .ToList();

        if (rows.Count > 0)
        {
            var parameters = rows.Select(r => new
            {
                r.Id,
                EquipmentId = equipmentId,
                r.RevisionDate,
                r.Comment,
                r.CreatedAt,
            });
            await connection.ExecuteAsync(new CommandDefinition(RevisionsInsertSql, parameters, transaction, cancellationToken: ct));
        }

        return rows.OrderByDescending(r => r.RevisionDate).ToList();
    }

    private static Equipment WithRevisions(Equipment equipment, IReadOnlyList<EquipmentRevision> revisions) => new()
    {
        Id = equipment.Id,
        UserId = equipment.UserId,
        DisplayName = equipment.DisplayName,
        Type = equipment.Type,
        Brand = equipment.Brand,
        Model = equipment.Model,
        PurchaseDate = equipment.PurchaseDate,
        NextRevisionDate = equipment.NextRevisionDate,
        AutoAdd = equipment.AutoAdd,
        Retired = equipment.Retired,
        CreatedAt = equipment.CreatedAt,
        Revisions = revisions,
    };
}
