namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordHistoryResponse(
    string Category,
    string WeightCategory,
    string AgeCategory,
    string Gender,
    string EquipmentType,
    string Era,
    IReadOnlyList<RecordHistoryEntry> Entries);