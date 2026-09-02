namespace ParagLog.Core.Equipment;

public sealed record RevisionInput(DateOnly RevisionDate, string? Comment);

public sealed record CreateEquipmentCommand(
    string DisplayName,
    EquipmentType Type,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    DateOnly? NextRevisionDate,
    bool AutoAdd,
    IReadOnlyList<RevisionInput> Revisions
);

public sealed record UpdateEquipmentCommand(
    string DisplayName,
    EquipmentType Type,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    DateOnly? NextRevisionDate,
    bool AutoAdd,
    IReadOnlyList<RevisionInput> Revisions
);
