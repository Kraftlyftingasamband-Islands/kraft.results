namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordGroup(
    string Category,
    IReadOnlyList<RecordEntry> Records);