using KRAFT.Results.Contracts.Meets;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.Get;

internal sealed class GetMeetsHandler(ResultsDbContext dbContext)
{
    public async Task<List<MeetSummary>> Handle(int? year, CancellationToken cancellationToken)
    {
        IQueryable<Meet> query = dbContext.Set<Meet>();

        if (year is not null && year > 0)
        {
            query = query.Where(x => x.StartDate.Year == year);
        }

        List<MeetProjection> raw = await query
            .OrderBy(x => x.StartDate)
            .Select(x => new MeetProjection(
                x.Slug,
                x.Title,
                x.Location,
                DateOnly.FromDateTime(x.StartDate),
                x.Category,
                x.IsRaw,
                x.Participations.Count))
            .ToListAsync(cancellationToken);

        return raw
            .Select(x => x.ToMeetSummary())
            .ToList();
    }
}