using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

using static KRAFT.Results.WebApi.Features.TeamCompetition.TeamStandingsBuilder;

namespace KRAFT.Results.WebApi.Features.TeamCompetition.Get;

internal sealed class GetTeamCompetitionHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetTeamCompetitionHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeamCompetitionResponse> Handle(int year, CancellationToken cancellationToken)
    {
        bool isGenderSplit = year >= GenderSplitStartYear;
        int bestN = GetBestN(year);

        List<TeamPointRow> rows = await _dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.IsInTeamCompetition)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.TeamId != null)
            .Where(p => p.TeamPoints != null && p.TeamPoints > 0)
            .Select(p => new TeamPointRow(
                p.TeamId!.Value,
                p.Team!.Title,
                p.Team!.TitleShort,
                p.Team.Slug,
                p.Team.LogoImageFilename,
                p.Athlete.Gender.Value,
                p.MeetId,
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
}
