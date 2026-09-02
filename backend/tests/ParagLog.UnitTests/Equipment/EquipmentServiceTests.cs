using ParagLog.Core.Abstractions;
using ParagLog.Core.Common;
using ParagLog.Core.Equipment;

namespace ParagLog.UnitTests.Equipment;

public class EquipmentServiceTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static CreateEquipmentCommand ValidCreateCommand(string displayName = "My wing") =>
        new(displayName, EquipmentType.Wing, "Brand", "Model", null, null, false, []);

    [Fact]
    public async Task CreateAsync_rejects_blank_display_name()
    {
        var service = new EquipmentService(new FakeEquipmentRepository());

        var result = await service.CreateAsync(UserId, ValidCreateCommand(displayName: "  "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.Error!.Type);
        Assert.Equal("displayName", result.Error.Field);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_display_name()
    {
        var service = new EquipmentService(new FakeEquipmentRepository(displayNameExists: true));

        var result = await service.CreateAsync(UserId, ValidCreateCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_valid_command()
    {
        var service = new EquipmentService(new FakeEquipmentRepository());

        var result = await service.CreateAsync(UserId, ValidCreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("My wing", result.Value.DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_hard_deletes_when_usage_is_zero()
    {
        var repository = new FakeEquipmentRepository(usage: new EquipmentUsage(0, 0));
        var service = new EquipmentService(repository);

        var result = await service.DeleteAsync(UserId, repository.ExistingId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.Deleted);
    }

    [Fact]
    public async Task DeleteAsync_refuses_when_the_equipment_is_in_use()
    {
        var repository = new FakeEquipmentRepository(usage: new EquipmentUsage(3, 12.5));
        var service = new EquipmentService(repository);

        var result = await service.DeleteAsync(UserId, repository.ExistingId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Conflict, result.Error!.Type);
        Assert.False(repository.Deleted);
    }

    [Fact]
    public async Task DeleteAsync_returns_not_found_for_missing_equipment()
    {
        var service = new EquipmentService(new FakeEquipmentRepository(existing: false));

        var result = await service.DeleteAsync(UserId, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.Error!.Type);
    }

    private sealed class FakeEquipmentRepository(
        bool displayNameExists = false, bool existing = true, EquipmentUsage? usage = null) : IEquipmentRepository
    {
        public Guid ExistingId { get; } = Guid.NewGuid();
        public bool Deleted { get; private set; }

        public Task<IReadOnlyList<ParagLog.Core.Equipment.Equipment>> ListAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ParagLog.Core.Equipment.Equipment>>([]);

        public Task<ParagLog.Core.Equipment.Equipment?> FindByIdAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            Task.FromResult(existing ? MakeEquipment(equipmentId) : null);

        public Task<EquipmentUsage> GetUsageAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            Task.FromResult(usage ?? new EquipmentUsage(0, 0));

        public Task<bool> DisplayNameExistsAsync(Guid userId, string displayName, Guid? excludingId, CancellationToken ct) =>
            Task.FromResult(displayNameExists);

        public Task<int> CountOwnedAsync(Guid userId, IReadOnlyList<Guid> equipmentIds, CancellationToken ct) =>
            Task.FromResult(equipmentIds.Count);

        public Task<ParagLog.Core.Equipment.Equipment> CreateAsync(Guid userId, CreateEquipmentCommand command, CancellationToken ct) =>
            Task.FromResult(new ParagLog.Core.Equipment.Equipment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisplayName = command.DisplayName,
                Type = command.Type,
                Brand = command.Brand,
                Model = command.Model,
                PurchaseDate = command.PurchaseDate,
                NextRevisionDate = command.NextRevisionDate,
                AutoAdd = command.AutoAdd,
                Retired = false,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        public Task<ParagLog.Core.Equipment.Equipment?> UpdateAsync(
            Guid userId, Guid equipmentId, UpdateEquipmentCommand command, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<ParagLog.Core.Equipment.Equipment?> RetireAsync(Guid userId, Guid equipmentId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(Guid userId, Guid equipmentId, CancellationToken ct)
        {
            Deleted = true;
            return Task.FromResult(true);
        }

        private ParagLog.Core.Equipment.Equipment MakeEquipment(Guid id) => new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            DisplayName = "Existing",
            Type = EquipmentType.Wing,
            AutoAdd = false,
            Retired = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
