namespace KRAFT.Results.Contracts.Teams;

public sealed record class TeamSummary(
    string Slug,
    string Title,
    string ShortTitle,
    string? LogoImageFilename,
    int AthleteCount);