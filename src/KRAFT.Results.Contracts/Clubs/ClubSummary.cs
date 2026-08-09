namespace KRAFT.Results.Contracts.Clubs;

public sealed record class ClubSummary(
    string Slug,
    string Title,
    string ShortTitle,
    string? LogoImageFilename,
    int AthleteCount);
