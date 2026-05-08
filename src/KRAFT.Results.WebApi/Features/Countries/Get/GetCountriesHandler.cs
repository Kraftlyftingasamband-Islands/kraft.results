using KRAFT.Results.Contracts.Countries;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Countries.Get;

internal sealed class GetCountriesHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetCountriesHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<CountrySummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Country>()
        .Select(x => new CountrySummary(x.Iso3, x.Name))
        .ToListAsync(cancellationToken);
}