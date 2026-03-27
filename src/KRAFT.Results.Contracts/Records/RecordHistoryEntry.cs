namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordHistoryEntry(
    DateOnly Date,
    string? Athlete,
    string? AthleteSlug,
    string? Club,
    decimal Weight,
    decimal? BodyWeight,
    string? Meet,
    string? MeetSlug,
    bool IsCurrent,
    bool IsStandard);