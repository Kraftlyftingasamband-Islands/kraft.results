namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthletePersonalBest(
    bool IsClassic,
    string Discipline,
    decimal Weight,
    string WeightCategory,
    decimal BodyWeight,
    string Meet,
    string MeetSlug,
    string MeetType,
    DateOnly Date);