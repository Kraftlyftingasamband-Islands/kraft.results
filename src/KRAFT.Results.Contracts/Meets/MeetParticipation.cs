namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetParticipation(
    int Rank,
    string Name,
    string Gender,
    int YearOfBirth,
    string AgeCategory,
    string WeightCategory,
    string Club,
    string ClubSlug,
    decimal BodyWeight,
    decimal Total,
    decimal IpfPoints,
    IEnumerable<MeetAttempt> Attempts);