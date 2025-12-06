namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthletePersonalBest(
    bool IsClassic,
    bool IsSingleLift,
    string Discipline,
    decimal Weight,
    string WeightCategory,
    decimal BodyWeight,
    string MeetSlug,
    string MeetType,
    DateOnly Date);