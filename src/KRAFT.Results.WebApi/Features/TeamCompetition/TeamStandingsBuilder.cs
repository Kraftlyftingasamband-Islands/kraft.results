using KRAFT.Results.Contracts.TeamCompetition;

namespace KRAFT.Results.WebApi.Features.TeamCompetition;

internal static class TeamStandingsBuilder
{
    internal const int GenderSplitStartYear = 2015;
    internal const int BestNThresholdYear = 2012;
    internal const int BestNModern = 5;
    internal const int BestNLegacy = 6;

    private static readonly int[] TiebreakerPointValues = [12, 9, 8, 7, 6, 5, 4, 3, 2, 1];

    internal static int GetBestN(int year) =>
        year >= BestNThresholdYear ? BestNModern : BestNLegacy;

    internal static List<TeamCompetitionStanding> BuildStandings(
        IEnumerable<TeamPointRow> rows, int bestN)
    {
        List<TeamAggregate> teamAggregates = rows
            .GroupBy(r => r.TeamId)
            .Select(g =>
            {
                List<List<int>> perMeetBestPoints = g
                    .GroupBy(r => r.MeetId)
                    .Select(mg => mg
                        .Select(r => r.Points)
                        .OrderByDescending(p => p)
                        .Take(bestN)
                        .ToList())
                    .ToList();

                List<int> allBestPoints = perMeetBestPoints
                    .SelectMany(p => p)
                    .ToList();

                int totalPoints = allBestPoints.Sum();

                Dictionary<int, int> tiebreakerCounts = new();
                foreach (int pointValue in TiebreakerPointValues)
                {
                    tiebreakerCounts[pointValue] = allBestPoints.Count(p => p == pointValue);
                }

                TeamPointRow first = g.First();

                return new TeamAggregate(
                    first.TeamName,
                    first.TeamTitleShort,
                    first.TeamSlug,
                    first.LogoImageFilename,
                    totalPoints,
                    tiebreakerCounts,
                    allBestPoints.OrderByDescending(p => p).ToList());
            })
            .OrderByDescending(t => t.TotalPoints)
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(12))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(9))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(8))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(7))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(6))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(5))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(4))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(3))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(2))
            .ThenByDescending(t => t.TiebreakerCounts.GetValueOrDefault(1))
            .ToList();

        List<TeamCompetitionStanding> standings = [];
        int currentRank = 1;

        for (int i = 0; i < teamAggregates.Count; i++)
        {
            TeamAggregate team = teamAggregates[i];

            if (i > 0 && !AreEqual(teamAggregates[i - 1], team))
            {
                currentRank = i + 1;
            }

            standings.Add(new TeamCompetitionStanding(
                currentRank,
                team.TeamName,
                team.TeamTitleShort,
                team.TeamSlug,
                team.LogoImageFilename,
                team.TotalPoints,
                team.AllBestPoints));
        }

        return standings;
    }

    private static bool AreEqual(TeamAggregate a, TeamAggregate b)
    {
        if (a.TotalPoints != b.TotalPoints)
        {
            return false;
        }

        return TiebreakerPointValues.All(pointValue =>
            a.TiebreakerCounts.GetValueOrDefault(pointValue) ==
            b.TiebreakerCounts.GetValueOrDefault(pointValue));
    }

    internal sealed record TeamPointRow(
        int TeamId,
        string TeamName,
        string TeamTitleShort,
        string TeamSlug,
        string? LogoImageFilename,
        string Gender,
        int MeetId,
        int Points);

    internal sealed record TeamAggregate(
        string TeamName,
        string TeamTitleShort,
        string TeamSlug,
        string? LogoImageFilename,
        int TotalPoints,
        Dictionary<int, int> TiebreakerCounts,
        IReadOnlyList<int> AllBestPoints);
}