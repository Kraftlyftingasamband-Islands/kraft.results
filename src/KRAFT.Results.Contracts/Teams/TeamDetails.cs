namespace KRAFT.Results.Contracts.Teams;

public sealed record class TeamDetails(
    string Slug,
    string Title,
    string ShortTitle,
    string FullTitle,
    int? CountryId,
    IReadOnlyList<TeamMember> Members);