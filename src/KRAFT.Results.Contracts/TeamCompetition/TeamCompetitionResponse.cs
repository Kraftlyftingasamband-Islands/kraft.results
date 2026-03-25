namespace KRAFT.Results.Contracts.TeamCompetition;

public sealed record class TeamCompetitionResponse(
    int Year,
    bool IsGenderSplit,
    IReadOnlyList<TeamCompetitionStanding> Women,
    IReadOnlyList<TeamCompetitionStanding> Men,
    IReadOnlyList<TeamCompetitionStanding> Combined);