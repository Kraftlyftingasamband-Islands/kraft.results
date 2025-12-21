namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteRecord(
    DateOnly Date,
    bool IsClassic,
    bool IsSingleLift,
    string WeightCategory,
    string AgeCategory,
    string Type,
    decimal Weight,
    string Meet,
    string MeetSlug);