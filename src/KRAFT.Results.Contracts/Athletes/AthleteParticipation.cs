namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteParticipation(
    DateOnly Date,
    string Meet,
    string MeetSlug,
    string MeetType,
    string? Club,
    string? ClubSlug,
    int Rank,
    string WeightCategory,
    decimal BodyWeight,
    decimal Squat,
    decimal Benchpress,
    decimal Deadlift,
    decimal Total,
    decimal Wilks,
    decimal? IpfPoints,
    bool IsDisqualified);