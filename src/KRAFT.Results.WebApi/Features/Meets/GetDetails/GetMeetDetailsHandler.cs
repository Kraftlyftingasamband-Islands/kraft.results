using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetDetails;

internal sealed class GetMeetDetailsHandler(ResultsDbContext dbContext)
{
    public async Task<MeetDetails?> Handle(string slug, CancellationToken cancellationToken)
    {
        var raw = await dbContext.Set<Meet>()
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                MeetId = EF.Property<int>(x, "MeetId"),
                x.Title,
                x.Slug,
                Location = x.Location ?? string.Empty,
                Text = x.Text ?? string.Empty,
                x.StartDate,
                x.EndDate,
                MeetTypeTitle = x.MeetType.Title,
                x.MeetType.MeetTypeId,
                x.ResultModeId,
                x.CalcPlaces,
                x.IsInTeamCompetition,
                x.ShowWilks,
                x.ShowTeams,
                x.ShowBodyWeight,
                x.PublishedInCalendar,
                x.PublishedResults,
                x.RecordsPossible,
                x.IsRaw,
                x.ShowTeamPoints,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
        {
            return null;
        }

        return new MeetDetails(
            raw.MeetId,
            raw.Title,
            raw.Slug,
            raw.Location,
            raw.Text,
            DateOnly.FromDateTime(raw.StartDate),
            DateOnly.FromDateTime(raw.EndDate),
            raw.MeetTypeTitle,
            raw.MeetTypeId,
            ((ResultMode)raw.ResultModeId).ToString(),
            raw.CalcPlaces,
            raw.IsInTeamCompetition,
            raw.ShowWilks,
            raw.ShowTeams,
            raw.ShowBodyWeight,
            raw.PublishedInCalendar,
            raw.PublishedResults,
            raw.RecordsPossible,
            raw.IsRaw,
            raw.ShowTeamPoints,
            MeetDisciplineResolver.ResolveDisciplines(raw.MeetTypeId, raw.MeetTypeTitle));
    }
}