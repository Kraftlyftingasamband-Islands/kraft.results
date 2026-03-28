using KRAFT.Results.Contracts.TeamCompetition;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetTeamPointsResponse(
    bool IsGenderSplit,
    IReadOnlyList<TeamCompetitionStanding> Women,
    IReadOnlyList<TeamCompetitionStanding> Men,
    IReadOnlyList<TeamCompetitionStanding> Combined);