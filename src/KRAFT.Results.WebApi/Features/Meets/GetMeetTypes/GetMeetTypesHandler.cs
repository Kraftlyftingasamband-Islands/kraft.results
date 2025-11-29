using KRAFT.Results.Contracts.Meets;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;

internal sealed class GetMeetTypesHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetMeetTypesHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MeetTypeSummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<MeetType>()
        .Select(x => new MeetTypeSummary(x.MeetTypeId, x.Title))
        .ToListAsync(cancellationToken);
}