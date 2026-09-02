using ParagLog.Core.Equipment;

namespace ParagLog.Api.Contracts.Equipment;

public sealed record RevisionRequest(DateOnly RevisionDate, string? Comment)
{
    public RevisionInput ToInput() => new(RevisionDate, Comment);
}

public sealed record CreateEquipmentRequest(
    string DisplayName,
    EquipmentType Type,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    DateOnly? NextRevisionDate,
    bool AutoAdd,
    IReadOnlyList<RevisionRequest>? Revisions)
{
    public CreateEquipmentCommand ToCommand() => new(
        DisplayName, Type, Brand, Model, PurchaseDate, NextRevisionDate, AutoAdd,
        Revisions?.Select(r => r.ToInput()).ToList() ?? []);
}

public sealed record UpdateEquipmentRequest(
    string DisplayName,
    EquipmentType Type,
    string? Brand,
    string? Model,
    DateOnly? PurchaseDate,
    DateOnly? NextRevisionDate,
    bool AutoAdd,
    IReadOnlyList<RevisionRequest>? Revisions)
{
    public UpdateEquipmentCommand ToCommand() => new(
        DisplayName, Type, Brand, Model, PurchaseDate, NextRevisionDate, AutoAdd,
        Revisions?.Select(r => r.ToInput()).ToList() ?? []);
}
