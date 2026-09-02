namespace ParagLog.Core.Equipment;

public sealed class EquipmentRevision
{
    public required Guid Id { get; init; }
    public required Guid EquipmentId { get; init; }
    public required DateOnly RevisionDate { get; init; }
    public string? Comment { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
