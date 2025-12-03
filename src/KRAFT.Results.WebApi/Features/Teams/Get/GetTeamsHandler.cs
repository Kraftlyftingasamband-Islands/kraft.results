using KRAFT.Results.Contracts.Teams;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.Get;

internal sealed class GetTeamsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetTeamsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TeamSummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.Country != null && x.Country.Iso3 == "ISL")
        .OrderBy(x => x.Title)
        .Select(x => new TeamSummary(
            x.Slug,
            x.Title,
            x.TitleShort,
            x.Athletes.Count))
        .ToListAsync(cancellationToken);
}