using ParagLog.Core.Abstractions;
using ParagLog.Core.Common;

namespace ParagLog.Core.Equipment;

public sealed class EquipmentService(IEquipmentRepository equipment)
{
    public Task<IReadOnlyList<Equipment>> ListAsync(Guid userId, CancellationToken ct) =>
        equipment.ListAsync(userId, ct);

    public async Task<EquipmentDetail?> GetAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        var item = await equipment.FindByIdAsync(userId, equipmentId, ct);
        if (item is null)
            return null;

        var usage = await equipment.GetUsageAsync(userId, equipmentId, ct);
        return new EquipmentDetail(item, usage);
    }

    public async Task<Result<Equipment>> CreateAsync(Guid userId, CreateEquipmentCommand command, CancellationToken ct)
    {
        var validation = Validate(command.DisplayName, command.Revisions);
        if (validation is not null)
            return Result<Equipment>.Failure(validation);

        if (await equipment.DisplayNameExistsAsync(userId, command.DisplayName, null, ct))
            return Result<Equipment>.Failure(
                DomainError.Conflict("You already have equipment with this name.", "displayName"));

        var created = await equipment.CreateAsync(userId, command, ct);
        return Result<Equipment>.Success(created);
    }

    public async Task<Result<Equipment>> UpdateAsync(
        Guid userId, Guid equipmentId, UpdateEquipmentCommand command, CancellationToken ct)
    {
        var validation = Validate(command.DisplayName, command.Revisions);
        if (validation is not null)
            return Result<Equipment>.Failure(validation);

        if (await equipment.DisplayNameExistsAsync(userId, command.DisplayName, equipmentId, ct))
            return Result<Equipment>.Failure(
                DomainError.Conflict("You already have equipment with this name.", "displayName"));

        var updated = await equipment.UpdateAsync(userId, equipmentId, command, ct);
        return updated is null
            ? Result<Equipment>.Failure(DomainError.NotFound("Equipment not found."))
            : Result<Equipment>.Success(updated);
    }

    public async Task<Result<Equipment>> RetireAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        var retired = await equipment.RetireAsync(userId, equipmentId, ct);
        return retired is null
            ? Result<Equipment>.Failure(DomainError.NotFound("Equipment not found."))
            : Result<Equipment>.Success(retired);
    }

    /// <summary>
    /// Usage is zero: hard delete. Usage is non-zero: refused (409) - the UI offers Retire
    /// instead, since cascading the delete would silently rewrite the logbook.
    /// </summary>
    public async Task<Result> DeleteAsync(Guid userId, Guid equipmentId, CancellationToken ct)
    {
        var item = await equipment.FindByIdAsync(userId, equipmentId, ct);
        if (item is null)
            return Result.Failure(DomainError.NotFound("Equipment not found."));

        var usage = await equipment.GetUsageAsync(userId, equipmentId, ct);
        if (usage.ActivityCount > 0)
            return Result.Failure(DomainError.Conflict(
                $"This equipment is used by {usage.ActivityCount} activit{(usage.ActivityCount == 1 ? "y" : "ies")}. Retire it instead of deleting."));

        var deleted = await equipment.DeleteAsync(userId, equipmentId, ct);
        return deleted ? Result.Success() : Result.Failure(DomainError.NotFound("Equipment not found."));
    }

    private static DomainError? Validate(string displayName, IReadOnlyList<RevisionInput> revisions)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return DomainError.Validation("Display name is required.", "displayName");

        if (revisions.Any(r => r.RevisionDate == default))
            return DomainError.Validation("Every revision needs a date.", "revisions");

        return null;
    }
}
