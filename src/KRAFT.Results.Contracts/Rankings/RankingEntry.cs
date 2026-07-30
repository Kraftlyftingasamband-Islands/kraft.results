namespace KRAFT.Results.Contracts.Rankings;

public sealed record class RankingEntry(
    int Rank,
    string Athlete,
    string AthleteSlug,
    decimal BodyWeight,
    decimal? IpfPoints,
    string MeetSlug);
