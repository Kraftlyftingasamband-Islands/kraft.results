namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordHistoryEntry(
    DateOnly Date,
    string? Athlete,
    string? AthleteSlug,
    decimal Weight,
    decimal? BodyWeight,
    string? Meet,
    string? MeetSlug,
    bool IsCurrent,
    bool IsStandard,
    decimal? Delta);