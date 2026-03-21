namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordEntry(
    string WeightCategory,
    string? Athlete,
    string? AthleteSlug,
    int? BirthYear,
    string? Club,
    decimal? BodyWeight,
    decimal Weight,
    DateOnly Date,
    string? Meet,
    string? MeetSlug,
    bool IsStandard);