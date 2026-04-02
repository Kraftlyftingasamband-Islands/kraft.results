namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetParticipation(
    int ParticipationId,
    int MeetId,
    int Rank,
    string Athlete,
    string AthleteSlug,
    string Gender,
    int YearOfBirth,
    string AgeCategory,
    string AgeCategorySlug,
    string WeightCategory,
    string Club,
    string ClubSlug,
    decimal BodyWeight,
    decimal Total,
    decimal IpfPoints,
    bool Disqualified,
    IEnumerable<MeetAttempt> Attempts);