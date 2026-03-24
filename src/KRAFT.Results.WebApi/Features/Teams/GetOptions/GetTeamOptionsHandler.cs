using KRAFT.Results.Contracts.Teams;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.GetOptions;

internal sealed class GetTeamOptionsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetTeamOptionsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<TeamOption>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.Country != null && x.Country.Iso3 == "ISL")
        .OrderBy(x => x.Title)
        .Select(x => new TeamOption(
            x.TeamId,
            x.Title))
        .ToListAsync(cancellationToken);
}