namespace KRAFT.Results.Contracts.Clubs;

public sealed record class ClubMember(
    string Slug,
    string Name,
    int? YearOfBirth,
    int ParticipationCount,
    string? ProfileImageFilename);
