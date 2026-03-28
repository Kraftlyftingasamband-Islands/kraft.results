using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetDetails;

internal sealed class GetMeetDetailsHandler(ResultsDbContext dbContext)
{
    public Task<MeetDetails?> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Meet>()
            .Where(x => x.Slug == slug)
            .Select(x => new MeetDetails(
                x.Title,
                x.Slug,
                x.Location ?? string.Empty,
                x.Text ?? string.Empty,
                DateOnly.FromDateTime(x.StartDate),
                DateOnly.FromDateTime(x.EndDate),
                x.MeetType.Title,
                x.MeetType.MeetTypeId,
                ((ResultMode)x.ResultModeId).ToString(),
                x.CalcPlaces,
                x.IsInTeamCompetition,
                x.ShowWilks,
                x.ShowTeams,
                x.ShowBodyWeight,
                x.PublishedInCalendar,
                x.PublishedResults,
                x.RecordsPossible,
                x.IsRaw,
                x.ShowTeamPoints))
            .FirstOrDefaultAsync(cancellationToken);
}