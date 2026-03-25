namespace KRAFT.Results.Contracts.TeamCompetition;

public sealed record class TeamCompetitionStanding(
    int Rank,
    string TeamName,
    string? TeamSlug,
    string? LogoImageFilename,
    int TotalPoints);