namespace KRAFT.Results.Contracts.Clubs;

public sealed record class ClubDetails(
    string Slug,
    string Title,
    string ShortTitle,
    string FullTitle,
    string CountryCode,
    string? LogoImageFilename,
    IReadOnlyList<ClubMember> Members);
