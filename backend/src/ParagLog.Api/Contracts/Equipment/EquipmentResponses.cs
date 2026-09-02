using ParagLog.Core.Equipment;

namespace ParagLog.Api.Contracts.Equipment;

public sealed record EquipmentSummaryResponse(
    Guid Id, string DisplayName, EquipmentType Type, bool AutoAdd, bool Retired)
{
    public static EquipmentSummaryResponse From(ParagLog.Core.Equipment.Equipment equipment) => new(
        equipment.Id, equipment.DisplayName, equipment.Type, equipment.AutoAdd, equipment.Retired);
}

public sealed record RevisionResponse(Guid Id, DateOnly RevisionDate, string? Comment)
{
    public static RevisionResponse From(EquipmentRevision revision) =>
        new(revision.Id, revision.RevisionDate, revision.Comment);
}

public sealed record EquipmentUsageResponse(int ActivityCount, double Hours)
{
    public static EquipmentUsageResponse From(EquipmentUsage usage) => new(usage.ActivityCount, usage.Hours);
}

public sealed record EquipmentResponse(
    Guid Id,
    string DisplayName,
    EquipmentType Type,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    DateOnly? NextRevisionDate,
    bool AutoAdd,
    bool Retired,
    IReadOnlyList<RevisionResponse> Revisions,
    EquipmentUsageResponse Usage)
{
    public static EquipmentResponse From(EquipmentDetail detail) => new(
        detail.Equipment.Id, detail.Equipment.DisplayName, detail.Equipment.Type, detail.Equipment.Brand,
        detail.Equipment.Model, detail.Equipment.PurchaseDate, detail.Equipment.NextRevisionDate,
        detail.Equipment.AutoAdd, detail.Equipment.Retired,
        detail.Equipment.Revisions.Select(RevisionResponse.From).ToList(),
        EquipmentUsageResponse.From(detail.Usage));

    public static EquipmentResponse FromWithoutUsage(ParagLog.Core.Equipment.Equipment equipment) => new(
        equipment.Id, equipment.DisplayName, equipment.Type, equipment.Brand,
        equipment.Model, equipment.PurchaseDate, equipment.NextRevisionDate,
        equipment.AutoAdd, equipment.Retired,
        equipment.Revisions.Select(RevisionResponse.From).ToList(),
        new EquipmentUsageResponse(0, 0));
}
