namespace KRAFT.Results.Contracts.Athletes;

public sealed record AthleteRecordMatch(
    string AgeCategory,
    string WeightCategory,
    bool IsWithinPowerlifting,
    bool IsSingleLift,
    bool IsStandaloneDiscipline);
