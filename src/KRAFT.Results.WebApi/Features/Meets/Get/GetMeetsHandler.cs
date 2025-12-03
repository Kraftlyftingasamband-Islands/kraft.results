using KRAFT.Results.Contracts.Meets;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.Get;

internal sealed class GetMeetsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetMeetsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MeetSummary>> Handle(int? year, CancellationToken cancellationToken)
    {
        IQueryable<Meet> query = _dbContext.Set<Meet>();

        if (year is not null && year > 0)
        {
            query = query.Where(x => x.StartDate.Year == year);
        }

        return query
            .OrderBy(x => x.StartDate)
            .Select(x => new MeetSummary(x.Slug, x.Title, x.Location, DateOnly.FromDateTime(x.StartDate)))
            .ToListAsync(cancellationToken);
    }
}