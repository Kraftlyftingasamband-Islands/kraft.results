namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteDetails(
    string Slug,
    string Name,
    int? YearOfBirth,
    string? Club,
    string? ClubSlug,
    int RecordCount,
    IReadOnlyList<AthletePersonalBest> PersonalBests,
    IReadOnlyList<AthleteRecord> Records,
    IReadOnlyList<AthleteParticipation> Participations);