namespace ParagLog.Core.Equipment;

public sealed class Equipment
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public required EquipmentType Type { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public DateOnly? PurchaseDate { get; init; }
    public DateOnly? NextRevisionDate { get; init; }
    public required bool AutoAdd { get; init; }
    public required bool Retired { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<EquipmentRevision> Revisions { get; init; } = [];
}
