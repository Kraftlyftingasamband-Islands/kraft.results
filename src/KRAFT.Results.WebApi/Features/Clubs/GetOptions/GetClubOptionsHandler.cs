using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Clubs.GetOptions;

internal sealed class GetClubOptionsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetClubOptionsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TeamOption>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.Country == Country.Iceland)
        .OrderBy(x => x.Title)
        .Select(x => new TeamOption(
            x.ClubId,
            x.Title))
        .ToListAsync(cancellationToken);
}
