namespace ParagLog.Core.Equipment;

public sealed record EquipmentUsage(int ActivityCount, double Hours);

public sealed record EquipmentDetail(Equipment Equipment, EquipmentUsage Usage);
