namespace KRAFT.Results.Contracts.Rankings;

public sealed record class RankingEntry(
    int Rank,
    string Athlete,
    string AthleteSlug,
    string Gender,
    decimal Result,
    string WeightCategory,
    decimal BodyWeight,
    decimal? IpfPoints,
    decimal Wilks,
    string MeetSlug,
    bool IsClassic,
    DateOnly MeetDate);