using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.TeamCompetition.Get;

internal sealed class GetTeamCompetitionHandler
{
    private const int GenderSplitStartYear = 2015;
    private const int BestNThresholdYear = 2012;
    private const int BestNModern = 5;
    private const int BestNLegacy = 6;

    private static readonly int[] TiebreakerPointValues = [12, 9, 8, 7, 6, 5, 4, 3, 2, 1];

    private readonly ResultsDbContext _dbContext;

    public GetTeamCompetitionHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeamCompetitionResponse> Handle(int year, CancellationToken cancellationToken)
    {
        bool isGenderSplit = year >= GenderSplitStartYear;
        int bestN = year >= BestNThresholdYear ? BestNModern : BestNLegacy;

        List<TeamPointRow> rows = await _dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.IsInTeamCompetition)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.TeamId != null)
            .Where(p => p.TeamPoints != null && p.TeamPoints > 0)
            .Select(p => new TeamPointRow(
                p.TeamId!.Value,
                p.Team!.Title,
                p.Team.Slug,
                p.Team.LogoImageFilename,
                p.Athlete.Gender.Value,
                p.TeamPoints!.Value))
            .ToListAsync(cancellationToken);

        if (isGenderSplit)
        {
            List<TeamCompetitionStanding> women = BuildStandings(
                rows.Where(r => r.Gender == "f"), bestN);

            List<TeamCompetitionStanding> men = BuildStandings(
                rows.Where(r => r.Gender == "m"), bestN);

            return new TeamCompetitionResponse(year, true, women, men, []);
        }

        List<TeamCompetitionStanding> combined = BuildStandings(rows, bestN);
        return new TeamCompetitionResponse(year, false, [], [], combined);
    }

    private static List<TeamCompetitionStanding> BuildStandings(
        IEnumerable<TeamPointRow> rows, int bestN)
    {
        List<TeamAggregate> teamAggregates = rows
            .GroupBy(r => r.TeamId)
            .Select(g =>
            {
                List<int> allPoints = g
                    .Select(r => r.Points)
                    .OrderByDescending(p => p)
                    .ToList();

                List<int> bestPoints = allPoints.Take(bestN).ToList();
                int totalPoints = bestPoints.Sum();

                Dictionary<int, int> tiebreakerCounts = new();
                foreach (int pointValue in TiebreakerPointValues)
                {
                    tiebreakerCounts[pointValue] = bestPoints.Count(p => p == pointValue);
                }

                TeamPointRow first = g.First();

                return new TeamAggregate(
                    first.TeamName,
                    first.TeamSlug,
                    first.LogoImageFilename,
                    totalPoints,
                    tiebreakerCounts);
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
                team.TeamSlug,
                team.LogoImageFilename,
                team.TotalPoints));
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

    private sealed record TeamPointRow(
        int TeamId,
        string TeamName,
        string TeamSlug,
        string? LogoImageFilename,
        string Gender,
        int Points);

    private sealed record TeamAggregate(
        string TeamName,
        string TeamSlug,
        string? LogoImageFilename,
        int TotalPoints,
        Dictionary<int, int> TiebreakerCounts);
}