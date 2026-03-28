using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.TeamCompetition;

using Microsoft.EntityFrameworkCore;

using static KRAFT.Results.WebApi.Features.TeamCompetition.TeamStandingsBuilder;

namespace KRAFT.Results.WebApi.Features.Meets.GetTeamPoints;

internal sealed class GetMeetTeamPointsHandler(ResultsDbContext dbContext)
{
    public async Task<MeetTeamPointsResponse?> Handle(string slug, CancellationToken cancellationToken)
    {
        Meet? meet = await dbContext.Set<Meet>()
            .Where(m => m.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (meet is null)
        {
            return null;
        }

        int year = meet.StartDate.Year;
        bool isGenderSplit = year >= GenderSplitStartYear;
        int bestN = GetBestN(year);

        List<TeamPointRow> rows = await dbContext.Set<Participation>()
            .Where(p => p.Meet.Slug == slug)
            .Where(p => !p.Disqualified)
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

            return new MeetTeamPointsResponse(true, women, men, []);
        }

        List<TeamCompetitionStanding> combined = BuildStandings(rows, bestN);
        return new MeetTeamPointsResponse(false, [], [], combined);
    }
}