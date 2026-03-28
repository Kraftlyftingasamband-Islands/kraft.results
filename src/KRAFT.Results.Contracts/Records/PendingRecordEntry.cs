namespace KRAFT.Results.Contracts.Records;

public sealed record class PendingRecordEntry(
    int AttemptId,
    string AthleteName,
    string Discipline,
    decimal Weight,
    string WeightCategory,
    string AgeCategory,
    decimal? CurrentRecordWeight);