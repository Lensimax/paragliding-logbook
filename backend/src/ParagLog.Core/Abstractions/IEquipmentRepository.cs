using ParagLog.Core.Equipment;

namespace ParagLog.Core.Abstractions;

public interface IEquipmentRepository
{
    Task<IReadOnlyList<ParagLog.Core.Equipment.Equipment>> ListAsync(Guid userId, CancellationToken ct);
    Task<ParagLog.Core.Equipment.Equipment?> FindByIdAsync(Guid userId, Guid equipmentId, CancellationToken ct);
    Task<EquipmentUsage> GetUsageAsync(Guid userId, Guid equipmentId, CancellationToken ct);
    Task<bool> DisplayNameExistsAsync(Guid userId, string displayName, Guid? excludingId, CancellationToken ct);
    Task<int> CountOwnedAsync(Guid userId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct);
    Task<ParagLog.Core.Equipment.Equipment> CreateAsync(Guid userId, CreateEquipmentCommand command, CancellationToken ct);

    Task<ParagLog.Core.Equipment.Equipment?> UpdateAsync(
        Guid userId, Guid equipmentId, UpdateEquipmentCommand command, CancellationToken ct);

    Task<ParagLog.Core.Equipment.Equipment?> RetireAsync(Guid userId, Guid equipmentId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid userId, Guid equipmentId, CancellationToken ct);
}
