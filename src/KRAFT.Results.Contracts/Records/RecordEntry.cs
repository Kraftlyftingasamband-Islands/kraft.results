namespace KRAFT.Results.Contracts.Records;

public sealed record class RecordEntry(
    int Id,
    string WeightCategory,
    string? Athlete,
    string? AthleteSlug,
    int? BirthYear,
    decimal? BodyWeight,
    decimal Weight,
    DateOnly Date,
    string? MeetSlug,
    bool IsStandard);