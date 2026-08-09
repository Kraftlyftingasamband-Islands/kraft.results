using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Clubs.Get;

internal sealed class GetClubsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetClubsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<ClubSummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.Country == Country.Iceland)
        .OrderBy(x => x.Title)
        .Select(x => new ClubSummary(
            x.Slug,
            x.Title,
            x.TitleShort,
            x.LogoImageFilename,
            x.Athletes.Count))
        .ToListAsync(cancellationToken);
}
